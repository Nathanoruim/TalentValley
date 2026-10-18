import type { ReactElement, ReactNode } from "react";
import { cleanup, render as rtlRender, type RenderOptions } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";
import { afterEach, beforeEach, vi } from "vitest";
import { talentValleyTheme } from "@/theme/theme";
import { stubMotionMedia } from "../auth/motion/motionMedia";

/*
 * The recruiter screens use the Talent Valley theme tokens and the shared entrance animation,
 * which reads `matchMedia`. Importing this file gives every test of the file both, with
 * "reduce motion" on so nothing waits for an animation.
 */
beforeEach(() => {
  stubMotionMedia("reduced");
});

afterEach(() => {
  cleanup(); // unmount before the stub goes away: a late render still reads matchMedia
  vi.unstubAllGlobals();
});

function ThemeWrapper({ children }: { children: ReactNode }) {
  return <ThemeProvider theme={talentValleyTheme}>{children}</ThemeProvider>;
}

/** `render` of Testing Library inside the Talent Valley theme (kept by `rerender` too). */
export function renderThemed(ui: ReactElement, options?: Omit<RenderOptions, "wrapper">) {
  return rtlRender(ui, { wrapper: ThemeWrapper, ...options });
}
