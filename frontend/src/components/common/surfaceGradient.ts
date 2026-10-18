import type { Theme } from "@mui/material/styles";

/** Soft brand-tinted wash used behind highlighted blocks (search, profile header, preview, selection bar). */
export function brandWash(theme: Theme, strength = 14): string {
  const { primary, surface } = (theme.vars ?? theme).palette;
  return `linear-gradient(135deg, color-mix(in srgb, ${primary.main} ${strength}%, ${surface.raised}) 0%, ${surface.raised} 70%)`;
}
