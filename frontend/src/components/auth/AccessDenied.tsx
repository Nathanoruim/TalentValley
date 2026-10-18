"use client";

import { Box } from "@mui/material";
import { getRoleDestination } from "@/lib/paths";
import type { UserRole } from "@/types/auth";
import { SystemStatePage } from "./SystemStatePage";

// Shown by ProtectedRoute when the signed-in role may not open the page. It appears inside
// the content area of the app menu, and the action takes the user to the home of their own role.
export function AccessDenied({ role }: { role: UserRole }) {
  return (
    <Box sx={{ flexGrow: 1, display: "flex", alignItems: "center", justifyContent: "center", p: 2 }}>
      <Box
        sx={{
          width: "100%",
          maxWidth: 520,
          p: { xs: 2.5, sm: 4 },
          border: 1,
          borderColor: "divider",
          borderRadius: 4,
          bgcolor: "background.paper",
        }}
      >
        <SystemStatePage
          scene="locked"
          eyebrow="ERRO 403"
          title="Acesso negado"
          message="Seu perfil não tem permissão para acessar esta página."
          severity="warning"
          action={{ label: "Ir para minha área", href: getRoleDestination(role) }}
        />
      </Box>
    </Box>
  );
}
