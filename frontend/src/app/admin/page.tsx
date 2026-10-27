import { AdminHome } from "./admin-home";

export default function AdminPage() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col gap-6 px-6 py-16">
      <h1 className="text-3xl font-semibold tracking-tight">Administration</h1>
      <AdminHome />
    </main>
  );
}
