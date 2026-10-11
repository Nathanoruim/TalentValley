import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ConsentField } from "@/components/registration/ConsentField";

vi.setConfig({ testTimeout: 15_000 });

const ERROR = "Você deve aceitar os termos para continuar.";
const READ_TEXT_BUTTON = "Ler o texto completo";
const DIALOG_TITLE = "Termos de Uso e Política de Privacidade";

function renderField(props: Partial<Parameters<typeof ConsentField>[0]> = {}) {
  const onChange = vi.fn();
  const inputRef = vi.fn();
  render(<ConsentField checked={false} error={null} disabled={false} onChange={onChange} inputRef={inputRef} {...props} />);
  return { onChange, inputRef };
}

describe("ConsentField", () => {
  it("links the terms and the privacy policy in a new tab without leaking the opener", () => {
    renderField();

    const terms = screen.getByRole("link", { name: "Termos de Uso" });
    const privacy = screen.getByRole("link", { name: "Política de Privacidade" });
    expect(terms.getAttribute("href")).toBe("/termos");
    expect(privacy.getAttribute("href")).toBe("/privacidade");
    for (const link of [terms, privacy]) {
      expect(link.getAttribute("target")).toBe("_blank");
      expect(link.getAttribute("rel")).toBe("noopener noreferrer");
    }
  });

  it("reports the new checked state when the checkbox is toggled", async () => {
    const user = userEvent.setup({ delay: null });
    const { onChange } = renderField();

    await user.click(screen.getByRole("checkbox"));

    expect(onChange).toHaveBeenCalledExactlyOnceWith(true);
  });

  it("reports unchecking when it is already checked", async () => {
    const user = userEvent.setup({ delay: null });
    const { onChange } = renderField({ checked: true });
    const checkbox = screen.getByRole("checkbox") as HTMLInputElement;

    expect(checkbox.checked).toBe(true);
    await user.click(checkbox);

    expect(onChange).toHaveBeenCalledExactlyOnceWith(false);
  });

  it("is required and hands its input to the focus registry", () => {
    const { inputRef } = renderField();

    const checkbox = screen.getByRole("checkbox") as HTMLInputElement;
    expect(checkbox.required).toBe(true);
    expect(inputRef).toHaveBeenCalledWith(checkbox);
  });

  it("disables the checkbox", () => {
    renderField({ disabled: true });

    expect((screen.getByRole("checkbox") as HTMLInputElement).disabled).toBe(true);
  });

  it("shows the error text when there is one", () => {
    renderField({ error: ERROR });

    expect(screen.getByText(ERROR)).toBeTruthy();
  });

  it("shows no error text when there is none", () => {
    renderField();

    expect(screen.queryByText(ERROR)).toBeNull();
  });

  it("opens the full text dialog with both texts without checking the checkbox", async () => {
    const user = userEvent.setup({ delay: null });
    const { onChange } = renderField();
    const checkbox = screen.getByRole("checkbox") as HTMLInputElement;

    await user.click(screen.getByRole("button", { name: READ_TEXT_BUTTON }));

    expect(screen.getByRole("dialog", { name: DIALOG_TITLE })).toBeTruthy();
    expect(screen.getByText(/Dúvidas sobre estes termos/)).toBeTruthy();
    expect(screen.getByText(/Encarregado pelo tratamento de dados/)).toBeTruthy();
    expect(checkbox.checked).toBe(false);
    expect(onChange).not.toHaveBeenCalled();
  });

  it("closes the dialog with the Fechar button and keeps the checkbox unchecked", async () => {
    const user = userEvent.setup({ delay: null });
    const { onChange } = renderField();
    const checkbox = screen.getByRole("checkbox") as HTMLInputElement;

    await user.click(screen.getByRole("button", { name: READ_TEXT_BUTTON }));
    await user.click(screen.getByRole("button", { name: "Fechar" }));

    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(checkbox.checked).toBe(false);
    expect(onChange).not.toHaveBeenCalled();
  });

  it("closes the dialog with the Escape key without checking the checkbox", async () => {
    const user = userEvent.setup({ delay: null });
    const { onChange } = renderField();
    const checkbox = screen.getByRole("checkbox") as HTMLInputElement;

    await user.click(screen.getByRole("button", { name: READ_TEXT_BUTTON }));
    await user.keyboard("{Escape}");

    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(checkbox.checked).toBe(false);
    expect(onChange).not.toHaveBeenCalled();
  });
});