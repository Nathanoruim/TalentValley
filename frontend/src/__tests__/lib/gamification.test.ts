import { describe, expect, it } from "vitest";
import {
  computeProfileProgress,
  getProfileLevel,
  PROFILE_PROGRESS_WEIGHTS,
} from "@/lib/gamification";
import { makeProfile } from "@/__tests__/components/profile/profileFixtures";
import type { MeResponse } from "@/types/student";

function emptyProfile(): MeResponse {
  return makeProfile({
    dadosBasicos: { nomeCompleto: "Maria Souza", fotoUrl: null, cidade: null, uf: null },
    sobre: { bio: null },
    contato: {
      telefone: null,
      emailProfissional: null,
      linkedInUrl: null,
      gitHubUrl: null,
      portfolioUrl: null,
    },
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

const formation = {
  id: "formacao-1",
  tipo: "GRADUACAO" as const,
  nome: "Sistemas de Informação",
  instituicao: "IF Sudeste MG",
  dataInicio: "2024-01-01",
  dataFim: null,
  cargaHoraria: null,
  status: "EM_ANDAMENTO" as const,
  principal: true,
  ehRioPombaValley: true,
  statusValidacaoRpv: null,
  possuiCertificado: false,
  criadoEm: "2026-01-01T00:00:00Z",
  atualizadoEm: "2026-01-01T00:00:00Z",
};

const experience = {
  id: "experiencia-1",
  empresa: "Empresa teste",
  cargo: "Estagiária",
  tipo: "ESTAGIO" as const,
  dataInicio: "2025-01-01",
  dataFim: null,
  atual: true,
  descricao: null,
  criadoEm: "2026-01-01T00:00:00Z",
  atualizadoEm: "2026-01-01T00:00:00Z",
};

const project = {
  id: "projeto-1",
  ordem: 1,
  nome: "Projeto teste",
  dataInicio: "2025-01-01",
  dataFim: null,
  emAndamento: true,
  descricao: "Descrição do projeto de teste.",
  demoUrl: null,
  repositorioUrl: null,
  competencias: [],
  criadoEm: "2026-01-01T00:00:00Z",
  atualizadoEm: "2026-01-01T00:00:00Z",
};

describe("computeProfileProgress", () => {
  it("returns zero for an empty profile", () => {
    const progress = computeProfileProgress(emptyProfile());

    expect(progress).toMatchObject({
      percent: 0,
      points: 0,
      level: "Começando",
      nextLevel: "Em evolução",
      pointsToNext: 40,
    });
    expect(progress.steps).toHaveLength(3);
  });

  it("returns one hundred for a fully completed profile", () => {
    const profile = emptyProfile();
    profile.dadosBasicos.fotoUrl = "/foto";
    profile.sobre.bio = "A".repeat(50);
    profile.contato = {
      telefone: "32999999999",
      emailProfissional: "maria@example.com",
      linkedInUrl: "https://linkedin.com/in/maria",
      gitHubUrl: null,
      portfolioUrl: null,
    };
    profile.formacoes = [formation];
    profile.experiencias = [experience];
    profile.competencias = [
      { id: 1, nome: "React" },
      { id: 2, nome: "TypeScript" },
      { id: 3, nome: "MUI" },
    ];
    profile.idiomas = [{ idiomaId: 1, nome: "Inglês", nivel: "INTERMEDIARIO" }];
    profile.disponibilidades = ["ESTAGIO"];
    profile.modalidades = ["REMOTO"];
    profile.projetos = [project];
    profile.curriculo = { possuiCurriculo: true, nomeArquivo: "curriculo.pdf" };

    expect(computeProfileProgress(profile)).toEqual({
      percent: 100,
      points: 100,
      level: "Perfil completo",
      nextLevel: null,
      pointsToNext: 0,
      steps: [],
    });
  });

  it.each([
    [PROFILE_PROGRESS_WEIGHTS.photo, "foto", (profile: MeResponse) => { profile.dadosBasicos.fotoUrl = "/foto"; }],
    [PROFILE_PROGRESS_WEIGHTS.about, "sobre", (profile: MeResponse) => { profile.sobre.bio = "A".repeat(50); }],
    [PROFILE_PROGRESS_WEIGHTS.contact, "contato", (profile: MeResponse) => { profile.contato = { telefone: "32999999999", emailProfissional: "maria@example.com", linkedInUrl: "https://linkedin.com/in/maria", gitHubUrl: null, portfolioUrl: null }; }],
    [PROFILE_PROGRESS_WEIGHTS.education, "formação", (profile: MeResponse) => { profile.formacoes = [formation]; }],
    [PROFILE_PROGRESS_WEIGHTS.experience, "experiência", (profile: MeResponse) => { profile.experiencias = [experience]; }],
    [PROFILE_PROGRESS_WEIGHTS.competencies, "competências", (profile: MeResponse) => { profile.competencias = [{ id: 1, nome: "React" }, { id: 2, nome: "TypeScript" }, { id: 3, nome: "MUI" }]; }],
    [PROFILE_PROGRESS_WEIGHTS.languages, "idiomas", (profile: MeResponse) => { profile.idiomas = [{ idiomaId: 1, nome: "Inglês", nivel: "INTERMEDIARIO" }]; }],
    [PROFILE_PROGRESS_WEIGHTS.availability, "disponibilidade", (profile: MeResponse) => { profile.disponibilidades = ["ESTAGIO"]; profile.modalidades = ["REMOTO"]; }],
    [PROFILE_PROGRESS_WEIGHTS.projects, "projetos", (profile: MeResponse) => { profile.projetos = [project]; }],
    [PROFILE_PROGRESS_WEIGHTS.curriculum, "currículo", (profile: MeResponse) => { profile.curriculo = { possuiCurriculo: true, nomeArquivo: "curriculo.pdf" }; }],
  ])("awards %i points only for %s", (expectedPoints, _criterion, complete) => {
    const profile = emptyProfile();
    complete(profile);

    const progress = computeProfileProgress(profile);

    expect(progress.points).toBe(expectedPoints);
    expect(progress.percent).toBe(expectedPoints);
  });

  it("handles optional data and threshold limits", () => {
    const profile = {
      ...emptyProfile(),
      dadosBasicos: undefined,
      sobre: { bio: "A".repeat(49) },
      competencias: [{ id: 1, nome: "React" }, { id: 2, nome: "TypeScript" }],
    } as unknown as MeResponse;

    expect(computeProfileProgress(profile).points).toBe(0);

    profile.sobre = { bio: "A".repeat(50) };
    profile.competencias = [
      { id: 1, nome: "React" },
      { id: 2, nome: "TypeScript" },
      { id: 3, nome: "MUI" },
    ];
    expect(computeProfileProgress(profile).points).toBe(25);
  });

  it("uses the defined level boundaries", () => {
    expect(getProfileLevel(39)).toMatchObject({ level: "Começando", nextLevel: "Em evolução", pointsToNext: 1 });
    expect(getProfileLevel(40)).toMatchObject({ level: "Em evolução", nextLevel: "Destaque", pointsToNext: 30 });
    expect(getProfileLevel(69)).toMatchObject({ level: "Em evolução", nextLevel: "Destaque", pointsToNext: 1 });
    expect(getProfileLevel(70)).toMatchObject({ level: "Destaque", nextLevel: "Perfil completo", pointsToNext: 20 });
    expect(getProfileLevel(89)).toMatchObject({ level: "Destaque", nextLevel: "Perfil completo", pointsToNext: 1 });
    expect(getProfileLevel(90)).toEqual({ level: "Perfil completo", nextLevel: null, pointsToNext: 0 });
  });

  it("orders pending steps by point value and limits them to three", () => {
    const progress = computeProfileProgress(emptyProfile());

    expect(progress.steps).toEqual([
      { text: "Adicione uma formação", points: 15, anchorId: "trajetoria" },
      { text: "Adicione pelo menos três competências", points: 15, anchorId: "competencias" },
      { text: "Adicione um projeto", points: 15, anchorId: "projetos" },
    ]);
  });
});
