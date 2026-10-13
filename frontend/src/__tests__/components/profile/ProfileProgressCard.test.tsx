import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ProfileProgressCard } from "@/components/profile/ProfileProgressCard";
import { CurriculoSection } from "@/components/profile/CurriculoSection";
import { SectionCard } from "@/components/profile/SectionCard";
import { StudentProfile } from "@/components/profile/StudentProfile";
import { useStudentProfile } from "@/hooks/useStudentProfile";
import { makeProfile, makeSectionProps } from "./profileFixtures";

const studentProfileHook = vi.hoisted(() => ({ useStudentProfile: vi.fn() }));

vi.mock("@/hooks/useStudentProfile", () => ({ useStudentProfile: studentProfileHook.useStudentProfile }));
vi.mock("@/components/profile/ProfileHeader", () => ({ ProfileHeader: () => null }));
vi.mock("@/components/profile/DadosBasicosSection", () => ({ DadosBasicosSection: () => null }));
vi.mock("@/components/profile/SobreSection", () => ({ SobreSection: () => null }));
vi.mock("@/components/profile/ContatoSection", () => ({ ContatoSection: () => null }));
vi.mock("@/components/profile/TrajetoriaSection", () => ({ TrajetoriaSection: () => null }));
vi.mock("@/components/profile/CompetenciasSection", () => ({ CompetenciasSection: () => null }));
vi.mock("@/components/profile/IdiomasSection", () => ({ IdiomasSection: () => null }));
vi.mock("@/components/profile/DisponibilidadeSection", () => ({ DisponibilidadeSection: () => null }));
vi.mock("@/components/profile/ProjetosSection", () => ({ ProjetosSection: () => null }));
vi.mock("@/components/profile/ExclusaoSection", () => ({ ExclusaoSection: () => null }));

const useStudentProfileMock = vi.mocked(useStudentProfile);

function emptyProfile() {
  return makeProfile({
    dadosBasicos: { nomeCompleto: "Maria Souza", fotoUrl: null, cidade: null, uf: null },
    sobre: { bio: null },
    contato: { telefone: null, emailProfissional: null, linkedInUrl: null, gitHubUrl: null, portfolioUrl: null },
    competencias: [],
    idiomas: [],
    disponibilidades: [],
    modalidades: [],
    formacoes: [],
    experiencias: [],
    projetos: [],
    curriculo: { possuiCurriculo: false, nomeArquivo: null },
  });
}

