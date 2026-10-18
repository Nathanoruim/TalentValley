"use client";

import { useCallback, useSyncExternalStore } from "react";

const STORAGE_KEY = "tv-sidebar-collapsed";
const listeners = new Set<() => void>();
// Used only when the browser blocks storage, so the toggle still works until the page reloads.
let memoryValue = false;

function subscribe(onChange: () => void) {
  listeners.add(onChange);
  window.addEventListener("storage", onChange);
  return () => {
    listeners.delete(onChange);
    window.removeEventListener("storage", onChange);
  };
}

function readCollapsed(): boolean {
  try {
    return window.localStorage.getItem(STORAGE_KEY) === "1";
  } catch {
    return memoryValue;
  }
}

/**
 * Whether the desktop menu is collapsed to icons. Only a screen convenience:
 * it is remembered in this browser and never leaves it. The server and the
 * first render always show the menu expanded.
 */
export function useSidebarCollapsed(): [boolean, (collapsed: boolean) => void] {
  const collapsed = useSyncExternalStore(subscribe, readCollapsed, () => false);

  const setCollapsed = useCallback((next: boolean) => {
    try {
      window.localStorage.setItem(STORAGE_KEY, next ? "1" : "0");
    } catch {
      // Blocked storage: memoryValue keeps the choice until the page reloads.
      memoryValue = next;
    }
    listeners.forEach((listener) => listener());
  }, []);

  return [collapsed, setCollapsed];
}
