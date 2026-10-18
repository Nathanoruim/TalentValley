import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PageHeader } from "@/components/common/PageHeader";

describe("PageHeader", () => {
  it("renders the title as the page heading", () => {
    render(<PageHeader title="Visão geral" />);
    expect(screen.getByRole("heading", { level: 1, name: "Visão geral" })).toBeTruthy();
  });

  it("renders the description and the action when given", () => {
    render(
      <PageHeader
        title="Favoritos"
        description="Talentos que você salvou."
        action={<button type="button">Explorar talentos</button>}
      />,
    );
    expect(screen.getByText("Talentos que você salvou.")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Explorar talentos" })).toBeTruthy();
  });

  it("leaves out the description and the action when absent", () => {
    render(<PageHeader title="Auditoria" />);
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.getAllByRole("heading")).toHaveLength(1);
  });
});
