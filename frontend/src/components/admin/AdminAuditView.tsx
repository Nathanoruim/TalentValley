"use client";

import { useState } from "react";
import { Typography } from "@mui/material";
import HistoryOutlined from "@mui/icons-material/HistoryOutlined";
import { EmptyState } from "@/components/common/EmptyState";
import { adminApi } from "@/lib/admin";
import { formatUpdatedAt } from "@/lib/format";
import type { AuditItem } from "@/types/admin";
import { useLoad } from "@/components/admin/useLoad";
import { Page, LoadState, AdminRow, IconTile, Pager } from "@/components/admin/adminShared";

export function AdminAuditView() {
  const [page, setPage] = useState(1);
  const state = useLoad(() => adminApi.audit(page), [page]);
  return (
    <Page title="Auditoria" description="Registro cronológico das ações administrativas. Somente leitura.">
      <LoadState {...state} />
      {!state.loading && !state.error && state.data && (
        <>
          {state.data.items.length > 0 ? (
            <div className="flex flex-col gap-3">
              {state.data.items.map((x: AuditItem, index) => (
                <AdminRow key={x.id} index={index}>
                  <div className="flex min-w-0 gap-3">
                    <IconTile size={40}>
                      <HistoryOutlined fontSize="small" />
                    </IconTile>
                    <div className="flex min-w-0 flex-1 flex-col gap-1">
                      <div className="flex flex-col gap-0.5 sm:flex-row sm:items-center sm:justify-between sm:gap-3">
                        <Typography component="h2" variant="subtitle1" sx={{ fontWeight: 700, overflowWrap: "anywhere" }}>
                          {x.acao}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatUpdatedAt(x.criadoEm)}
                        </Typography>
                      </div>
                      <Typography variant="body2" sx={{ overflowWrap: "anywhere", wordBreak: "break-word" }}>
                        {x.descricao}
                      </Typography>
                      <div className="flex flex-wrap items-center gap-x-4 gap-y-0.5">
                        <Typography variant="caption" color="text.secondary" sx={{ overflowWrap: "anywhere" }}>
                          {x.adminEmail}
                        </Typography>
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{ overflowWrap: "anywhere", wordBreak: "break-word" }}
                        >
                          {x.entidadeTipo}: {x.entidadeId}
                        </Typography>
                      </div>
                    </div>
                  </div>
                </AdminRow>
              ))}
            </div>
          ) : (
            <EmptyState
              icon={<HistoryOutlined />}
              title="Nenhum registro de auditoria"
              description="As ações administrativas aparecerão aqui."
            />
          )}
          <Pager data={state.data} setPage={setPage} />
        </>
      )}
    </Page>
  );
}
