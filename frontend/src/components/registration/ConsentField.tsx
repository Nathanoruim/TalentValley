"use client";

import { useState } from "react";
import Link from "next/link";
import { Button, Checkbox, FormControl, FormControlLabel, FormHelperText } from "@mui/material";
import { LegalTextDialog } from "@/components/legal/LegalTextDialog";
import type { FocusableControl } from "./registrationForm";

export function ConsentField({
  checked,
  error,
  disabled,
  onChange,
  inputRef,
}: {
  checked: boolean;
  error: string | null | undefined;
  disabled: boolean;
  onChange: (checked: boolean) => void;
  inputRef: (node: FocusableControl) => void;
}) {
  const [legalTextOpen, setLegalTextOpen] = useState(false);

  return (
    <FormControl error={Boolean(error)}>
      <FormControlLabel
        control={
          <Checkbox
            checked={checked}
            onChange={(event) => onChange(event.target.checked)}
            slotProps={{ input: { ref: inputRef } }}
            disabled={disabled}
            required
          />
        }
        label={
          <>
            Eu li e concordo com os{" "}
            <Link href="/termos" target="_blank" rel="noopener noreferrer">Termos de Uso</Link>
            {" "}e a{" "}
            <Link href="/privacidade" target="_blank" rel="noopener noreferrer">Política de Privacidade</Link>.
          </>
        }
      />
      {/* Fora do FormControlLabel de propósito: clicar aqui não pode marcar a caixa. */}
      <Button
        type="button"
        variant="text"
        size="small"
        onClick={() => setLegalTextOpen(true)}
        sx={{ alignSelf: "flex-start", minWidth: 0, p: 0.25 }}
      >
        Ler o texto completo
      </Button>
      {error && <FormHelperText>{error}</FormHelperText>}
      <LegalTextDialog open={legalTextOpen} onClose={() => setLegalTextOpen(false)} />
    </FormControl>
  );
}