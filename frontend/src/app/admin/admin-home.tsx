"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { adminSend } from "./admin-client";

export function AdminHome() {
  const router = useRouter();
  const [login, setLogin] = useState<string | null>(null);

  useEffect(() => {
    fetch("/api/v1/admin/sessions/current", { cache: "no-store" })
      .then(async (response) => {
        if (!response.ok) {
          router.replace("/admin/login");
          return;
        }
        setLogin(((await response.json()) as { login: string }).login);
      })
      .catch(() => router.replace("/admin/login"));
  }, [router]);

  async function signOut() {
    await adminSend("DELETE", "/api/v1/admin/sessions/current").catch(() => undefined);
    router.replace("/admin/login");
  }

  if (login === null) return <p>Checking session…</p>;
  return (
    <>
      <p>Signed in as {login}. Publishing tools are not available yet.</p>
      <button onClick={signOut} className="w-fit rounded border px-3 py-2 font-medium">
        Sign out
      </button>
    </>
  );
}
