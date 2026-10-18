"use client";

import { useId, type ReactNode } from "react";
import { Card, Typography } from "@mui/material";
import { Reveal } from "./Reveal";

interface SurfaceCardProps {
  /** Section title (`h2`). Without it the card is just a surface. */
  title?: string;
  /** Control shown next to the title (an edit button, for example). */
  action?: ReactNode;
  /** Lifts the card on hover. Use it only when the whole card is a link or button. */
  interactive?: boolean;
  /** Position in a group, so the entrance of the cards is staggered. */
  index?: number;
  children: ReactNode;
}

/** Standard card of the signed-in screens: bordered surface, soft entrance, optional title row. */
export function SurfaceCard({ title, action, interactive = false, index = 0, children }: SurfaceCardProps) {
  const titleId = useId();

  return (
    <Reveal index={index} className="h-full">
      <Card
        component="section"
        aria-labelledby={title ? titleId : undefined}
        sx={(theme) => {
          const { surface } = (theme.vars ?? theme).palette;
          return {
            height: "100%",
            padding: 2.5,
            ...(interactive && {
              transition: theme.transitions.create(["box-shadow", "transform", "border-color"], { duration: 200 }),
              "&:hover": {
                borderColor: surface.border,
                boxShadow: theme.shadows[3],
                transform: "translateY(-2px)",
              },
              "@media (prefers-reduced-motion: reduce)": {
                transition: "none",
                "&:hover": { transform: "none" },
              },
            }),
          };
        }}
      >
        {title && (
          <div className="mb-3 flex items-center justify-between gap-2">
            <Typography id={titleId} component="h2" variant="h6">
              {title}
            </Typography>
            {action}
          </div>
        )}
        {children}
      </Card>
    </Reveal>
  );
}
