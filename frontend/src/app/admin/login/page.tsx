import { LoginForm } from "./login-form";

export default function AdminLoginPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-6 px-6 py-16">
      <h1 className="text-3xl font-semibold tracking-tight">Administrator sign-in</h1>
      <LoginForm />
    </main>
  );
}
