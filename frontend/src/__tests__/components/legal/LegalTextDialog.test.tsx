import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { LegalTextDialog } from "@/components/legal/LegalTextDialog";

const TITLE = "Termos de Uso e Política de Privacidade";

describe("LegalTextDialog", () => {
  it("links the dialog to its title and shows both texts, terms before privacy", () => {
    render(<LegalTextDialog open onClose={() => undefined} />);

    expect(screen.getByRole("dialog", { name: TITLE })).toBeTruthy();
    // Both texts keep the same section titles as h2.
    expect(screen.getAllByRole("heading", { level: 2, name: "1. Identidade e Contato" })).toHaveLength(2);

    const termsParagraph = screen.getByText(/Dúvidas sobre estes termos/);
    const privacyParagraph = screen.getByText(/Encarregado pelo tratamento de dados/);
    const followsTerms =
      termsParagraph.compareDocumentPosition(privacyParagraph) & Node.DOCUMENT_POSITION_FOLLOWING;
    expect(followsTerms).toBe(Node.DOCUMENT_POSITION_FOLLOWING);

    expect(screen.getByRole("button", { name: "Fechar" })).toBeTruthy();
  });

  it("asks to close when Fechar is clicked", async () => {
    const user = userEvent.setup({ delay: null });
    const onClose = vi.fn();
    render(<LegalTextDialog open onClose={onClose} />);

    await user.click(screen.getByRole("button", { name: "Fechar" }));

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("asks to close on the Escape key", async () => {
    const user = userEvent.setup({ delay: null });
    const onClose = vi.fn();
    render(<LegalTextDialog open onClose={onClose} />);

    await user.keyboard("{Escape}");

    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("renders nothing when closed", () => {
    render(<LegalTextDialog open={false} onClose={() => undefined} />);

    expect(screen.queryByRole("dialog")).toBeNull();
  });
});