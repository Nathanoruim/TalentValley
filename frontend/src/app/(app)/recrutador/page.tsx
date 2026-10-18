"use client";

import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { RecruiterDashboardView } from "@/components/recruiter/RecruiterDashboardView";

export default function RecrutadorPage() {
  return (
    <ProtectedRoute allowedRoles={["RECRUTADOR"]}>
      <RecruiterDashboardView />
    </ProtectedRoute>
  );
}
