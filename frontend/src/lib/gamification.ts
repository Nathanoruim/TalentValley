import type { MeResponse } from "@/types/student";

export const PROFILE_PROGRESS_WEIGHTS = {
  photo: 10,
  about: 10,
  contact: 10,
  education: 15,
  experience: 5,
  competencies: 15,
  languages: 5,
  availability: 5,
  projects: 15,
  curriculum: 10,
} as const;

export interface ProfileProgressStep {
  text: string;
  points: number;
  anchorId: string;
}

export interface ProfileProgress {
  percent: number;
  points: number;
  level: string;
  nextLevel: string | null;
  pointsToNext: number;
  steps: ProfileProgressStep[];
}

interface ProfileCriterion {
  points: number;
  text: string;
  anchorId: string;
  isComplete: (profile: MeResponse) => boolean;
}

function hasText(value: string | null | undefined): boolean {
  return Boolean(value?.trim());
}

function hasProfessionalLink(profile: MeResponse): boolean {
  const contact = profile.contato;
  return Boolean(
    hasText(contact?.linkedInUrl) ||
      hasText(contact?.gitHubUrl) ||
      hasText(contact?.portfolioUrl)
  );
}

const PROFILE_CRITERIA: readonly ProfileCriterion[] = [
  {
    points: PROFILE_PROGRESS_WEIGHTS.photo,
    text: "Adicione uma foto de perfil",
    anchorId: "foto",
    isComplete: (profile) => hasText(profile.dadosBasicos?.fotoUrl),
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.about,
    text: "Escreva uma apresentação com pelo menos 50 caracteres",
    anchorId: "sobre",
    isComplete: (profile) => (profile.sobre?.bio?.trim().length ?? 0) >= 50,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.contact,
    text: "Informe telefone, e-mail profissional e um link",
    anchorId: "contato",
    isComplete: (profile) =>
      hasText(profile.contato?.telefone) &&
      hasText(profile.contato?.emailProfissional) &&
      hasProfessionalLink(profile),
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.education,
    text: "Adicione uma formação",
    anchorId: "trajetoria",
    isComplete: (profile) => (profile.formacoes?.length ?? 0) > 0,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.experience,
    text: "Adicione uma experiência",
    anchorId: "trajetoria",
    isComplete: (profile) => (profile.experiencias?.length ?? 0) > 0,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.competencies,
    text: "Adicione pelo menos três competências",
    anchorId: "competencias",
    isComplete: (profile) => (profile.competencias?.length ?? 0) >= 3,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.languages,
    text: "Adicione um idioma",
    anchorId: "idiomas",
    isComplete: (profile) => (profile.idiomas?.length ?? 0) > 0,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.availability,
    text: "Informe disponibilidade e modalidade de trabalho",
    anchorId: "disponibilidade",
    isComplete: (profile) =>
      (profile.disponibilidades?.length ?? 0) > 0 &&
      (profile.modalidades?.length ?? 0) > 0,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.projects,
    text: "Adicione um projeto",
    anchorId: "projetos",
    isComplete: (profile) => (profile.projetos?.length ?? 0) > 0,
  },
  {
    points: PROFILE_PROGRESS_WEIGHTS.curriculum,
    text: "Envie seu currículo em PDF",
    anchorId: "curriculo",
    isComplete: (profile) => Boolean(profile.curriculo?.possuiCurriculo),
  },
];

export function getProfileLevel(points: number): Pick<ProfileProgress, "level" | "nextLevel" | "pointsToNext"> {
  if (points < 40) {
    return { level: "Começando", nextLevel: "Em evolução", pointsToNext: 40 - points };
  }

  if (points < 70) {
    return { level: "Em evolução", nextLevel: "Destaque", pointsToNext: 70 - points };
  }

  if (points < 90) {
    return { level: "Destaque", nextLevel: "Perfil completo", pointsToNext: 90 - points };
  }

  return { level: "Perfil completo", nextLevel: null, pointsToNext: 0 };
}

export function computeProfileProgress(profile: MeResponse): ProfileProgress {
  const pendingCriteria = PROFILE_CRITERIA.filter((criterion) => !criterion.isComplete(profile));
  const points = PROFILE_CRITERIA.reduce(
    (total, criterion) => total + (criterion.isComplete(profile) ? criterion.points : 0),
    0
  );
  const level = getProfileLevel(points);

  return {
    percent: points,
    points,
    ...level,
    steps: pendingCriteria
      .map(({ text, points: stepPoints, anchorId }, order) => ({
        text,
        points: stepPoints,
        anchorId,
        order,
      }))
      .sort((first, second) => second.points - first.points || first.order - second.order)
      .slice(0, 3)
      .map(({ text, points: stepPoints, anchorId }) => ({
        text,
        points: stepPoints,
        anchorId,
      })),
  };
}
