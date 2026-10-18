import { render, screen } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";
import PeopleIcon from "@mui/icons-material/People";
import { afterEach, describe, expect, it, vi } from "vitest";
import { StatCard } from "@/components/common/StatCard";
import { talentValleyTheme } from "@/theme/theme";
import { stubMotionMedia } from "../auth/motion/motionMedia";

function card(props: Partial<Parameters<typeof StatCard>[0]> = {}) {
  return (
    <ThemeProvider theme={talentValleyTheme}>
      <StatCard label="Novos alunos" value={7} icon={<PeopleIcon />} {...props} />
    </ThemeProvider>
  );
}

const group = () => screen.getByRole("group", { name: "Novos alunos" });

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("StatCard", () => {
  it("names the group after its label and shows the value right away with reduced motion", () => {
    stubMotionMedia("reduced");
    render(card());
    expect(group().textContent).toContain("7");
  });

  it("counts up to the value when motion is allowed", async () => {
    stubMotionMedia("pointer");
    render(card({ value: 12 }));
    expect(await screen.findByText("12", {}, { timeout: 3000 })).toBeTruthy();
  });

  it("follows the value when it changes", () => {
    stubMotionMedia("reduced");
    const { rerender } = render(card({ value: 3 }));
    rerender(card({ value: 9 }));
    expect(group().textContent).toContain("9");
  });

  it("tells screen readers when the indicator needs attention", () => {
    stubMotionMedia("reduced");
    render(card({ attention: true }));
    expect(screen.getByText("Requer atenção")).toBeTruthy();
  });

  it("does not announce attention by default", () => {
    stubMotionMedia("reduced");
    render(card());
    expect(screen.queryByText("Requer atenção")).toBeNull();
  });

  it("still shows the number zero (quietly) when there is nothing to count", () => {
    stubMotionMedia("reduced");
    render(card({ value: 0 }));
    expect(group().textContent).toContain("0");
  });
});
