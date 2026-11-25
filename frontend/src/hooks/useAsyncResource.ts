import { useCallback, useEffect, useState } from "react";

/**
 * Loading → ready | error, as one value, so a page can never render stale data
 * together with an error or forget the loading case.
 */
export type AsyncResource<T> =
  | { status: "loading" }
  | { status: "error"; error: unknown }
  | { status: "ready"; data: T };

/**
 * Runs `load` on mount (and on `reload()`), aborting the request when the
 * component unmounts or `load` changes. `load` must be stable: a module-level
 * function or one wrapped in `useCallback`.
 */
export function useAsyncResource<T>(load: (signal: AbortSignal) => Promise<T>) {
  const [resource, setResource] = useState<AsyncResource<T>>({ status: "loading" });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    load(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) setResource({ status: "ready", data });
      },
      (error: unknown) => {
        if (!controller.signal.aborted) setResource({ status: "error", error });
      },
    );
    return () => controller.abort();
  }, [load, attempt]);

  const reload = useCallback(() => {
    setResource({ status: "loading" });
    setAttempt((current) => current + 1);
  }, []);

  /** Updates loaded data in place (e.g. after a successful save). */
  const update = useCallback((change: (current: T) => T) => {
    setResource((current) => current.status === "ready"
      ? { status: "ready", data: change(current.data) }
      : current);
  }, []);

  return { resource, reload, update };
}
