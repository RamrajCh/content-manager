"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  PublishingError, publishingGet, publishingWrite, revisionHeader,
  type AuditEvent, type Page, type PolicySetting,
} from "./publishing-client";

const buttonClass = "w-fit rounded border px-3 py-2 disabled:opacity-50";

function standaloneSetting(page: Page<PolicySetting>) {
  const setting = page.items.find((item) => item.key === "standaloneDonationsEnabled");
  if (!setting) throw new PublishingError("The standalone donation setting is unavailable. Reload after rebuilding the API.", 503);
  return setting;
}

export function PublishingSettings() {
  const router = useRouter();
  const [setting, setSetting] = useState<PolicySetting | null>(null);
  const [enabled, setEnabled] = useState(false);
  const [audit, setAudit] = useState<Page<AuditEvent>>({ items: [], nextCursor: null });
  const [auditQuery, setAuditQuery] = useState("");
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState<PublishingError | null>(null);
  const [notice, setNotice] = useState("");
  const dirty = setting !== null && enabled !== setting.value.enabled;

  useEffect(() => {
    let active = true;
    Promise.all([
      publishingGet<Page<PolicySetting>>("settings"),
      publishingGet<Page<AuditEvent>>("audit-events?limit=20"),
    ]).then(([settings, events]) => {
      const current = standaloneSetting(settings);
      if (!active) return;
      setSetting(current); setEnabled(current.value.enabled); setAudit(events);
    }).catch((cause: unknown) => {
      if (!active) return;
      const failure = cause instanceof PublishingError ? cause
        : new PublishingError("Unable to load settings and audit events. Retry after checking the API.", 0);
      setError(failure);
      if (failure.status === 401) router.replace("/admin/login");
    }).finally(() => { if (active) setBusy(false); });
    return () => { active = false; };
  }, [router]);

  useEffect(() => {
    if (!dirty) return;
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  async function run(action: () => Promise<void>) {
    setBusy(true); setError(null); setNotice("");
    try { await action(); }
    catch (cause) {
      const failure = cause instanceof PublishingError ? cause
        : new PublishingError("The settings/audit service is unavailable. A write may have completed; reload to check its saved value before retrying.", 0);
      setError(failure);
      if (failure.status === 401) router.replace("/admin/login");
    }
    finally { setBusy(false); }
  }

  async function reload() {
    if (dirty && !window.confirm("Discard the unsaved standalone donation policy change?")) return;
    await run(async () => {
      const [settings, events] = await Promise.all([
        publishingGet<Page<PolicySetting>>("settings"),
        publishingGet<Page<AuditEvent>>(`audit-events?limit=20${auditQuery}`),
      ]);
      const current = standaloneSetting(settings);
      setSetting(current); setEnabled(current.value.enabled); setAudit(events);
      setNotice("Saved settings and projected audit events reloaded.");
    });
  }

  return (
    <section className="flex flex-col gap-5 border-t pt-6" aria-label="Settings and audit" aria-busy={busy}>
      <h2 className="text-2xl font-semibold">Settings and audit</h2>
      <div role="alert">
        {error && <>
          <p>{error.message}</p>
          <ul>{Object.entries(error.fields).map(([field, messages]) => <li key={field}>{field}: {messages.join(" ")}</li>)}</ul>
        </>}
      </div>
      <p role="status">{busy ? "Working..." : notice}</p>
      <button disabled={busy} onClick={reload} className={buttonClass}>Reload settings and audit</button>
      {setting && <form onSubmit={(event) => {
        event.preventDefault();
        void run(async () => {
          const saved = await publishingWrite<PolicySetting>("PUT", `settings/${setting.key}`,
            { value: { enabled } }, revisionHeader(setting.revision));
          if (!saved) throw new Error("Missing saved setting");
          setSetting(saved); setEnabled(saved.value.enabled);
          setNotice("Policy saved. No checkout or payment collection is enabled by this setting.");
        });
      }}>
        <fieldset disabled={busy} className="flex flex-col gap-3">
          <legend className="mb-3 text-xl font-semibold">Standalone donation policy</legend>
          <p>This policy defaults off. It only governs future standalone support requests, not free access or product-linked optional contributions. Checkout and payment collection are not implemented.</p>
          <label className="flex items-start gap-2"><input type="checkbox" checked={enabled} onChange={(event) => setEnabled(event.target.checked)} />
            Allow standalone donations when the donation workflow is available
          </label>
          <p>Saved revision: {setting.revision}. Saved value: {setting.value.enabled ? "Enabled" : "Disabled"}.{dirty ? " Unsaved change." : ""}</p>
          <button type="submit" className={buttonClass} disabled={!dirty}>Save policy</button>
        </fieldset>
      </form>}
      <section className="flex flex-col gap-3" aria-label="Audit events">
        <h3 className="text-xl font-semibold">Audit events</h3>
        <p>Newest projected events first. Default range: last seven days. Product/category/settings events are projected periodically; offline owner audit events require their documented projection command. Refresh to see newly projected events.</p>
        <form className="flex flex-wrap items-end gap-3" onSubmit={(event) => {
          event.preventDefault();
          const form = new FormData(event.currentTarget);
          const query = new URLSearchParams();
          const since = String(form.get("since") ?? "");
          const until = String(form.get("until") ?? "");
          if (since) query.set("since", `${since}T00:00:00Z`);
          if (until) query.set("until", `${until}T23:59:59.999Z`);
          const suffix = query.size ? `&${query.toString()}` : "";
          void run(async () => {
            const page = await publishingGet<Page<AuditEvent>>(`audit-events?limit=20${suffix}`);
            setAudit(page); setAuditQuery(suffix);
          });
        }}>
          <label>From (UTC)<input name="since" type="date" disabled={busy} className="block rounded border p-2" /></label>
          <label>Through (UTC)<input name="until" type="date" disabled={busy} className="block rounded border p-2" /></label>
          <button type="submit" disabled={busy} className={buttonClass}>Apply range (up to 31 days)</button>
        </form>
        {!busy && audit.items.length === 0 && <p>No audit events loaded for this range.</p>}
        <ol className="flex flex-col gap-3">
          {audit.items.map((item) => <li key={item.id} className="break-words rounded border p-3">
            <p><time dateTime={item.at}>{item.at}</time> - <strong>{item.action}</strong></p>
            <p>Entity: {item.entityType} / {item.entityId}</p>
            <p>Actor: {item.actorId}</p>
            {item.fromRevision !== null && <p>Revision: {item.fromRevision} to {item.toRevision}</p>}
            {item.reason && <p className="whitespace-pre-wrap">Reason: {item.reason}</p>}
          </li>)}
        </ol>
        {audit.nextCursor && <button disabled={busy} className={buttonClass} onClick={() => run(async () => {
          const page = await publishingGet<Page<AuditEvent>>(`audit-events?limit=20${auditQuery}&cursor=${encodeURIComponent(audit.nextCursor!)}`);
          setAudit((current) => ({ items: [...current.items, ...page.items.filter((item) => !current.items.some((old) => old.id === item.id))], nextCursor: page.nextCursor }));
        })}>Load older events</button>}
      </section>
    </section>
  );
}
