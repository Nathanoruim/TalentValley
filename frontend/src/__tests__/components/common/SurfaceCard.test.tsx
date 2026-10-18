import { render, screen } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SurfaceCard } from "@/components/common/SurfaceCard";
import { talentValleyTheme } from "@/theme/theme";
import { stubMotionMedia } from "../auth/motion/motionMedia";

function renderCard(props: Partial<Parameters<typeof SurfaceCard>[0]> = {}) {
  return render(
    <ThemeProvider theme={talentValleyTheme}>
      <SurfaceCard {...props}>Conteúdo do cartão</SurfaceCard>
    </ThemeProvider>,
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("SurfaceCard", () => {
  it("renders the content and the title as a labelled section", () => {
    stubMotionMedia("reduced");
    renderCard({ title: "Sobre", action: <button type="button">Editar</button> });
    const section = screen.getByRole("region", { name: "Sobre" });
    expect(section.textContent).toContain("Conteúdo do cartão");
    expect(screen.getByRole("heading", { level: 2, name: "Sobre" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Editar" })).toBeTruthy();
  });

  it("is not a landmark when it has no title", () => {
    stubMotionMedia("reduced");
    renderCard();
    expect(screen.queryByRole("region")).toBeNull();
    expect(screen.getByText("Conteúdo do cartão")).toBeTruthy();
  });

  it("still renders as a titled card section when interactive", () => {
    stubMotionMedia("pointer");
    renderCard({ title: "Lista", interactive: true });
    const section = screen.getByRole("region", { name: "Lista" });
    expect(section.className).toContain("MuiCard-root");
  });
});
