import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/meu-perfil/page";
import { STUDENT, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/profile/StudentProfile", () => ({ StudentProfile: () => <p>student profile</p> }));

describe("my profile page", () => {
  it("shows the student profile inside the role guard, restricted to students", () => {
    render(<Page />);

    expectViewInGuard("student profile", STUDENT);
  });
});
