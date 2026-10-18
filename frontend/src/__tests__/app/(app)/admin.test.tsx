import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/AdminDashboardView", () => ({ AdminDashboardView: () => <p>admin dashboard view</p> }));

describe("admin dashboard page", () => {
  it("shows the dashboard view inside the role guard, restricted to admins", () => {
    render(<Page />);

    expectViewInGuard("admin dashboard view", ADMIN);
  });
});
