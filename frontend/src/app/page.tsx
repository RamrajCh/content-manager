export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-6 px-6 py-16">
      <p className="text-sm font-semibold uppercase tracking-widest">
        Development foundation
      </p>
      <h1 className="text-4xl font-semibold tracking-tight">Content Manager</h1>
      <p className="max-w-xl text-lg leading-8">
        The application foundation is running. Publishing, payments, and downloads
        are not available yet.
      </p>
      <a className="w-fit underline underline-offset-4" href="/api/v1/health/ready">
        Check API readiness
      </a>
      <p className="text-sm">Development only. No purchases or emails are sent.</p>
    </main>
  );
}
