import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/recrutadores/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/AdminViews", () => ({ AdminRecruitersView: () => <p>admin recruiters view</p> }));

describe("admin recruiters page", () => {
  it("shows the recruiters view inside the role guard, restricted to admins", () => {
    render(<Page />);

    expectViewInGuard("admin recruiters view", ADMIN);
  });
});
