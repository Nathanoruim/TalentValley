import { ProtectedRoute } from "@/components/auth/ProtectedRoute";
import { AdminStudentDetailView } from "@/components/admin/AdminStudentDetailView";
export default async function Page({ params }: PageProps<"/admin/alunos/[id]">) { const { id } = await params; return <ProtectedRoute allowedRoles={["ADMIN"]}><AdminStudentDetailView id={id} /></ProtectedRoute>; }
