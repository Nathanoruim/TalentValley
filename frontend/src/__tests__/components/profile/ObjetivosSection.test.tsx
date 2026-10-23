import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ObjetivosSection } from "@/components/profile/ObjetivosSection";
import { updateObjetivos } from "@/lib/student";
import { sanitizeCargoDesejado } from "@/lib/validation";
import type { ObjetivosResponse } from "@/types/student";
import { makeSectionProps } from "./profileFixtures";

vi.mock("@/lib/student", () => ({
  updateObjetivos: vi.fn(),
}));

const FILLED: ObjetivosResponse = {
  cargoDesejado: "Desenvolvedor Full Stack",
  objetivoProfissional: "Atuar em projetos reais de tecnologia.",
};
const EMPTY: ObjetivosResponse = { cargoDesejado: null, objetivoProfissional: null };
const EMPTY_MESSAGE = "Informe o cargo que deseja alcançar e conte seus objetivos para o futuro.";
const EDIT_LABEL = "Editar objetivos profissionais";
const SUCCESS_MESSAGE = "Objetivos profissionais atualizados.";
const FALLBACK_ERROR = "Não foi possível salvar os objetivos. Tente novamente.";
const CARGO_LABEL = "Cargo desejado";
const OBJETIVO_LABEL = "Objetivo profissional";
const SAVE = "Salvar";

const updateObjetivosMock = vi.mocked(updateObjetivos);

function renderSection(objetivos: ObjetivosResponse) {
  const props = makeSectionProps({ objetivos });
  render(<ObjetivosSection {...props} />);
  return props;
}

async function openEditor() {
  await userEvent.click(screen.getByRole("button", { name: EDIT_LABEL }));
  const cargo = (await screen.findByRole("textbox", { name: CARGO_LABEL })) as HTMLInputElement;
  const objetivo = (await screen.findByRole("textbox", { name: OBJETIVO_LABEL })) as HTMLTextAreaElement;
  return { cargo, objetivo };
}

