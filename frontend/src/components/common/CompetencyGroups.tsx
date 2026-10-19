import { Chip, Stack, Typography } from "@mui/material";
import type { TipoCompetencia } from "@/types/student";

interface CompetencyItem {
  id: number;
  nome: string;
  tipo: TipoCompetencia;
}

interface CompetencyGroupsProps {
  competencias: CompetencyItem[];
  size?: "small" | "medium";
  variant?: "filled" | "outlined";
  color?: "default" | "primary";
  // Maximum number of chips shown (hard skills first); the rest becomes a "+N" chip.
  limit?: number;
}

const GROUPS: { tipo: TipoCompetencia; label: string }[] = [
  { tipo: "TECNICA", label: "Hard skills" },
  { tipo: "COMPORTAMENTAL", label: "Soft skills" },
];

// Shows competencies split into hard skills (technical) and soft skills (behavioral).
// A group with no competencies is not rendered.
export function CompetencyGroups({
  competencias,
  size = "small",
  variant = "filled",
  color = "default",
  limit,
}: CompetencyGroupsProps) {
  const ordered = GROUPS.flatMap((group) => competencias.filter((item) => item.tipo === group.tipo));
  const visibleIds = new Set((limit === undefined ? ordered : ordered.slice(0, limit)).map((item) => item.id));
  const hidden = ordered.length - visibleIds.size;

  return (
    <Stack spacing={1}>
      {GROUPS.map(({ tipo, label }) => {
        const items = competencias.filter((item) => item.tipo === tipo && visibleIds.has(item.id));
        if (items.length === 0) return null;
        return (
          <Stack key={tipo} spacing={0.5}>
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
              {label}
            </Typography>
            <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexWrap: "wrap" }}>
              {items.map((item) => (
                <Chip key={item.id} label={item.nome} size={size} variant={variant} color={color} />
              ))}
            </Stack>
          </Stack>
        );
      })}
      {hidden > 0 && <Chip label={`+${hidden}`} size={size} sx={{ alignSelf: "flex-start" }} />}
    </Stack>
  );
}
