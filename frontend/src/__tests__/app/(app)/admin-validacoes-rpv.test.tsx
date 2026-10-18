import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/validacoes-rpv/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/AdminRpvValidationsView", () => ({ AdminRpvValidationsView: () => <p>admin rpv validations view</p> }));

describe("admin RPV validations page", () => {
  it("shows the RPV validations view inside the role guard, restricted to admins", () => {
    render(<Page />);

    expectViewInGuard("admin rpv validations view", ADMIN);
  });
});
