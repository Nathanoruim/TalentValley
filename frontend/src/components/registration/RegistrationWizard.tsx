"use client";

import { useCallback, useState, type ComponentType, type FormEvent } from "react";
import { Alert, Button, Typography } from "@mui/material";
import { AuthFormPage } from "@/components/auth/AuthFormPage";
import { AuthSubmitButton } from "@/components/auth/AuthSubmitButton";
import { useMotionPolicy } from "@/components/auth/motion/useMotionPolicy";
import { getAuthErrorMessage } from "@/lib/authErrors";
import { normalizeEmailInput, normalizePhone, normalizeWhitespace } from "@/lib/validation";
import { PersonalDataStep } from "./PersonalDataStep";
import { RegistrationSuccess } from "./RegistrationSuccess";
import { REVIEW_FOLD_MS, ReviewStep, type ReviewDetails } from "./ReviewStep";
import { SignupFooter } from "./SignupFooter";
import {
  commonBlank,
  commonErrors,
  firstError,
  validateConsent,
  type CommonForm,
  type FieldErrors,
  type FocusableControl,
} from "./registrationForm";
import { useCepLookup } from "./useCepLookup";
import { useFormFocus } from "./useFormFocus";
import { useWizard } from "./useWizard";
import { WizardProgress, WizardStepHeading, WizardStepTransition } from "./WizardProgress";

interface WizardStep {
  label: string;
  title: string;
  description: string;
}

// Both signups share the first (personal data) and last (review and terms) steps;
// only the middle step changes with the profile.
const personalStep: WizardStep = { label: "Dados", title: "Dados pessoais", description: "Como podemos identificar você." };
const reviewStep: WizardStep = {
  label: "Revisão",
  title: "Revisão e termos",
  description: "Confira os dados e aceite os termos para enviar a solicitação.",
};

// Visual/DOM order of each step's fields. Used to focus the first invalid field.
const PERSONAL_FIELD_ORDER = ["nomeCompleto", "email", "telefone", "cep", "uf", "cidade"];
const REVIEW_FIELD_ORDER = ["consentTermos"];

// How long the drawn check stays on the button before the confirmation replaces the form.
const SUCCESS_PAUSE_MS = 700;

const wait = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

/** What every signup request carries, already normalized. The CEP is a lookup helper and is never sent. */
export interface RegistrationBasePayload {
  nomeCompleto: string;
  email: string;
  telefone: string;
  cidade: string;
  uf: string;
  consentTermos: true;
  consentNewsletter: boolean;
}

export interface DetailsStepProps<T> {
  value: T;
  errors: FieldErrors;
  onChange: (key: keyof T, value: string) => void;
  onBlur: (key: keyof T) => void;
  registerFieldRef: (key: string) => (node: FocusableControl) => void;
  disabled: boolean;
}

/** The parts of a signup that depend on the profile: its middle step, its validation and its request. */
export interface RegistrationProfile<T extends Record<string, string>> {
  title: string;
  detailsStep: WizardStep;
  detailsFields: string[];
  blank: T;
  validate: (details: T) => FieldErrors;
  DetailsStep: ComponentType<DetailsStepProps<T>>;
  review: (details: T) => ReviewDetails;
  send: (base: RegistrationBasePayload, details: T) => Promise<unknown>;
}

