"use client";

import { registrationApi } from "@/lib/admin";
import {
  normalizeWhitespace,
  validateCourseName,
  validateFreeText,
  validateInstitutionName,
  validateYear,
} from "@/lib/validation";
import { TIPO_FORMACAO_LABELS } from "@/lib/labels";
import type { TipoFormacao } from "@/types/student";
import { RegistrationWizard, type RegistrationProfile } from "./RegistrationWizard";
import {
  studentEducationBlank,
  StudentEducationStep,
  type StudentEducationForm,
} from "./StudentEducationStep";

const studentProfile: RegistrationProfile<StudentEducationForm> = {
  title: "Solicitar acesso como aluno",
  detailsStep: {
    label: "Formação",
    title: "Formação acadêmica",
    description: "Conte onde você estuda e qual formação está cursando.",
  },
  detailsFields: ["instituicaoEnsino", "curso", "tipoFormacao", "anoConclusaoPrevisto", "relacaoRioPombaValley"],
  blank: studentEducationBlank,
  validate: (school) => ({
    instituicaoEnsino: validateInstitutionName(school.instituicaoEnsino),
    curso: validateCourseName(school.curso),
    tipoFormacao: school.tipoFormacao ? null : "Selecione um tipo de formação válido.",
    anoConclusaoPrevisto: validateYear(school.anoConclusaoPrevisto),
    relacaoRioPombaValley: school.relacaoRioPombaValley
      ? validateFreeText(school.relacaoRioPombaValley, 500)
      : null,
  }),
  DetailsStep: StudentEducationStep,
  review: (school) => ({
    title: "Formação acadêmica",
    editLabel: "Editar formação acadêmica",
    rows: [
      { label: "Instituição de ensino", value: school.instituicaoEnsino },
      { label: "Curso", value: school.curso },
      {
        label: "Tipo de formação",
        value: school.tipoFormacao ? TIPO_FORMACAO_LABELS[school.tipoFormacao] : "",
      },
      { label: "Ano previsto de conclusão", value: school.anoConclusaoPrevisto },
      { label: "Relação com o Rio Pomba Valley", value: school.relacaoRioPombaValley },
    ],
  }),
  send: (base, school) =>
    registrationApi.student({
      ...school,
      ...base,
      instituicaoEnsino: normalizeWhitespace(school.instituicaoEnsino),
      curso: normalizeWhitespace(school.curso),
      tipoFormacao: school.tipoFormacao as TipoFormacao,
      anoConclusaoPrevisto: school.anoConclusaoPrevisto ? Number(school.anoConclusaoPrevisto) : null,
      relacaoRioPombaValley: school.relacaoRioPombaValley.trim() || null,
    }),
};

// Student signup in three steps: personal data, education, review and terms.
export function StudentRegistrationWizard() {
  return <RegistrationWizard profile={studentProfile} />;
}
