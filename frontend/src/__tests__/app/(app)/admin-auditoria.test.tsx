import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/auditoria/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/AdminViews", () => ({ AdminAuditView: () => <p>admin audit view</p> }));

describe("admin audit page", () => {
  it("shows the audit view inside the role guard, restricted to admins", () => {
    render(<Page />);

    expectViewInGuard("admin audit view", ADMIN);
  });
});
