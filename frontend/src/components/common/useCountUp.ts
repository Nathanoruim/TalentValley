"use client";

import { useEffect, useRef, useState } from "react";
import { animate } from "framer-motion";
import { duration, ease } from "@/components/auth/motion/tokens";
import { useMotionPolicy } from "@/components/auth/motion/useMotionPolicy";

const COUNT_DURATION = duration.slow * 1.5;

/**
 * Returns the number to show: it counts up to `value` and follows later changes.
 * With "reduce motion" (and before hydration) it is simply `value`.
 */
export function useCountUp(value: number): number {
  const policy = useMotionPolicy();
  const animated = policy === "touch" || policy === "pointer";
  const [counted, setCounted] = useState(0);
  const latestRef = useRef(0);

  useEffect(() => {
    if (!animated) return;
    const controls = animate(latestRef.current, value, {
      duration: COUNT_DURATION,
      ease: ease.outExpo,
      onUpdate: (latest) => {
        latestRef.current = latest;
        setCounted(Math.round(latest));
      },
    });
    return () => controls.stop();
  }, [animated, value]);

  return animated ? counted : value;
}
