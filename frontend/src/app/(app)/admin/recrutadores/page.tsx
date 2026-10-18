import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminRecruitersView } from "@/components/admin/AdminRecruitersView";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminRecruitersView /></ProtectedRoute>; }
