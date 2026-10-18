"use client";

import Link from "next/link";
import { Box, Stack, Typography } from "@mui/material";
import { TalentValleyMark } from "./TalentValleyMark";

export const BRAND_LINK_LABEL = "Talent Valley — ir para a página inicial";

/** Keyboard focus ring shared by every logo that links to the landing page. */
export const brandFocusSx = (offset: number) => ({
  "&:focus-visible": { outline: "2px solid", outlineColor: "primary.main", outlineOffset: offset },
});

interface BrandLinkProps {
  /** Only the mark, for the collapsed menu. The link keeps its full accessible name. */
  compact?: boolean;
  onNavigate?: () => void;
}

/** Mark and wordmark of the signed-in screens and legal pages. It always leads to the landing page. */
export function BrandLink({ compact = false, onNavigate }: BrandLinkProps) {
  return (
    <Stack
      component={Link}
      href="/"
      aria-label={BRAND_LINK_LABEL}
      onClick={onNavigate}
      direction="row"
      spacing={1.25}
      sx={{
        alignItems: "center",
        color: "inherit",
        textDecoration: "none",
        borderRadius: 1,
        minWidth: 0,
        ...brandFocusSx(2),
      }}
    >
      <Box sx={{ display: "flex", flexShrink: 0 }}>
        <TalentValleyMark width={compact ? 40 : 46} />
      </Box>
      {!compact && (
        <Typography
          sx={{
            fontFamily: "var(--font-display), sans-serif",
            fontWeight: 600,
            fontSize: 17,
            letterSpacing: "-0.04em",
            lineHeight: 1.1,
            color: "text.primary",
            whiteSpace: "nowrap",
          }}
        >
          Talent{" "}
          <Box component="span" sx={{ color: "secondary.main" }}>
            Valley
          </Box>
        </Typography>
      )}
    </Stack>
  );
}
