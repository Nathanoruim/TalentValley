import type { ReactNode } from "react";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AppShell } from "@/components/layout/AppShell";

// Single guard and menu of every signed-in screen, so the menu stays mounted while the user
// moves between pages. Each page still restricts itself to its own roles with ProtectedRoute.
export default function AppRouteLayout({ children }: { children: ReactNode }) {
  return (
    <ProtectedRoute>
      <AppShell>{children}</AppShell>
    </ProtectedRoute>
  );
}
