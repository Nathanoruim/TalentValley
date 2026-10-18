import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminAuditView } from "@/components/admin/AdminAuditView";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminAuditView /></ProtectedRoute>; }
