import { render, screen } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";
import SearchOffIcon from "@mui/icons-material/SearchOff";
import { describe, expect, it } from "vitest";
import { EmptyState } from "@/components/common/EmptyState";
import { talentValleyTheme } from "@/theme/theme";

function renderState(props: Partial<Parameters<typeof EmptyState>[0]> = {}) {
  return render(
    <ThemeProvider theme={talentValleyTheme}>
      <EmptyState title="Nenhum favorito ainda" {...props} />
    </ThemeProvider>,
  );
}

describe("EmptyState", () => {
  it("shows only the title when nothing else is given", () => {
    renderState();
    expect(screen.getByText("Nenhum favorito ainda")).toBeTruthy();
    expect(screen.queryByRole("button")).toBeNull();
  });

  it("renders the title as a paragraph by default and as a heading when asked", () => {
    const { unmount } = renderState();
    expect(screen.queryByRole("heading")).toBeNull();
    unmount();
    renderState({ titleAs: "h1" });
    expect(screen.getByRole("heading", { level: 1, name: "Nenhum favorito ainda" })).toBeTruthy();
  });

  it("shows the description, the icon and the action", () => {
    const { container } = renderState({
      icon: <SearchOffIcon />,
      description: "Favorite talentos para vê-los aqui.",
      action: <button type="button">Explorar talentos</button>,
    });
    expect(screen.getByText("Favorite talentos para vê-los aqui.")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Explorar talentos" })).toBeTruthy();
    expect(container.querySelector("svg[data-testid='SearchOffIcon']")).not.toBeNull();
  });

  it("hides the decorative icon from screen readers", () => {
    const { container } = renderState({ icon: <SearchOffIcon /> });
    expect(container.querySelector("[aria-hidden='true'] svg")).not.toBeNull();
  });
});
