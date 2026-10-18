import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminDashboardView } from "@/components/admin/AdminViews";

export default function Page() {
  return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminDashboardView /></ProtectedRoute>;
}