describe("ObjetivosSection", () => {
  beforeEach(() => {
    vi.resetAllMocks();
  });

  it("shows both saved values and no empty state", () => {
    renderSection(FILLED);

    expect(screen.getByRole("heading", { name: "Objetivos profissionais" })).toBeTruthy();
    expect(screen.getByText(FILLED.cargoDesejado!)).toBeTruthy();
    expect(screen.getByText(FILLED.objetivoProfissional!)).toBeTruthy();
    expect(screen.queryByText(EMPTY_MESSAGE)).toBeNull();
  });

  it("shows only the filled field when the other one is empty", () => {
    renderSection({ cargoDesejado: "Analista de Cibersegurança", objetivoProfissional: null });

    expect(screen.getByText("Analista de Cibersegurança")).toBeTruthy();
    expect(screen.queryByText(OBJETIVO_LABEL)).toBeNull();
    expect(screen.queryByText(EMPTY_MESSAGE)).toBeNull();
  });

  it("shows the empty state with a call to action for profiles without objectives", async () => {
    renderSection(EMPTY);

    expect(screen.getByText(EMPTY_MESSAGE)).toBeTruthy();
    await userEvent.click(screen.getByRole("button", { name: "Informar objetivos" }));

    const cargo = await screen.findByRole("textbox", { name: CARGO_LABEL });
    expect((cargo as HTMLInputElement).value).toBe("");
  });

  it("opens the dialog prefilled with counters for both fields", async () => {
    renderSection(FILLED);

    const { cargo, objetivo } = await openEditor();

    expect(screen.getByRole("dialog", { name: "Editar objetivos profissionais" })).toBeTruthy();
    expect(cargo.value).toBe(FILLED.cargoDesejado);
    expect(objetivo.value).toBe(FILLED.objetivoProfissional);
    expect(screen.getByText(`${FILLED.cargoDesejado!.length} de 100 caracteres`)).toBeTruthy();
    expect(screen.getByText(`${FILLED.objetivoProfissional!.length} de 500 caracteres`)).toBeTruthy();
  });

  it("sets the character limits on both inputs", async () => {
    renderSection(EMPTY);

    const { cargo, objetivo } = await openEditor();

    expect(cargo.maxLength).toBe(100);
    expect(objetivo.maxLength).toBe(500);
  });

  it("truncates pasted text to the limits and strips emoji", async () => {
    renderSection(EMPTY);
    const { cargo, objetivo } = await openEditor();

    fireEvent.change(cargo, { target: { value: `Dev \u{1F600}${"a".repeat(150)}` } });
    fireEvent.change(objetivo, { target: { value: "b".repeat(600) } });

    expect(cargo.value.length).toBe(100);
    expect(cargo.value.startsWith("Dev a")).toBe(true);
    expect(objetivo.value.length).toBe(500);
  });

  it("turns line breaks in the objective into a single space", async () => {
    renderSection(EMPTY);
    const { objetivo } = await openEditor();

    fireEvent.change(objetivo, { target: { value: "linha um\nlinha dois" } });
    expect(objetivo.value).toBe("linha um linha dois");

    fireEvent.change(objetivo, { target: { value: "linha um\r\nlinha dois" } });
    expect(objetivo.value).toBe("linha um linha dois");
  });

  it("sanitizes line breaks in the desired role into a space instead of joining words", () => {
    // A single-line input already drops line breaks on its own in the browser;
    // the sanitizer is the second line of defense for any other value source.
    expect(sanitizeCargoDesejado("Desenvolvedor\nBackend")).toBe("Desenvolvedor Backend");
    expect(sanitizeCargoDesejado("Desenvolvedor\r\nBackend")).toBe("Desenvolvedor Backend");
  });

  it("saves trimmed values, closes the dialog, refreshes and notifies", async () => {
    updateObjetivosMock.mockResolvedValue(undefined);
    const props = renderSection(EMPTY);
    const { cargo, objetivo } = await openEditor();

    fireEvent.change(cargo, { target: { value: "  Engenheiro de Automação  " } });
    fireEvent.change(objetivo, { target: { value: " Crescer na área " } });
    await userEvent.click(screen.getByRole("button", { name: SAVE }));

    await waitFor(() => expect(props.onChanged).toHaveBeenCalledTimes(1));
    expect(updateObjetivosMock).toHaveBeenCalledWith({
      cargoDesejado: "Engenheiro de Automação",
      objetivoProfissional: "Crescer na área",
    });
    expect(props.notify).toHaveBeenCalledWith(SUCCESS_MESSAGE);
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
  });

  it("allows saving only one field and sends null for the other", async () => {
    updateObjetivosMock.mockResolvedValue(undefined);
    const props = renderSection(EMPTY);
    const { objetivo } = await openEditor();

    fireEvent.change(objetivo, { target: { value: "Só o objetivo" } });
    await userEvent.click(screen.getByRole("button", { name: SAVE }));

    await waitFor(() => expect(props.onChanged).toHaveBeenCalledTimes(1));
    expect(updateObjetivosMock).toHaveBeenCalledWith({
      cargoDesejado: null,
      objetivoProfissional: "Só o objetivo",
    });
  });

  it("sends null for both fields when they are cleared", async () => {
    updateObjetivosMock.mockResolvedValue(undefined);
    const props = renderSection(FILLED);
    const { cargo, objetivo } = await openEditor();

    await userEvent.clear(cargo);
    await userEvent.clear(objetivo);
    await userEvent.click(screen.getByRole("button", { name: SAVE }));

    await waitFor(() => expect(props.onChanged).toHaveBeenCalledTimes(1));
    expect(updateObjetivosMock).toHaveBeenCalledWith({ cargoDesejado: null, objetivoProfissional: null });
  });

  it("shows the API error message and keeps the dialog open on failure", async () => {
    updateObjetivosMock.mockRejectedValue(new Error("Servidor indisponível"));
    const props = renderSection(FILLED);
    await openEditor();

    await userEvent.click(screen.getByRole("button", { name: SAVE }));

    expect((await screen.findByRole("alert")).textContent).toBe("Servidor indisponível");
    expect(screen.getByRole("dialog", { name: "Editar objetivos profissionais" })).toBeTruthy();
    expect(props.onChanged).not.toHaveBeenCalled();
    expect(props.notify).not.toHaveBeenCalled();
  });

  it("falls back to the default message when the rejection has no message", async () => {
    updateObjetivosMock.mockRejectedValue("falha");
    renderSection(FILLED);
    await openEditor();

    await userEvent.click(screen.getByRole("button", { name: SAVE }));

    expect((await screen.findByRole("alert")).textContent).toBe(FALLBACK_ERROR);
  });

  it("closes on cancel without saving and reopens with the original values", async () => {
    renderSection(FILLED);
    const { cargo } = await openEditor();
    await userEvent.type(cargo, " extra");

    await userEvent.click(screen.getByRole("button", { name: "Cancelar" }));
    await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    expect(updateObjetivosMock).not.toHaveBeenCalled();

    const reopened = await openEditor();
    expect(reopened.cargo.value).toBe(FILLED.cargoDesejado);
  });
});
