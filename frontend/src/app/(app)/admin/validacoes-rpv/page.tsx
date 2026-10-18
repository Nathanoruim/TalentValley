import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminRpvValidationsView } from "@/components/admin/AdminRpvValidationsView";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminRpvValidationsView /></ProtectedRoute>; }