function completedProfile() {
  return makeProfile({
    dadosBasicos: { nomeCompleto: "Maria Souza", fotoUrl: "/foto", cidade: "Rio Pomba", uf: "MG" },
    sobre: { bio: "A".repeat(50) },
    competencias: [{ id: 1, nome: "React" }, { id: 2, nome: "TypeScript" }, { id: 3, nome: "MUI" }],
    formacoes: [{ id: "formacao-1", tipo: "GRADUACAO", nome: "Sistemas de Informação", instituicao: "IF", dataInicio: "2024-01-01", dataFim: null, cargaHoraria: null, status: "EM_ANDAMENTO", principal: true, ehRioPombaValley: false, statusValidacaoRpv: null, possuiCertificado: false, criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z" }],
    experiencias: [{ id: "experiencia-1", empresa: "Empresa", cargo: "Estagiária", tipo: "ESTAGIO", dataInicio: "2025-01-01", dataFim: null, atual: true, descricao: null, criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z" }],
    projetos: [{ id: "projeto-1", ordem: 1, nome: "Projeto", dataInicio: "2025-01-01", dataFim: null, emAndamento: true, descricao: "Projeto de teste", demoUrl: null, repositorioUrl: null, competencias: [], criadoEm: "2026-01-01T00:00:00Z", atualizadoEm: "2026-01-01T00:00:00Z" }],
    curriculo: { possuiCurriculo: true, nomeArquivo: "curriculo.pdf" },
  });
}

afterEach(() => {
  document.body.replaceChildren();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

describe("ProfileProgressCard", () => {
  it("shows percentage, points, level, progress accessibility and at most three steps", () => {
    render(<ProfileProgressCard profile={emptyProfile()} />);

    expect(screen.getByText("0% concluído · 0 pontos")).toBeTruthy();
    expect(screen.getByText("Começando")).toBeTruthy();
    expect(screen.getByRole("progressbar", { name: "Progresso do perfil" }).getAttribute("aria-valuenow")).toBe("0");
    expect(screen.getAllByRole("button")).toHaveLength(3);
  });

  it("scrolls to the selected stable anchor", async () => {
    const target = document.createElement("section");
    target.id = "trajetoria";
    const scrollIntoView = vi.fn();
    target.scrollIntoView = scrollIntoView;
    document.body.append(target);
    render(<ProfileProgressCard profile={emptyProfile()} />);

    await userEvent.click(screen.getByRole("button", { name: /Adicione uma formação/ }));

    expect(scrollIntoView).toHaveBeenCalledWith({ behavior: "smooth", block: "start" });
  });

  it("scrolls to every anchor offered by the profile steps", async () => {
    const targets = ["trajetoria", "competencias", "projetos"].map((id) => {
      const target = document.createElement("section");
      target.id = id;
      target.scrollIntoView = vi.fn();
      document.body.append(target);
      return target;
    });
    render(<ProfileProgressCard profile={emptyProfile()} />);

    const buttons = screen.getAllByRole("button");
    for (const button of buttons) {
      await userEvent.click(button);
    }

    for (const target of targets) {
      expect(target.scrollIntoView).toHaveBeenCalledWith({ behavior: "smooth", block: "start" });
    }
  });

  it("uses instant scrolling when reduced motion is preferred", async () => {
    vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true })));
    const target = document.createElement("section");
    target.id = "trajetoria";
    target.scrollIntoView = vi.fn();
    document.body.append(target);
    render(<ProfileProgressCard profile={emptyProfile()} />);

    await userEvent.click(screen.getAllByRole("button")[0]);

    expect(target.scrollIntoView).toHaveBeenCalledWith({ behavior: "auto", block: "start" });
  });

  it("renders every profile anchor with a margin for the fixed header", () => {
    useStudentProfileMock.mockReturnValue({
      profile: emptyProfile(),
      loading: false,
      error: null,
      refresh: vi.fn(),
    });
    const profileView = render(<StudentProfile />);

    for (const id of ["foto", "trajetoria", "projetos"]) {
      const target = document.getElementById(id);
      expect(target).not.toBeNull();
      expect(getComputedStyle(target!).scrollMarginTop).toBe("80px");
    }
    profileView.unmount();

    const cards = render(
      <>
        <SectionCard editLabel="Editar" onEdit={vi.fn()} title="Sobre">Conteúdo</SectionCard>
        <SectionCard editLabel="Editar" onEdit={vi.fn()} title="Contato">Conteúdo</SectionCard>
        <SectionCard editLabel="Editar" onEdit={vi.fn()} title="Competências">Conteúdo</SectionCard>
        <SectionCard editLabel="Editar" onEdit={vi.fn()} title="Idiomas">Conteúdo</SectionCard>
        <SectionCard editLabel="Editar" onEdit={vi.fn()} title="Disponibilidade e modalidade">Conteúdo</SectionCard>
      </>
    );

    for (const id of ["sobre", "contato", "competencias", "idiomas", "disponibilidade"]) {
      const target = document.getElementById(id);
      expect(target).not.toBeNull();
      expect(getComputedStyle(target!).scrollMarginTop).toBe("80px");
    }
    cards.unmount();

    render(<CurriculoSection {...makeSectionProps()} />);
    const curriculum = document.getElementById("curriculo");
    expect(curriculum).not.toBeNull();
    expect(getComputedStyle(curriculum!).scrollMarginTop).toBe("80px");
  });

  it("shows the completion message and no next-step buttons at one hundred percent", () => {
    render(<ProfileProgressCard profile={completedProfile()} />);

    expect(screen.getByText("100% concluído · 100 pontos")).toBeTruthy();
    expect(screen.getByText("Perfil completo")).toBeTruthy();
    expect(screen.getByText("Perfil completo! Mantenha-o atualizado.")).toBeTruthy();
    expect(screen.queryByRole("button")).toBeNull();
  });
});
