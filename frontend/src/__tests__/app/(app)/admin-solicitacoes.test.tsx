import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/solicitacoes/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/RegistrationRequestsView", () => ({ RegistrationRequestsView: () => <p>registration requests view</p> }));

describe("admin registration requests page", () => {
  it("shows the requests view inside the role guard, restricted to admins", () => {
    render(<Page />);

    expectViewInGuard("registration requests view", ADMIN);
  });
});
