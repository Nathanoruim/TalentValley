"use client";

import { Suspense } from "react";
import { Container, Skeleton, Stack } from "@mui/material";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { TalentDiscoveryView } from "@/components/recruiter/TalentDiscoveryView";

export default function TalentDiscoveryPage() {
  return <ProtectedRoute allowedRoles={["RECRUTADOR"]}>
    <Suspense fallback={<Container maxWidth="lg" sx={{ py: 4 }}><Stack spacing={2}>{[1, 2, 3].map((item) => <Skeleton key={item} variant="rounded" height={180} />)}</Stack></Container>}>
      <TalentDiscoveryView />
    </Suspense>
  </ProtectedRoute>;
}
