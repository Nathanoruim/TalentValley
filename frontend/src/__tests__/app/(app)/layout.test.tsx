import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import AppRouteLayout from "@/app/(app)/layout";

vi.mock("@/components/auth/ProtectedRoute", () => ({
  ProtectedRoute: ({ allowedRoles, children }: { allowedRoles?: string[]; children: ReactNode }) => (
    <div data-testid="protected-route" data-roles={allowedRoles?.join(",")}>
      {children}
    </div>
  ),
}));
vi.mock("@/components/layout/AppShell", () => ({
  AppShell: ({ children }: { children: ReactNode }) => <div data-testid="app-shell">{children}</div>,
}));

describe("app route layout", () => {
  it("shows the page inside the menu, and the menu only to signed-in users of any role", () => {
    render(
      <AppRouteLayout>
        <p>conteúdo</p>
      </AppRouteLayout>,
    );

    const guard = screen.getByTestId("protected-route");
    expect(guard.getAttribute("data-roles")).toBeNull();
    expect(within(within(guard).getByTestId("app-shell")).getByText("conteúdo")).not.toBeNull();
  });
});
