import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminRecruitersView } from "@/components/admin/AdminViews";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminRecruitersView /></ProtectedRoute>; }
