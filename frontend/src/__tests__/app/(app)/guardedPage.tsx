import { use, type ReactElement, type ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import { expect } from "vitest";

// Shared stubs and assertions for the thin (app) pages, which only compose
// ProtectedRoute (their role restriction) + one view. The menu and the session guard
// live in the (app) layout, so they are not part of these tests.
// Each page test mocks ProtectedRoute itself and keeps its own `it`.
export const ADMIN = "ADMIN";
export const RECRUITER = "RECRUTADOR";
export const STUDENT = "ALUNO";

export function ProtectedRouteStub({ allowedRoles, children }: { allowedRoles?: string[]; children: ReactNode }) {
  return (
    <div data-testid="protected-route" data-roles={allowedRoles?.join(",")}>
      {children}
    </div>
  );
}

// Asserts the page nests view inside ProtectedRoute and that the route is
// restricted to exactly `roles`.
export function expectViewInGuard(view: string, roles: string) {
  const guard = screen.getByTestId("protected-route");
  expect(guard.getAttribute("data-roles")).toBe(roles);
  expect(within(guard).getByText(view)).toBeTruthy();
}

const suspense = { active: false };
const NEVER_RESOLVES = new Promise<never>(() => undefined);

// View stand-in that can be told to suspend, to exercise a page's Suspense fallback.
export function SuspendableView({ label }: { label: string }) {
  if (suspense.active) use(NEVER_RESOLVES);
  return <p>{label}</p>;
}

// Renders the page while its view suspends and returns the skeletons it shows
// inside the guard instead of the view.
export function renderWhileViewSuspends(page: ReactElement, view: string) {
  suspense.active = true;
  try {
    render(page);
  } finally {
    suspense.active = false;
  }
  const guard = screen.getByTestId("protected-route");
  expect(within(guard).queryByText(view)).toBeNull();
  return guard.querySelectorAll(".MuiSkeleton-root");
}
