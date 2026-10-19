"use client";

import { Suspense } from "react";
import { Container, Skeleton } from "@mui/material";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { CONTENT_MAX_WIDTH } from "@/components/layout/contentWidth";
import { FavoritesView } from "@/components/recruiter/FavoritesView";

export default function FavoritesPage() {
  return <ProtectedRoute allowedRoles={["RECRUTADOR"]}><Suspense fallback={<Container maxWidth={false} sx={{ maxWidth: CONTENT_MAX_WIDTH, py: 4 }}><Skeleton variant="rounded" height={300} /></Container>}><FavoritesView /></Suspense></ProtectedRoute>;
}
