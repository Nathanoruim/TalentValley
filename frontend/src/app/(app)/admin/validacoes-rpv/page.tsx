import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminRpvValidationsView } from "@/components/admin/AdminViews";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminRpvValidationsView /></ProtectedRoute>; }
