"use client";

import { useState } from "react";
import type { Category, PricingMode, Product, ProductInput } from "./publishing-client";

const modes: PricingMode[] = ["Free", "Fixed", "PWYW", "Suggested", "OptionalDonation", "FixedPlusDonation"];
const inputClass = "w-full rounded border px-3 py-2";

export function ProductEditor({ product, categories, busy, onDirty, onSave }: {
  product: Product | null;
  categories: Category[];
  busy: boolean;
  onDirty: () => void;
  onSave: (input: ProductInput) => Promise<void>;
}) {
  const [mode, setMode] = useState<PricingMode>(product?.pricing.mode ?? "Free");
  const [selectedCategories, setSelectedCategories] = useState(product?.categoryIds ?? []);
  const fixed = mode === "Fixed" || mode === "FixedPlusDonation";
  const variable = mode === "PWYW" || mode === "Suggested";
  const free = mode === "Free" || mode === "OptionalDonation";
  const categoryChoices = [
    ...categories,
    ...selectedCategories.filter((id) => !categories.some((category) => category.id === id))
      .map((id) => ({ id, name: `Assigned category (${id})`, slug: "", revision: 0 })),
  ];

  async function save(form: FormData) {
    const text = (name: string) => String(form.get(name) ?? "");
    const amount = (name: string) => text(name) === "" ? null : Number(text(name));
    await onSave({
      title: text("title"), slug: text("slug"), summary: text("summary"),
      descriptionHtml: text("descriptionHtml"),
      categoryIds: selectedCategories,
      tags: text("tags").split(",").map((tag) => tag.trim()).filter(Boolean),
      pricing: {
        mode, currency: "NPR",
        fixedPaisa: fixed ? amount("fixedPaisa") : null,
        minimumPaisa: variable ? amount("minimumPaisa") : null,
        suggestedPaisa: variable ? amount("suggestedPaisa") : null,
        allowZero: free || (variable && form.get("allowZero") === "on"),
        contributionEnabled: mode === "OptionalDonation" || (mode === "Free" && form.get("contributionEnabled") === "on"),
      },
      license: { version: Number(text("licenseVersion")), text: text("licenseText") },
      seo: { title: text("seoTitle"), description: text("seoDescription") },
    });
  }

  return (
    <form onSubmit={(event) => {
      event.preventDefault();
      void save(new FormData(event.currentTarget));
    }}>
      <fieldset disabled={busy || product?.state === "Published"} onChange={onDirty} className="flex flex-col gap-4 disabled:opacity-60">
        <legend className="mb-4 text-xl font-semibold">{product ? "Edit product" : "New draft"}</legend>
        {product?.state === "Published" && <p>Unpublish before editing. Existing customer entitlements are not changed here.</p>}
        <label>Title<input name="title" required maxLength={160} defaultValue={product?.title} className={inputClass} /></label>
        <label>Slug<input name="slug" required maxLength={120} pattern="[a-z0-9]+(-[a-z0-9]+)*" defaultValue={product?.slug} className={inputClass} /></label>
        <p className="text-sm">Old product slugs remain permanently reserved after a rename.</p>
        <label>Summary<textarea name="summary" maxLength={500} defaultValue={product?.summary} className={inputClass} /></label>
        <label>Description HTML<textarea name="descriptionHtml" rows={8} defaultValue={product?.descriptionHtml} className={`${inputClass} font-mono`} /></label>
        <p className="text-sm">The API sanitizes descriptions. Save, then open the saved preview to see the accepted version.</p>
        <fieldset className="rounded border p-3">
          <legend>Categories (up to three)</legend>
          {categoryChoices.length === 0 && <p>Create a category below to assign one.</p>}
          {categoryChoices.map((category) => (
            <label key={category.id} className="flex items-center gap-2 py-1">
              <input type="checkbox" checked={selectedCategories.includes(category.id)}
                disabled={!selectedCategories.includes(category.id) && selectedCategories.length >= 3}
                onChange={(event) => setSelectedCategories(event.target.checked
                  ? [...selectedCategories, category.id] : selectedCategories.filter((id) => id !== category.id))} />
              {category.name}
            </label>
          ))}
        </fieldset>
        <label>Tags (comma-separated, up to ten)<input name="tags" defaultValue={product?.tags.join(", ")} className={inputClass} /></label>
        <fieldset className="flex flex-col gap-3 rounded border p-3">
          <legend>Pricing configuration (NPR)</legend>
          <p className="text-sm">Authoring only; this does not enable checkout or payment collection. Amounts are integer paisa (100 paisa = NPR 1).</p>
          <label>Pricing mode<select value={mode} onChange={(event) => {
            const selected = modes.find((value) => value === event.target.value);
            if (selected) setMode(selected);
          }} className={inputClass}>
            {modes.map((value) => <option key={value}>{value}</option>)}
          </select></label>
          {fixed && <label>Fixed price (paisa)<input key={`fixed-${mode}`} name="fixedPaisa" type="number" min={100} max={10000000} step={1} required
            defaultValue={product?.pricing.fixedPaisa ?? ""} className={inputClass} /></label>}
          {variable && <>
            <label>Positive minimum (paisa)<input name="minimumPaisa" type="number" min={100} max={10000000} step={1} required
              defaultValue={product?.pricing.minimumPaisa ?? ""} className={inputClass} /></label>
            <label>Suggested price (paisa)<input name="suggestedPaisa" type="number" min={100} max={10000000} step={1} required={mode === "Suggested"}
              defaultValue={product?.pricing.suggestedPaisa ?? ""} className={inputClass} /></label>
            <label className="flex gap-2"><input name="allowZero" type="checkbox" defaultChecked={product?.pricing.allowZero ?? true} />Allow zero-price access independently of the positive minimum</label>
          </>}
          {mode === "Free" && <label className="flex gap-2"><input name="contributionEnabled" type="checkbox" defaultChecked={product?.pricing.contributionEnabled ?? false} />Offer a separate optional contribution (never required for free access)</label>}
          {mode === "OptionalDonation" && <p>Access remains free. Contributions are separate and optional.</p>}
          {mode === "FixedPlusDonation" && <p>The fixed price is required. Any future voluntary top-up is optional.</p>}
        </fieldset>
        <label>License version<input name="licenseVersion" type="number" min={1} max={2147483647} step={1} required
          defaultValue={product?.license.version ?? 1} className={inputClass} /></label>
        <label>License text<textarea name="licenseText" rows={4} maxLength={10000} defaultValue={product?.license.text} className={inputClass} /></label>
        <p className="text-sm">Increase the license version whenever you change saved license text.</p>
        <label>SEO title<input name="seoTitle" maxLength={160} defaultValue={product?.seo.title} className={inputClass} /></label>
        <label>SEO description<textarea name="seoDescription" maxLength={500} defaultValue={product?.seo.description} className={inputClass} /></label>
        <button type="submit" className="w-fit rounded border px-4 py-2 font-semibold">{busy ? "Working..." : product ? "Save changes" : "Create draft"}</button>
      </fieldset>
    </form>
  );
}

