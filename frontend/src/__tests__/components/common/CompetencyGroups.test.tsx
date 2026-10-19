import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { CompetencyGroups } from "@/components/common/CompetencyGroups";
import type { TipoCompetencia } from "@/types/student";

const item = (id: number, nome: string, tipo: TipoCompetencia) => ({ id, nome, tipo });

describe("CompetencyGroups", () => {
  it("splits technical competencies into Hard skills and behavioral ones into Soft skills", () => {
    render(
      <CompetencyGroups
        competencias={[item(1, "Comunicação", "COMPORTAMENTAL"), item(2, "React", "TECNICA")]}
      />,
    );

    const hard = screen.getByText("Hard skills").parentElement!;
    const soft = screen.getByText("Soft skills").parentElement!;
    expect(hard.textContent).toContain("React");
    expect(hard.textContent).not.toContain("Comunicação");
    expect(soft.textContent).toContain("Comunicação");
    expect(soft.textContent).not.toContain("React");
  });

  it("does not render the heading of a group without competencies", () => {
    render(<CompetencyGroups competencias={[item(1, "React", "TECNICA")]} />);

    expect(screen.getByText("Hard skills")).toBeTruthy();
    expect(screen.queryByText("Soft skills")).toBeNull();
  });

  it("limits the visible chips, hard skills first, and counts the rest", () => {
    render(
      <CompetencyGroups
        limit={2}
        competencias={[
          item(1, "Comunicação", "COMPORTAMENTAL"),
          item(2, "React", "TECNICA"),
          item(3, "Docker", "TECNICA"),
          item(4, "Liderança", "COMPORTAMENTAL"),
        ]}
      />,
    );

    expect(screen.getByText("React")).toBeTruthy();
    expect(screen.getByText("Docker")).toBeTruthy();
    expect(screen.queryByText("Comunicação")).toBeNull();
    expect(screen.queryByText("Soft skills")).toBeNull();
    expect(screen.getByText("+2")).toBeTruthy();
  });
});
