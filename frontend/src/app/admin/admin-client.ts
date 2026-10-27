async function csrfToken(): Promise<string> {
  const response = await fetch("/api/v1/auth/csrf", { cache: "no-store" });
  if (!response.ok) throw new Error("csrf");
  return ((await response.json()) as { csrfToken: string }).csrfToken;
}

export async function adminSend(
  method: "POST" | "PATCH" | "PUT" | "DELETE",
  path: string,
  body?: unknown,
  headers?: Record<string, string>,
) {
  return fetch(path, {
    method,
    cache: "no-store",
    headers: {
      "Content-Type": "application/json",
      ...headers,
      "X-CSRF-Token": await csrfToken(),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}
