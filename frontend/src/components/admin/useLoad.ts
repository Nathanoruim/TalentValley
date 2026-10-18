"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { getApiErrorMessage } from "@/lib/api";

export function useLoad<T>(
  loader: () => Promise<T>,
  dependencies: readonly unknown[],
) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const loaderRef = useRef(loader);
  const requestIdRef = useRef(0);
  const mountedRef = useRef(true);
  const key = JSON.stringify(dependencies);
  useEffect(() => {
    loaderRef.current = loader;
  }, [loader]);
  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);
  const run = useCallback((requestId: number) => {
    loaderRef
      .current()
      .then((result) => {
        if (mountedRef.current && requestId === requestIdRef.current)
          setData(result);
      })
      .catch((e) => {
        if (mountedRef.current && requestId === requestIdRef.current)
          setError(
            getApiErrorMessage(e, "Não foi possível carregar os dados."),
          );
      })
      .finally(() => {
        if (mountedRef.current && requestId === requestIdRef.current)
          setLoading(false);
      });
  }, []);
  const reload = useCallback(() => {
    requestIdRef.current += 1;
    setLoading(true);
    setError(null);
    run(requestIdRef.current);
  }, [run]);
  // Reset loading/error when the request parameters change. React recommends
  // adjusting state during render instead of setting it inside an effect body.
  const [prevKey, setPrevKey] = useState(key);
  if (key !== prevKey) {
    setPrevKey(key);
    setLoading(true);
    setError(null);
  }
  useEffect(() => {
    requestIdRef.current += 1;
    run(requestIdRef.current);
  }, [key, run]);
  return { data, loading, error, reload };
}
