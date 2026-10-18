"use client";

import { Suspense } from "react";
import { Container, Skeleton } from "@mui/material";
import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { FavoritesView } from "@/components/recruiter/FavoritesView";

export default function FavoritesPage() {
  return <ProtectedRoute allowedRoles={["RECRUTADOR"]}><Suspense fallback={<Container sx={{ py: 4 }}><Skeleton variant="rounded" height={300} /></Container>}><FavoritesView /></Suspense></ProtectedRoute>;
}
