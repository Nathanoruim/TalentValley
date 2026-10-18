import type { ReactNode } from "react";
import { Typography } from "@mui/material";

interface PageHeaderProps {
  title: string;
  description?: string;
  /** Main action of the page (a button), shown on the right on wide screens. */
  action?: ReactNode;
}

/** Title block of a signed-in page: the page's only `h1`, an optional description and an action. */
export function PageHeader({ title, description, action }: PageHeaderProps) {
  return (
    <header className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <Typography component="h1" variant="h4">
          {title}
        </Typography>
        {description && (
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            {description}
          </Typography>
        )}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </header>
  );
}
