"use client";

import { useState, type FormEvent } from "react";
import { Stack, TextField, Typography } from "@mui/material";
import { getApiErrorMessage } from "@/lib/api";
import {
  CARGO_DESEJADO_MAX_LENGTH,
  OBJETIVO_PROFISSIONAL_MAX_LENGTH,
  sanitizeCargoDesejado,
  sanitizeObjetivoProfissional,
} from "@/lib/validation";
import { updateObjetivos } from "@/lib/student";
import type { ObjetivosResponse } from "@/types/student";
import { FormDialog } from "./FormDialog";
import { SectionCard } from "./SectionCard";
import type { SectionProps } from "./sectionProps";

const TEXT_SX = {
  whiteSpace: "pre-wrap",
  overflowWrap: "anywhere",
  wordBreak: "break-word",
  color: "text.primary",
} as const;

export function ObjetivosSection({ profile, onChanged, notify }: SectionProps) {
  const [open, setOpen] = useState(false);
  const { cargoDesejado, objetivoProfissional } = profile.objetivos;

  return (
    <SectionCard
      title="Objetivos profissionais"
      editLabel="Editar objetivos profissionais"
      onEdit={() => setOpen(true)}
      isEmpty={!cargoDesejado && !objetivoProfissional}
      emptyMessage="Informe o cargo que deseja alcançar e conte seus objetivos para o futuro."
      emptyActionLabel="Informar objetivos"
    >
      <Stack spacing={2}>
        {cargoDesejado && (
          <Stack spacing={0.5}>
            <Typography variant="subtitle2" color="text.secondary">
              Cargo desejado
            </Typography>
            <Typography variant="body2" sx={TEXT_SX}>
              {cargoDesejado}
            </Typography>
          </Stack>
        )}
        {objetivoProfissional && (
          <Stack spacing={0.5}>
            <Typography variant="subtitle2" color="text.secondary">
              Objetivo profissional
            </Typography>
            <Typography variant="body2" sx={TEXT_SX}>
              {objetivoProfissional}
            </Typography>
          </Stack>
        )}
      </Stack>
      {open && (
        <ObjetivosForm
          objetivos={profile.objetivos}
          onClose={() => setOpen(false)}
          onSaved={() => {
            setOpen(false);
            onChanged();
            notify("Objetivos profissionais atualizados.");
          }}
        />
      )}
    </SectionCard>
  );
}

interface ObjetivosFormProps {
  objetivos: ObjetivosResponse;
  onClose: () => void;
  onSaved: () => void;
}

function ObjetivosForm({ objetivos, onClose, onSaved }: ObjetivosFormProps) {
  const [cargo, setCargo] = useState(() => sanitizeCargoDesejado(objetivos.cargoDesejado ?? ""));
  const [objetivo, setObjetivo] = useState(() =>
    sanitizeObjetivoProfissional(objetivos.objetivoProfissional ?? "")
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSaving(true);
    try {
      // Blank becomes null, matching the backend semantics (empty value stored as null).
      await updateObjetivos({
        cargoDesejado: cargo.trim() ? cargo.trim() : null,
        objetivoProfissional: objetivo.trim() ? objetivo.trim() : null,
      });
      onSaved();
    } catch (err) {
      setError(
        getApiErrorMessage(err, "Não foi possível salvar os objetivos. Tente novamente.")
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <FormDialog
      title="Editar objetivos profissionais"
      onClose={onClose}
      onSubmit={handleSubmit}
      saving={saving}
      error={error}
    >
      <Stack spacing={2.5}>
        <TextField
          id="objetivos-cargo"
          label="Cargo desejado"
          value={cargo}
          onChange={(e) => setCargo(sanitizeCargoDesejado(e.target.value))}
          fullWidth
          disabled={saving}
          helperText={`${cargo.length} de ${CARGO_DESEJADO_MAX_LENGTH} caracteres`}
          slotProps={{
            htmlInput: { maxLength: CARGO_DESEJADO_MAX_LENGTH, "aria-label": "Cargo desejado" },
          }}
        />
        <TextField
          id="objetivos-descricao"
          label="Objetivo profissional"
          value={objetivo}
          onChange={(e) => setObjetivo(sanitizeObjetivoProfissional(e.target.value))}
          multiline
          rows={5}
          fullWidth
          disabled={saving}
          helperText={`${objetivo.length} de ${OBJETIVO_PROFISSIONAL_MAX_LENGTH} caracteres`}
          slotProps={{
            htmlInput: {
              maxLength: OBJETIVO_PROFISSIONAL_MAX_LENGTH,
              "aria-label": "Objetivo profissional",
            },
          }}
        />
        <Typography variant="caption" color="text.secondary">
          Os dois campos são opcionais. Descreva brevemente onde você quer chegar na carreira,
          em um único parágrafo (quebras de linha são trocadas por espaço).
        </Typography>
      </Stack>
    </FormDialog>
  );
}
