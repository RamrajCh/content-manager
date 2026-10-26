"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { adminSend } from "../admin-client";

export function LoginForm() {
  const router = useRouter();
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    try {
      const response = await adminSend("POST", "/api/v1/admin/sessions", {
        login: form.get("login"),
        password: form.get("password"),
        code: form.get("code"),
      });
      if (response.ok) {
        router.replace("/admin");
        return;
      }
      setError(
        response.status === 429
          ? "Too many attempts. Wait and try again."
          : "Sign-in failed. Check your details and try again.",
      );
    } catch {
      setError("Sign-in is unavailable. Try again shortly.");
    }
    setBusy(false);
  }

  return (
    <form onSubmit={submit} className="flex max-w-sm flex-col gap-4">
      <label className="flex flex-col gap-1 text-sm">
        Login
        <input name="login" autoComplete="username" required maxLength={256} className="rounded border px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        Password
        <input name="password" type="password" autoComplete="current-password" required maxLength={256} className="rounded border px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        Authenticator or recovery code
        <input name="code" autoComplete="one-time-code" required maxLength={32} className="rounded border px-3 py-2" />
      </label>
      <button type="submit" disabled={busy} className="rounded border px-3 py-2 font-medium">
        {busy ? "Signing in…" : "Sign in"}
      </button>
      <p role="alert" className="min-h-6 text-sm">
        {error}
      </p>
    </form>
  );
}
