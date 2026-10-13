"use client";

import { Button, Chip, LinearProgress, Paper, Stack, Typography } from "@mui/material";
import { computeProfileProgress } from "@/lib/gamification";
import type { MeResponse } from "@/types/student";

interface ProfileProgressCardProps {
  profile: MeResponse;
}

function scrollToSection(anchorId: string) {
  const target = document.getElementById(anchorId);
  if (!target) return;

  const reduceMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
  target.scrollIntoView({ behavior: reduceMotion ? "auto" : "smooth", block: "start" });
}

export function ProfileProgressCard({ profile }: ProfileProgressCardProps) {
  const progress = computeProfileProgress(profile);
  const isComplete = progress.percent === 100;

  return (
    <Paper
      component="section"
      elevation={0}
      aria-labelledby="profile-progress-title"
      sx={{ p: { xs: 2.5, sm: 3 }, border: 1, borderColor: "divider" }}
    >
      <Stack spacing={2}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ alignItems: { sm: "center" }, justifyContent: "space-between" }}
        >
          <Stack spacing={0.5}>
            <Typography id="profile-progress-title" component="h2" variant="h6">
              Progresso do perfil
            </Typography>
            <Typography color="text.secondary" variant="body2">
              {progress.percent}% concluído · {progress.points} pontos
            </Typography>
          </Stack>
          <Chip color="primary" label={progress.level} size="small" />
        </Stack>

        <LinearProgress
          aria-label="Progresso do perfil"
          aria-valuemax={100}
          aria-valuemin={0}
          aria-valuenow={progress.percent}
          value={progress.percent}
          variant="determinate"
        />

        {isComplete ? (
          <Typography color="text.secondary" variant="body2">
            Perfil completo! Mantenha-o atualizado.
          </Typography>
        ) : (
          <Stack spacing={1.25}>
            {progress.nextLevel && (
              <Typography color="text.secondary" variant="body2">
                Faltam {progress.pointsToNext} pontos para {progress.nextLevel}.
              </Typography>
            )}
            <Typography component="h3" variant="subtitle2">
              Próximos passos
            </Typography>
            <Stack component="ul" spacing={1} sx={{ m: 0, p: 0, listStyle: "none" }}>
              {progress.steps.map((step) => (
                <li key={`${step.anchorId}-${step.text}`}>
                  <Button
                    onClick={() => scrollToSection(step.anchorId)}
                    size="small"
                    sx={{ justifyContent: "flex-start", textAlign: "left" }}
                    variant="text"
                  >
                    {step.text} (+{step.points} pontos)
                  </Button>
                </li>
              ))}
            </Stack>
          </Stack>
        )}
      </Stack>
    </Paper>
  );
}
