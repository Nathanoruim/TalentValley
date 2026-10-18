import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/alunos/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/AdminStudentsView", () => ({ AdminStudentsView: () => <p>admin students view</p> }));

describe("admin students page", () => {
  it("shows the students view inside the role guard, restricted to admins", () => {
    render(<Page />);

    expectViewInGuard("admin students view", ADMIN);
  });
});
