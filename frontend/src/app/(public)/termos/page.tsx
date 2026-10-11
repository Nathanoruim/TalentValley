import type { Metadata } from "next";
import { LegalDocument } from "@/components/legal/LegalDocument";
import { TERMS_SECTIONS } from "@/components/legal/legalContent";

export const metadata: Metadata = { title: "Termos de Uso | Talent Valley" };

export default function TermosPage() {
  return (
    <LegalDocument
      title="Termos de Uso"
      effective="a partir da publicação (implantação)"
      sections={TERMS_SECTIONS}
      lastUpdated="data da implantação"
    />
  );
}