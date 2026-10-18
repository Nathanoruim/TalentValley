import { render } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import Page from "@/app/(app)/recrutador/comparar/page";
import {
  RECRUITER,
  expectViewInGuard,
  renderWhileViewSuspends,
} from "@/__tests__/app/(app)/guardedPage";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/recruiter/TalentComparisonView", async () => {
  const { SuspendableView } = await import("@/__tests__/app/(app)/guardedPage");
  return { TalentComparisonView: () => <SuspendableView label="comparison view" /> };
});

describe("recruiter comparison page", () => {
  it("shows the comparison view inside the role guard, restricted to recruiters", () => {
    render(<Page />);

    expectViewInGuard("comparison view", RECRUITER);
  });

  it("shows a loading skeleton while the view loads", () => {
    const skeletons = renderWhileViewSuspends(<Page />, "comparison view");

    expect(skeletons).toHaveLength(1);
  });
});
