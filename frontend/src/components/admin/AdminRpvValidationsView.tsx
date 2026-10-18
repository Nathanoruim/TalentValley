"use client";

import { useCallback, useEffect, useId, useRef, useState } from "react";
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Snackbar, Stack, Typography } from "@mui/material";
import WorkspacePremiumOutlined from "@mui/icons-material/WorkspacePremiumOutlined";
import { EmptyState } from "@/components/common/EmptyState";
import { adminApi } from "@/lib/admin";
import { getApiErrorMessage } from "@/lib/api";
import { formatDate, formatUpdatedAt } from "@/lib/format";
import { STATUS_FORMACAO_LABELS, STATUS_VALIDACAO_RPV_LABELS, TIPO_FORMACAO_LABELS } from "@/lib/labels";
import { ProtectedFileButton } from "@/components/recruiter/ProtectedFileButton";
import type { RpvValidationDetail } from "@/types/admin";
import { useLoad } from "@/components/admin/useLoad";
import { Page, LoadState, AdminRow, DetailSkeleton, IconTile, Status, Pager, Confirm } from "@/components/admin/adminShared";

export function AdminRpvValidationsView() {
  const [page, setPage] = useState(1),
    [selectedId, setSelectedId] = useState<string | null>(null),
    [detail, setDetail] = useState<RpvValidationDetail | null>(null),
    [detailLoading, setDetailLoading] = useState(false),
    [detailError, setDetailError] = useState<string | null>(null),
    [target, setTarget] = useState<"aprovar" | "rejeitar" | null>(null),
    [busy, setBusy] = useState(false),
    [actionError, setActionError] = useState<string | null>(null),
    [notice, setNotice] = useState<string | null>(null);
  const state = useLoad(() => adminApi.validations(page), [page]);
  const detailRequestRef = useRef(0);
  const busyRef = useRef(false);
  const mountedRef = useRef(true);
  const titleId = useId();
  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);
  const open = useCallback((id: string) => {
    const requestId = ++detailRequestRef.current;
    setSelectedId(id);
    setDetail(null);
    setDetailLoading(true);
    setDetailError(null);
    setActionError(null);
    setTarget(null);
    adminApi
      .validation(id)
      .then((data) => {
        if (mountedRef.current && requestId === detailRequestRef.current)
          setDetail(data);
      })
      .catch((e) => {
        if (mountedRef.current && requestId === detailRequestRef.current)
          setDetailError(
            getApiErrorMessage(e, "Não foi possível carregar a validação."),
          );
      })
      .finally(() => {
        if (mountedRef.current && requestId === detailRequestRef.current)
          setDetailLoading(false);
      });
  }, []);
  const retryDetail = useCallback(() => {
    if (selectedId) open(selectedId);
  }, [open, selectedId]);
  const closeDetail = useCallback(() => {
    detailRequestRef.current += 1;
    setSelectedId(null);
    setDetail(null);
    setDetailLoading(false);
    setDetailError(null);
    setActionError(null);
    setTarget(null);
  }, []);
  const startTarget = (value: "aprovar" | "rejeitar") => {
    setActionError(null);
    setTarget(value);
  };
  const cancelTarget = () => {
    setActionError(null);
    setTarget(null);
  };
  const action = async () => {
    if (!detail || !target || busyRef.current) return;
    // When the only row of a non-first page is processed, that page becomes
    // empty, so navigate back instead of reloading a blank page.
    const emptyAfter = state.data?.items.length === 1 && page > 1;
    busyRef.current = true;
    setBusy(true);
    setActionError(null);
    try {
      await adminApi.validationAction(detail.formacaoId, target);
      setNotice(`Formação ${target === "aprovar" ? "aprovada" : "rejeitada"}.`);
      closeDetail();
      if (emptyAfter) setPage((p) => (p > 1 ? p - 1 : p));
      else state.reload();
    } catch (e) {
      setActionError(
        getApiErrorMessage(e, "Não foi possível concluir a validação."),
      );
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  };
  return (
    <Page title="Validações RPV" description="Analise o certificado de cada formação e aprove ou rejeite a validação.">
      <LoadState {...state} />
      {!state.loading && !state.error && state.data && (
        <>
          <Stack spacing={1.5}>
            {state.data.items.map((x, index) => (
              <AdminRow key={x.formacaoId} index={index}>
                <Stack
                  direction={{ xs: "column", md: "row" }}
                  spacing={2}
                  sx={{
                    justifyContent: "space-between",
                    alignItems: { md: "center" },
                  }}
                >
                  <Stack direction="row" spacing={1.5} sx={{ alignItems: "center", minWidth: 0 }}>
                    <IconTile>
                      <WorkspacePremiumOutlined />
                    </IconTile>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>{x.nome}</Typography>
                      <Typography color="text.secondary">
                        {x.alunoNome} · {x.instituicao} · {TIPO_FORMACAO_LABELS[x.tipo]}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Atualizado {formatUpdatedAt(x.atualizadoEm)} ·{" "}
                        {x.possuiCertificado ? "Certificado disponível" : "Sem certificado"}
                      </Typography>
                    </Box>
                  </Stack>
                  <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
                    <Status active={x.alunoAtivo} />
                    <Button variant="outlined" onClick={() => void open(x.formacaoId)}>
                      Analisar
                    </Button>
                  </Stack>
                </Stack>
              </AdminRow>
            ))}
          </Stack>
          {state.data.items.length === 0 && (
            <EmptyState
              icon={<WorkspacePremiumOutlined />}
              title="Não há validações RPV pendentes."
              description="Quando um aluno enviar uma formação para validação, ela aparece aqui."
            />
          )}
          <Pager data={state.data} setPage={setPage} />
        </>
      )}
      <Dialog
        open={selectedId !== null}
        onClose={busy ? undefined : closeDetail}
        fullWidth
        maxWidth="sm"
        aria-labelledby={titleId}
      >
        <DialogTitle id={titleId}>Validação de formação RPV</DialogTitle>
        <DialogContent>
          {detailLoading && (
            <DetailSkeleton />
          )}
          {!detailLoading && detailError && (
            <Alert
              severity="error"
              action={
                <Button color="inherit" onClick={retryDetail}>
                  Tentar novamente
                </Button>
              }
            >
              {detailError}
            </Alert>
          )}
          {!detailLoading && !detailError && detail && (
            <Stack spacing={2} sx={{ pt: 1, minWidth: 0 }}>
              <Typography variant="h6" sx={{ overflowWrap: "anywhere" }}>
                {detail.formacao.nome}
              </Typography>
              <Box component="dl" className="m-0 grid gap-x-6 gap-y-3 sm:grid-cols-2">
                {[
                  ["Aluno", detail.aluno.nomeCompleto],
                  ["Situação do aluno", <Status key="status" active={detail.aluno.ativo} />],
                  ["Instituição", detail.formacao.instituicao],
                  ["Tipo", TIPO_FORMACAO_LABELS[detail.formacao.tipo]],
                  [
                    "Período",
                    `${formatDate(detail.formacao.dataInicio)}${detail.formacao.dataFim ? ` — ${formatDate(detail.formacao.dataFim)}` : ""}`,
                  ],
                  ["Carga horária", detail.formacao.cargaHoraria ?? "Não informada"],
                  ["Status da formação", STATUS_FORMACAO_LABELS[detail.formacao.status]],
                  ["Validação RPV", STATUS_VALIDACAO_RPV_LABELS[detail.formacao.statusValidacaoRpv]],
                ].map(([label, value]) => (
                  <Box key={String(label)} sx={{ minWidth: 0 }}>
                    <Typography component="dt" variant="caption" color="text.secondary">
                      {label}
                    </Typography>
                    <Typography component="dd" sx={{ m: 0, overflowWrap: "anywhere" }}>
                      {value}
                    </Typography>
                  </Box>
                ))}
              </Box>
              {detail.formacao.possuiCertificado ? (
                <ProtectedFileButton
                  path={`/api/admin/validacoes-rpv/${detail.formacaoId}/certificado`}
                  label="Abrir certificado"
                />
              ) : (
                <Alert severity="info">
                  Não há certificado disponível para esta formação.
                </Alert>
              )}
            </Stack>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDetail} disabled={busy}>
            Fechar
          </Button>
          {!detailLoading && !detailError && detail && (
            <>
              <Button
                color="error"
                onClick={() => startTarget("rejeitar")}
                disabled={busy}
              >
                Rejeitar
              </Button>
              <Button
                variant="contained"
                onClick={() => startTarget("aprovar")}
                disabled={busy}
              >
                Aprovar
              </Button>
            </>
          )}
        </DialogActions>
      </Dialog>
      <Confirm
        open={!!target}
        title={
          target === "aprovar" ? "Aprovar formação?" : "Rejeitar formação?"
        }
        text={
          target === "aprovar"
            ? "A formação será marcada como verificada pelo Rio Pomba Valley."
            : "A formação será marcada como rejeitada."
        }
        busy={busy}
        error={actionError}
        onClose={cancelTarget}
        confirm={() => void action()}
        danger={target === "rejeitar"}
      />
      <Snackbar
        open={!!notice}
        autoHideDuration={5000}
        onClose={() => setNotice(null)}
        message={notice}
      />
    </Page>
  );
}
