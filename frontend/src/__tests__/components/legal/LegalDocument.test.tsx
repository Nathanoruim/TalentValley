import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LegalDocument } from "@/components/legal/LegalDocument";

const sections = [{ title: "1. Identidade", paragraphs: ["Texto da seção."] }];

function renderDocument() {
  return render(<LegalDocument title="Termos de Uso" effective="01/01/2026" sections={sections} lastUpdated="02/02/2026" />);
}

describe("LegalDocument", () => {
  it("shows the logo at the top as a link to the landing page", () => {
    renderDocument();

    const logo = screen.getByRole("link", { name: "Talent Valley — ir para a página inicial" });
    expect(logo.getAttribute("href")).toBe("/");
  });

  it("keeps the Página inicial button next to the logo", () => {
    renderDocument();

    expect(screen.getByRole("link", { name: "Página inicial" }).getAttribute("href")).toBe("/");
  });

  it("shows the title, the effective date, the sections and the last update", () => {
    renderDocument();

    expect(screen.getByRole("heading", { level: 1, name: "Termos de Uso" })).not.toBeNull();
    expect(screen.getByText("01/01/2026")).not.toBeNull();
    expect(screen.getByRole("heading", { level: 2, name: "1. Identidade" })).not.toBeNull();
    expect(screen.getByText("Texto da seção.")).not.toBeNull();
    expect(screen.getByText(/02\/02\/2026/)).not.toBeNull();
  });
});
