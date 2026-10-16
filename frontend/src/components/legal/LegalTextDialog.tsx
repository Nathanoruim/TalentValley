"use client";

import { useId } from "react";
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from "@mui/material";
import { PRIVACY_SECTIONS, TERMS_SECTIONS } from "./legalContent";
import type { LegalSection } from "./LegalDocument";

// Mostra os dois textos legais (Termos de Uso e Política de Privacidade) em uma
// janela com rolagem, sem sair do formulário. Não altera nenhum estado do cadastro.
export function LegalTextDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const titleId = useId();
  const renderSections = (sections: LegalSection[]) =>
    sections.map((section) => (
      <section key={section.title}>
        <Typography component="h2" variant="h6" sx={{ fontWeight: 600, mt: 1.5 }}>
          {section.title}
        </Typography>
        {section.paragraphs.map((text) => (
          <Typography key={text} color="text.secondary">
            {text}
          </Typography>
        ))}
      </section>
    ));

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm" aria-labelledby={titleId}>
      <DialogTitle id={titleId}>Termos de Uso e Política de Privacidade</DialogTitle>
      <DialogContent dividers>
        <Stack>
          {renderSections(TERMS_SECTIONS)}
          {renderSections(PRIVACY_SECTIONS)}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button type="button" variant="contained" onClick={onClose} autoFocus>
          Fechar
        </Button>
      </DialogActions>
    </Dialog>
  );
}