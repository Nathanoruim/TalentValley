import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { RegistrationRequestsView } from "@/components/admin/RegistrationRequestsView";
export default function Page() { return <ProtectedRoute allowedRoles={["ADMIN"]}><RegistrationRequestsView /></ProtectedRoute>; }
