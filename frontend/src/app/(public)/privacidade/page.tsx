import type { Metadata } from "next";
import { LegalDocument } from "@/components/legal/LegalDocument";
import { PRIVACY_SECTIONS } from "@/components/legal/legalContent";

export const metadata: Metadata = { title: "Política de Privacidade | Talent Valley" };

export default function PrivacidadePage() {
  return (
    <LegalDocument
      title="Política de Privacidade"
      effective="a partir da publicação (implantação)"
      sections={PRIVACY_SECTIONS}
      lastUpdated="data da implantação"
    />
  );
}