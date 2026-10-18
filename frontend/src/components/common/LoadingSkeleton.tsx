import { Skeleton } from "@mui/material";

type LoadingSkeletonVariant = "stats" | "cards" | "list";

interface LoadingSkeletonProps {
  variant?: LoadingSkeletonVariant;
  /** How many placeholders to draw (at most 6). */
  count?: number;
  /** Height of each placeholder in pixels, when the real items are taller than the default. */
  height?: number;
}

const LAYOUT: Record<LoadingSkeletonVariant, { className: string; height: number }> = {
  stats: { className: "grid gap-4 sm:grid-cols-2 lg:grid-cols-3", height: 104 },
  cards: { className: "grid gap-4 sm:grid-cols-2", height: 168 },
  list: { className: "flex flex-col gap-3", height: 76 },
};

const SLOTS = ["first", "second", "third", "fourth", "fifth", "sixth"];

/** Placeholder shown while data loads. Screen readers hear "Carregando…"; the shapes are hidden from them. */
export function LoadingSkeleton({ variant = "list", count = 3, height: customHeight }: LoadingSkeletonProps) {
  const { className, height: defaultHeight } = LAYOUT[variant];
  const height = customHeight ?? defaultHeight;

  return (
    <div role="status">
      <span className="sr-only">Carregando…</span>
      <div aria-hidden="true" className={className}>
        {SLOTS.slice(0, count).map((slot) => (
          <Skeleton key={slot} variant="rounded" height={height} sx={{ borderRadius: 4 }} />
        ))}
      </div>
    </div>
  );
}
