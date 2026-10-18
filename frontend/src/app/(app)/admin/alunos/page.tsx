import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminStudentsView } from "@/components/admin/AdminViews";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminStudentsView /></ProtectedRoute>; }
