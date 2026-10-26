"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { adminSend } from "./admin-client";

export function AdminHome() {
  const router = useRouter();
  const [login, setLogin] = useState<string | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    fetch("/api/v1/admin/sessions/current", { cache: "no-store" })
      .then(async (response) => {
        if (response.status === 401) {
          router.replace("/admin/login");
          return;
        }
        if (!response.ok) throw new Error("session unavailable");
        setLogin(((await response.json()) as { login: string }).login);
      })
      .catch(() => setError("Unable to check your session. Reload to try again."));
  }, [router]);

  async function signOut() {
    setBusy(true);
    setError("");
    try {
      const response = await adminSend("DELETE", "/api/v1/admin/sessions/current");
      if (response.ok || response.status === 401) {
        router.replace("/admin/login");
        return;
      }
      setError("Sign-out failed. Your session may still be active. Try again.");
    } catch {
      setError("Sign-out is unavailable. Your session may still be active. Try again.");
    }
    setBusy(false);
  }

  if (login === null) return <p role="status">{error || "Checking session…"}</p>;
  return (
    <>
      <p>Signed in as {login}. Publishing tools are not available yet.</p>
      <button onClick={signOut} disabled={busy} className="w-fit rounded border px-3 py-2 font-medium">
        {busy ? "Signing out…" : "Sign out"}
      </button>
      <p role="alert">{error}</p>
    </>
  );
}
