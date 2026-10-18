"use client";

import { useParams } from "next/navigation";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { TalentProfileView } from "@/components/recruiter/TalentProfileView";

export default function TalentProfilePage() {
  const { slug } = useParams<{ slug: string }>();
  return <ProtectedRoute allowedRoles={["RECRUTADOR"]}><TalentProfileView slug={slug} /></ProtectedRoute>;
}
