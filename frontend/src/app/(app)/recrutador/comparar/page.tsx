"use client";

import { Suspense } from "react";
import { Container, Skeleton } from "@mui/material";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { CONTENT_MAX_WIDTH } from "@/components/layout/contentWidth";
import { TalentComparisonView } from "@/components/recruiter/TalentComparisonView";

export default function ComparisonPage() {
  return <ProtectedRoute allowedRoles={["RECRUTADOR"]}><Suspense fallback={<Container maxWidth={false} sx={{ maxWidth: CONTENT_MAX_WIDTH, py: 4 }}><Skeleton variant="rounded" height={500} /></Container>}><TalentComparisonView /></Suspense></ProtectedRoute>;
}
