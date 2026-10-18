"use client";

import { useId, type ReactNode } from "react";
import { Box, Card, Typography } from "@mui/material";
import { Reveal } from "./Reveal";
import { useCountUp } from "./useCountUp";

interface StatCardProps {
  label: string;
  value: number;
  /** Material icon, decorative (the label already names the indicator). */
  icon: ReactNode;
  /** Highlights an indicator that asks for action (pending requests, for example). */
  attention?: boolean;
  /** Position in a group, so the entrance of the cards is staggered. */
  index?: number;
}

/** Indicator card: icon, label and a number that counts up. Zero is shown quietly unless it needs attention. */
export function StatCard({ label, value, icon, attention = false, index = 0 }: StatCardProps) {
  const labelId = useId();
  const shown = useCountUp(value);
  const quiet = value === 0 && !attention;

  return (
    <Reveal index={index} className="h-full">
      <Card
        role="group"
        aria-labelledby={labelId}
        sx={(theme) => {
          const palette = (theme.vars ?? theme).palette;
          return {
            height: "100%",
            padding: 2.5,
            ...(attention && { borderColor: palette.warning.main }),
          };
        }}
      >
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <Typography id={labelId} variant="body2" color="text.secondary">
              {label}
            </Typography>
            <Typography
              component="p"
              variant="h3"
              sx={{
                mt: 0.5,
                fontVariantNumeric: "tabular-nums",
                ...(quiet && { color: "text.secondary", fontWeight: 600 }),
              }}
            >
              {shown}
            </Typography>
            {attention && <span className="sr-only">Requer atenção</span>}
          </div>
          <Box
            aria-hidden="true"
            className="flex size-11 shrink-0 items-center justify-center"
            sx={(theme) => {
              const tone = (theme.vars ?? theme).palette[attention ? "warning" : "primary"].main;
              return {
                borderRadius: 3,
                color: tone,
                backgroundColor: `color-mix(in srgb, ${tone} 14%, transparent)`,
                opacity: quiet ? 0.6 : 1,
              };
            }}
          >
            {icon}
          </Box>
        </div>
      </Card>
    </Reveal>
  );
}
