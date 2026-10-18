import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/admin/alunos/[id]/page";
import { ADMIN, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/admin/AdminStudentDetailView", () => ({ AdminStudentDetailView: ({ id }: { id: string }) => <p>student detail {id}</p> }));

describe("admin student detail page", () => {
  it("resolves the route id and passes it to the detail view, restricted to admins", async () => {
    render(await Page({ params: Promise.resolve({ id: "42" }), searchParams: Promise.resolve({}) }));

    expectViewInGuard("student detail 42", ADMIN);
  });
});
