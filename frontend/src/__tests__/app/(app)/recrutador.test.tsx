import { render } from "@testing-library/react";
import { describe, it, vi } from "vitest";
import Page from "@/app/(app)/recrutador/page";
import { RECRUITER, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/recruiter/RecruiterDashboardView", () => ({ RecruiterDashboardView: () => <p>recruiter dashboard view</p> }));

describe("recruiter dashboard page", () => {
  it("shows the dashboard view inside the role guard, restricted to recruiters", () => {
    render(<Page />);

    expectViewInGuard("recruiter dashboard view", RECRUITER);
  });
});
