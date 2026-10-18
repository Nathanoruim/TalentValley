"use client";

import type { ReactNode } from "react";
import { Box, Typography } from "@mui/material";

interface EmptyStateProps {
  /** Material icon, decorative. */
  icon?: ReactNode;
  title: string;
  /** Element of the title. Use "h1" when the placeholder replaces a whole page. */
  titleAs?: "p" | "h1" | "h2";
  description?: string;
  /** What the person can do next (a button or link). */
  action?: ReactNode;
}

/** Friendly placeholder for a list or section with nothing to show yet. */
export function EmptyState({ icon, title, titleAs = "p", description, action }: EmptyStateProps) {
  return (
    <Box
      className="flex flex-col items-center gap-3 px-6 py-10 text-center"
      sx={(theme) => ({
        border: `1px dashed ${(theme.vars ?? theme).palette.surface.border}`,
        borderRadius: 4,
      })}
    >
      {icon && (
        <Box
          aria-hidden="true"
          className="flex size-12 items-center justify-center"
          sx={(theme) => {
            const palette = (theme.vars ?? theme).palette;
            return { backgroundColor: palette.surface.sunken, color: palette.text.secondary, borderRadius: 3 };
          }}
        >
          {icon}
        </Box>
      )}
      <div>
        <Typography component={titleAs} variant="subtitle1">
          {title}
        </Typography>
        {description && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, maxWidth: 420 }}>
            {description}
          </Typography>
        )}
      </div>
      {action}
    </Box>
  );
}
