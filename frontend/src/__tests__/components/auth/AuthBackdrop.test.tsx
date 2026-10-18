import type { ReactNode } from "react";
import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { stubMotionMedia } from "./motion/motionMedia";
import { AuthBackdrop } from "@/components/auth/AuthBackdrop";

vi.mock("framer-motion", async (importOriginal) => ({
  ...(await importOriginal<typeof import("framer-motion")>()),
  useInView: () => true,
}));
vi.mock("@/components/auth/LoginVisualScene", () => ({ LoginVisualScene: () => null }));
vi.mock("@/components/auth/motion/AmbientMotion", () => ({ AmbientMotion: ({ children }: { children: ReactNode }) => <>{children}</> }));
vi.mock("@/components/auth/motion/AuroraBackground", () => ({ AuroraBackground: () => null }));
vi.mock("@/components/auth/motion/DotSpotlight", () => ({ DotSpotlight: () => null }));
vi.mock("@/components/auth/motion/TalentNetwork", () => ({ TalentNetwork: () => null }));
vi.mock("@/components/auth/motion/usePageVisible", () => ({ usePageVisible: () => true }));
vi.mock("@/components/auth/motion/usePointerParallax", () => ({ usePointerParallax: () => ({ current: { x: 0, y: 0 } }) }));

describe("AuthBackdrop", () => {
  beforeEach(() => stubMotionMedia("reduced"));
  afterEach(() => vi.unstubAllGlobals());

  it("makes the logo a link to the landing page, on every screen that uses the backdrop", () => {
    render(
      <AuthBackdrop entrance={false}>
        <p>formulário</p>
      </AuthBackdrop>,
    );

    const logo = screen.getByRole("link", { name: "Talent Valley — ir para a página inicial" });
    expect(logo.getAttribute("href")).toBe("/");
    expect(logo.textContent).toContain("Talent");
  });

  it("keeps the logo above the content box, which covers the whole screen and would swallow its clicks", () => {
    render(
      <AuthBackdrop entrance={false}>
        <p>formulário</p>
      </AuthBackdrop>,
    );

    const logoBox = screen.getByRole("link", { name: "Talent Valley — ir para a página inicial" }).parentElement as HTMLElement;
    const contentBox = screen.getByText("formulário").parentElement as HTMLElement;
    expect(Number(getComputedStyle(logoBox).zIndex)).toBeGreaterThan(Number(getComputedStyle(contentBox).zIndex));
  });

  it("still shows the page content over the backdrop", () => {
    render(
      <AuthBackdrop entrance>
        <p>formulário</p>
      </AuthBackdrop>,
    );

    expect(screen.getByText("formulário")).not.toBeNull();
  });
});
