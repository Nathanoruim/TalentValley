import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import Page from "@/app/(app)/conta/privacidade/page";
import { RECRUITER, expectViewInGuard } from "@/__tests__/app/(app)/guardedPage";
import { fetchMyDataExport } from "@/lib/recruiter";

vi.mock("@/components/auth/ProtectedRoute", async () => ({ ProtectedRoute: (await import("@/__tests__/app/(app)/guardedPage")).ProtectedRouteStub }));
vi.mock("@/components/account/PrivacidadeSection", () => ({
  PrivacidadeSection: (props: { fetchData: unknown }) => (
    <p data-uses-recruiter-export={String(props.fetchData === fetchMyDataExport)}>privacy section</p>
  ),
}));

describe("recruiter privacy page", () => {
  it("shows the privacy section inside the role guard, restricted to recruiters", () => {
    render(<Page />);

    expectViewInGuard("privacy section", RECRUITER);
  });

  it("has the page title as its only main heading", () => {
    render(<Page />);

    expect(screen.getByRole("heading", { level: 1, name: "Minha conta" })).toBeTruthy();
  });

  it("downloads the recruiter's own export, never the student's", () => {
    render(<Page />);

    expect(screen.getByText("privacy section").getAttribute("data-uses-recruiter-export")).toBe("true");
  });
});
