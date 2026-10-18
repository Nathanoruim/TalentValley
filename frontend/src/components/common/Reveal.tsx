"use client";

import type { ReactNode } from "react";
import { motion } from "framer-motion";
import { duration, ease } from "@/components/auth/motion/tokens";
import { useMotionPolicy } from "@/components/auth/motion/useMotionPolicy";

const STAGGER_STEP = 0.05;
const MAX_STAGGER_STEPS = 8;

interface RevealProps {
  children: ReactNode;
  /** Position of the item in a group: later items enter a little after the earlier ones. */
  index?: number;
  className?: string;
}

/**
 * Soft entrance (fade + short rise) shared by the cards of the signed-in screens.
 * With "reduce motion" it is only a short fade; before hydration the content is simply visible.
 */
export function Reveal({ children, index = 0, className }: RevealProps) {
  const policy = useMotionPolicy();
  const reduced = policy === "reduced";
  const delay = reduced ? 0 : Math.min(index, MAX_STAGGER_STEPS) * STAGGER_STEP;

  return (
    <motion.div
      className={className}
      initial={policy === "pending" ? false : { opacity: 0, y: reduced ? 0 : 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: reduced ? duration.fast : duration.base, ease: ease.outExpo, delay }}
    >
      {children}
    </motion.div>
  );
}