export function ProductPreview({ product }: { product: Product }) {
  return (
    <section aria-label="Saved private preview" className="flex flex-col gap-3 rounded border p-4">
      <h3 className="text-xl font-semibold">Saved private preview</h3>
      <p>Only saved content is shown. This is not a public product page.</p>
      <h4 className="text-lg font-semibold">{product.title}</h4>
      <p>{product.summary}</p>
      <iframe title="Sanitized saved product description" sandbox="" referrerPolicy="no-referrer"
        srcDoc={`<!doctype html><html><head><meta http-equiv="Content-Security-Policy" content="default-src 'none'; base-uri 'none'; form-action 'none'"></head><body>${product.descriptionHtml}</body></html>`}
        className="min-h-64 w-full rounded border bg-white" />
      <p>State: {product.state}. Revision: {product.revision}. Pricing: {product.pricing.mode} (NPR).</p>
      <p className="whitespace-pre-wrap">License v{product.license.version}: {product.license.text || "Not supplied"}</p>
      <p>Tags: {product.tags.join(", ") || "None"}</p>
      <p>Reserved redirects: {product.redirectSlugs.join(", ") || "None"}</p>
      <h4 className="font-semibold">Attachments</h4>
      {product.attachments.length === 0 ? <p>No attachments. Upload and validated publication arrive in EX-09.</p>
        : <ul>{product.attachments.map((asset) => <li key={asset.id}>{asset.name}: {asset.kind}, {asset.status}, {asset.mediaType}, {asset.sizeBytes} bytes</li>)}</ul>}
    </section>
  );
}
