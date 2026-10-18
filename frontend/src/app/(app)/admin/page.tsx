import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminDashboardView } from "@/components/admin/AdminDashboardView";

export default function Page() {
  return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminDashboardView /></ProtectedRoute>;
}