// Signup in three steps: personal data, profile details, review and terms.
// All data lives here, so moving between steps never loses what was typed.
export function RegistrationWizard<T extends Record<string, string>>({
  profile,
}: Readonly<{ profile: RegistrationProfile<T> }>) {
  const { DetailsStep } = profile;
  const steps = [personalStep, profile.detailsStep, reviewStep];
  const fieldOrder = [PERSONAL_FIELD_ORDER, profile.detailsFields, REVIEW_FIELD_ORDER];
  const wizard = useWizard(steps.length);
  const [common, setCommon] = useState<CommonForm>(commonBlank);
  const [details, setDetails] = useState<T>(profile.blank);
  const [consent, setConsent] = useState(false);
  const [newsletter, setNewsletter] = useState(false);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [busy, setBusy] = useState(false);
  const [finished, setFinished] = useState(false);
  const [success, setSuccess] = useState(false);
  const reducedMotion = useMotionPolicy() === "reduced";
  const { error, clearError, reportError, errorAlertRef, registerFieldRef } = useFormFocus();

  // Editing a field drops the error summary, so it never stays on screen
  // after the problem it described was fixed.
  const changeCommon = useCallback(
    (key: keyof CommonForm, value: string) => {
      setCommon((current) => ({ ...current, [key]: value }));
      setErrors((current) => ({ ...current, [key]: null }));
      clearError();
    },
    [clearError],
  );

  const changeDetails = (key: keyof T, value: string) => {
    setDetails((current) => ({ ...current, [key]: value }));
    setErrors((current) => ({ ...current, [key]: null }));
    clearError();
  };

  // The lookup lives here, not in the step, so coming back to the first step
  // never repeats it over a city the person already confirmed.
  const cepStatus = useCepLookup({
    cep: common.cep,
    cidade: common.cidade,
    uf: common.uf,
    onChange: changeCommon,
    disabled: success,
  });

  const stepErrors = (step: number): FieldErrors => {
    if (step === 0) return commonErrors(common);
    if (step === 1) return profile.validate(details);
    return { consentTermos: validateConsent(consent) };
  };

  const blurCommon = (key: keyof CommonForm) =>
    setErrors((current) => ({ ...current, [key]: commonErrors(common)[key] }));
  const blurDetails = (key: keyof T) =>
    setErrors((current) => ({ ...current, [key]: profile.validate(details)[key as string] }));

  const editStep = (step: number) => {
    clearError();
    wizard.goTo(step);
  };

  const submit = async () => {
    setBusy(true);
    try {
      // The review summary folds away before the request leaves.
      if (!reducedMotion) await wait(REVIEW_FOLD_MS);
      await profile.send(
        {
          nomeCompleto: normalizeWhitespace(common.nomeCompleto),
          email: normalizeEmailInput(common.email),
          telefone: normalizePhone(common.telefone),
          cidade: normalizeWhitespace(common.cidade),
          uf: common.uf,
          consentTermos: true,
          consentNewsletter: newsletter,
        },
        details,
      );
      // The check is drawn on the button before the confirmation takes over.
      setFinished(true);
      if (!reducedMotion) await wait(SUCCESS_PAUSE_MS);
      setSuccess(true);
    } catch (reason) {
      reportError(
        getAuthErrorMessage(reason, {
          fallback: "Não foi possível enviar a solicitação. Revise os dados e tente novamente.",
          overrides: { 403: "Não foi possível enviar a solicitação agora. Recarregue a página e tente novamente." },
        }),
      );
    } finally {
      setBusy(false);
    }
  };

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    clearError();

    // Advancing validates the current step; the last step validates every step,
    // so a stale earlier step can never be submitted.
    const stepsToValidate = wizard.isLast
      ? steps.map((_, index) => index)
      : [wizard.step];

    for (const step of stepsToValidate) {
      const nextErrors = stepErrors(step);
      const message = firstError(nextErrors);
      if (!message) continue;
      setErrors((current) => ({ ...current, ...nextErrors }));
      if (step !== wizard.step) wizard.goTo(step);
      reportError(message, fieldOrder[step].find((key) => nextErrors[key]) ?? null);
      return;
    }

    if (wizard.isLast) {
      void submit();
    } else {
      wizard.next();
    }
  };

  if (success) {
    return <RegistrationSuccess />;
  }

  const current = steps[wizard.step];

  return (
    <AuthFormPage
      eyebrow="FAÇA PARTE DO TALENT VALLEY"
      title={profile.title}
      subtitle="Seu pedido será analisado pela equipe do Talent Valley antes da criação da conta."
      onSubmit={handleSubmit}
      compact
      ariaBusy={busy}
      footer={<SignupFooter backHref="/cadastro" backLabel="Voltar para escolher perfil" inline />}
    >
      <WizardProgress steps={steps.map((step) => step.label)} activeStep={wizard.step} />

      {error && (
        <Alert
          ref={errorAlertRef}
          tabIndex={-1}
          severity="error"
          variant="filled"
          sx={{ fontSize: "0.875rem" }}
        >
          {error}
        </Alert>
      )}

      <WizardStepHeading
        step={wizard.step}
        total={steps.length}
        title={current.title}
        description={current.description}
      />

      <WizardStepTransition step={wizard.step} direction={wizard.direction} moved={wizard.moved}>
        {wizard.step === 0 && (
          <PersonalDataStep
            value={common}
            errors={errors}
            onChange={changeCommon}
            onBlur={blurCommon}
            registerFieldRef={registerFieldRef}
            disabled={busy}
            cepStatus={cepStatus}
          />
        )}

        {wizard.step === 1 && (
          <DetailsStep
            value={details}
            errors={errors}
            onChange={changeDetails}
            onBlur={blurDetails}
            registerFieldRef={registerFieldRef}
            disabled={busy}
          />
        )}

        {wizard.step === 2 && (
          <ReviewStep
            personal={common}
            details={profile.review(details)}
            consent={consent}
            consentError={errors.consentTermos}
            newsletter={newsletter}
            disabled={busy}
            collapsed={busy}
            onConsentChange={(checked) => {
              setConsent(checked);
              setErrors((currentErrors) => ({ ...currentErrors, consentTermos: null }));
              clearError();
            }}
            onNewsletterChange={setNewsletter}
            onEditStep={editStep}
            registerFieldRef={registerFieldRef}
          />
        )}
      </WizardStepTransition>

      {!wizard.isLast && (
        <Typography variant="caption" color="text.secondary">
          Os campos com * são obrigatórios.
        </Typography>
      )}

      <div className="flex gap-3">
        {!wizard.isFirst && (
          <Button
            type="button"
            variant="outlined"
            size="large"
            disabled={busy}
            onClick={() => editStep(wizard.step - 1)}
            sx={{ flexShrink: 0 }}
          >
            Voltar
          </Button>
        )}
        <div className="min-w-0 flex-1">
          <AuthSubmitButton loading={busy} success={finished} loadingLabel="Enviando solicitação...">
            {wizard.isLast ? "Enviar solicitação" : "Continuar"}
          </AuthSubmitButton>
        </div>
      </div>
    </AuthFormPage>
  );
}
