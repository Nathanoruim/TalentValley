"use client";

import type { ReactNode } from "react";
import { usePathname } from "next/navigation";
import { Reveal } from "@/components/common/Reveal";

// Next remounts a template only when the segment right below it changes, so moving between pages
// of the same section (admin, recrutador) would not animate. Keying by address makes every page
// change play the soft fade and rise, while the menu in the layout stays in place. Query changes
// (filters, pages) do not alter the pathname, so they never replay it nor reset a screen.
export default function AppTemplate({ children }: { children: ReactNode }) {
  const pathname = usePathname();

  return (
    <Reveal key={pathname} className="flex flex-1 flex-col">
      {children}
    </Reveal>
  );
}
