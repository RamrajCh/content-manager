"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ProductEditor, ProductPreview } from "./product-editor";
import {
  PublishingError, publishingGet, publishingWrite, revisionHeader,
  type Category, type Page, type Product, type ProductInput, type ProductState,
} from "./publishing-client";

const buttonClass = "rounded border px-3 py-2 disabled:opacity-50";

export function PublishingWorkspace() {
  const router = useRouter();
  const [products, setProducts] = useState<Page<Product>>({ items: [], nextCursor: null });
  const [categories, setCategories] = useState<Page<Category>>({ items: [], nextCursor: null });
  const [product, setProduct] = useState<Product | null>(null);
  const [category, setCategory] = useState<Category | null>(null);
  const [filter, setFilter] = useState("");
  const [busy, setBusy] = useState(true);
  const [loaded, setLoaded] = useState(false);
  const [error, setError] = useState<PublishingError | null>(null);
  const [notice, setNotice] = useState("");
  const [dirty, setDirty] = useState(false);
  const [categoryDirty, setCategoryDirty] = useState(false);
  const [editorVersion, setEditorVersion] = useState(0);
  const [categoryEditorVersion, setCategoryEditorVersion] = useState(0);
  const [preview, setPreview] = useState<Product | null>(null);
  const [reason, setReason] = useState("");
  const productCreate = useRef<{ body: string; key: string } | null>(null);
  const categoryCreate = useRef<{ body: string; key: string } | null>(null);

  function report(cause: unknown) {
    const failure = cause instanceof PublishingError ? cause
      : new PublishingError("The publishing service is unavailable. Your changes may not have reached the server. Retry the same request or reload the saved item to check.", 0);
    setError(failure);
    if (failure.status === 401) router.replace("/admin/login");
  }

  useEffect(() => {
    let active = true;
    Promise.all([
      publishingGet<Page<Product>>("products?limit=20"),
      publishingGet<Page<Category>>("categories?limit=50"),
    ]).then(([nextProducts, nextCategories]) => {
      if (!active) return;
      setProducts(nextProducts);
      setCategories(nextCategories);
      setLoaded(true);
    }).catch((cause: unknown) => {
      if (!active) return;
      const failure = cause instanceof PublishingError ? cause : new PublishingError("Unable to load publishing data. Retry to check the service.", 0);
      setError(failure);
      if (failure.status === 401) router.replace("/admin/login");
    }).finally(() => { if (active) setBusy(false); });
    return () => { active = false; };
  }, [router]);

  useEffect(() => {
    if (!dirty && !categoryDirty) return;
    const warn = (event: BeforeUnloadEvent) => { event.preventDefault(); };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty, categoryDirty]);

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError(null);
    setNotice("");
    try { await action(); }
    catch (cause) { report(cause); }
    finally { setBusy(false); }
  }

  function discard() {
    return !dirty || window.confirm("Discard unsaved product changes?");
  }

  function selectProduct(next: Product | null) {
    setProduct(next);
    setDirty(false);
    setPreview(null);
    setReason("");
    setEditorVersion((version) => version + 1);
    productCreate.current = null;
  }

  function updateProduct(next: Product) {
    setProducts((current) => ({ ...current, items: current.items
      .map((item) => item.id === next.id ? next : item)
      .filter((item) => !filter || item.state === filter) }));
    selectProduct(next);
  }

  function selectCategory(next: Category | null) {
    if (categoryDirty && !window.confirm("Discard unsaved category changes?")) return;
    setCategory(next);
    setCategoryDirty(false);
    setCategoryEditorVersion((version) => version + 1);
    categoryCreate.current = null;
  }

  function createHeaders(ref: { current: { body: string; key: string } | null }, input: unknown) {
    const body = JSON.stringify(input);
    if (ref.current?.body !== body) ref.current = { body, key: crypto.randomUUID() };
    return { "Idempotency-Key": ref.current.key };
  }

  async function saveProduct(input: ProductInput) {
    await run(async () => {
      const saved = await publishingWrite<Product>(product ? "PATCH" : "POST",
        product ? `products/${product.id}` : "products", input,
        product ? revisionHeader(product.revision) : createHeaders(productCreate, input));
      if (!saved) throw new Error("Missing saved product");
      const creating = product === null;
      updateProduct(saved);
      setNotice("Product saved. Open the saved preview to review sanitized content.");
      if (creating) {
        const page = await publishingGet<Page<Product>>("products?limit=20");
        setProducts(page);
        setFilter("");
      }
    });
  }

  async function transition(to: ProductState) {
    if (!product || !discard()) return;
    if (!window.confirm(`Change this product to ${to}?${to === "Archived" ? " Archiving cannot be undone here." : ""}`)) return;
    await run(async () => {
      const saved = await publishingWrite<Product>("POST", `products/${product.id}/transitions`,
        { to, reason: reason || null }, revisionHeader(product.revision));
      if (!saved) throw new Error("Missing product transition");
      updateProduct(saved);
      setNotice(`Product is now ${saved.state}.`);
    });
  }

  async function reloadProduct() {
    if (!product || !discard()) return;
    await run(async () => updateProduct(await publishingGet<Product>(`products/${product.id}`)));
  }

  async function saveCategory(form: FormData) {
    const input = { name: String(form.get("name") ?? ""), slug: String(form.get("slug") ?? "") };
    await run(async () => {
      const saved = await publishingWrite<Category>(category ? "PATCH" : "POST",
        category ? `categories/${category.id}` : "categories", input,
        category ? revisionHeader(category.revision) : createHeaders(categoryCreate, input));
      if (!saved) throw new Error("Missing saved category");
      setCategories((current) => ({ ...current, items: current.items.some((item) => item.id === saved.id)
        ? current.items.map((item) => item.id === saved.id ? saved : item) : [...current.items, saved] }));
      setCategory(saved);
      setCategoryDirty(false);
      setCategoryEditorVersion((version) => version + 1);
      categoryCreate.current = null;
      setNotice("Category saved.");
    });
  }

  return (
    <section className="flex flex-col gap-6" aria-label="Publishing" aria-busy={busy}>
      <h2 className="text-2xl font-semibold">Publishing</h2>
      <p>Private owner workspace. Payments and publication are not enabled by these tools.</p>
      <div role="alert" aria-live="assertive">
        {error && <>
          <p>{error.message}</p>
          <ul>{Object.entries(error.fields).map(([field, messages]) => <li key={field}><strong>{field}:</strong> {messages.join(" ")}</li>)}</ul>
        </>}
      </div>
      <p role="status">{busy ? "Working..." : notice}</p>
      {!loaded ? <button disabled={busy} className={buttonClass} onClick={() => run(async () => {
        const [nextProducts, nextCategories] = await Promise.all([
          publishingGet<Page<Product>>("products?limit=20"), publishingGet<Page<Category>>("categories?limit=50"),
        ]);
        setProducts(nextProducts); setCategories(nextCategories); setLoaded(true);
      })}>Retry loading publishing data</button> : <>
        <section className="flex flex-col gap-3" aria-label="Products">
          <h3 className="text-xl font-semibold">Products</h3>
          <div className="flex flex-wrap items-center gap-3">
            <label>State filter<select value={filter} disabled={busy} className="ml-2 rounded border p-2" onChange={(event) => {
              const next = event.target.value;
              void run(async () => {
                const page = await publishingGet<Page<Product>>(`products?limit=20${next ? `&state=${next}` : ""}`);
                setProducts(page); setFilter(next);
              });
            }}>
              <option value="">All states</option>
              {["Draft", "Published", "Unpublished", "Archived"].map((state) => <option key={state}>{state}</option>)}
            </select></label>
            <button disabled={busy} className={buttonClass} onClick={() => { if (discard()) selectProduct(null); }}>New draft</button>
          </div>
          {products.items.length === 0 ? <p>No products in this view.</p> : <ul className="flex flex-col gap-2">
            {products.items.map((item) => <li key={item.id}>
              <button disabled={busy} className={`${buttonClass} text-left`} onClick={() => {
                if (!discard()) return;
                void run(async () => selectProduct(await publishingGet<Product>(`products/${item.id}`)));
              }}>{item.title} - {item.state}{product?.id === item.id ? " (selected)" : ""}</button>
            </li>)}
          </ul>}
          {products.nextCursor && <button disabled={busy} className={`${buttonClass} w-fit`} onClick={() => run(async () => {
            const page = await publishingGet<Page<Product>>(`products?limit=20${filter ? `&state=${filter}` : ""}&cursor=${encodeURIComponent(products.nextCursor!)}`);
            setProducts((current) => ({ items: [...current.items, ...page.items.filter((item) => !current.items.some((old) => old.id === item.id))], nextCursor: page.nextCursor }));
          })}>Load more products</button>}
        </section>
        <ProductEditor key={`${product?.id ?? "new"}:${editorVersion}`} product={product} categories={categories.items}
          busy={busy} onDirty={() => setDirty(true)} onSave={saveProduct} />
        {product && <section className="flex flex-col gap-3" aria-label="Product lifecycle">
          <p>State: {product.state}. Revision: {product.revision}.{dirty ? " Unsaved changes." : ""}</p>
          <label>Transition reason (optional)<input maxLength={500} value={reason} disabled={busy}
            onChange={(event) => setReason(event.target.value)} className="w-full rounded border px-3 py-2" /></label>
          <div className="flex flex-wrap gap-3">
            <button disabled={busy} className={buttonClass} onClick={reloadProduct}>Reload saved product</button>
            <button disabled={busy} className={buttonClass} onClick={() => run(async () => {
              const saved = await publishingGet<Product>(`products/${product.id}`);
              setPreview(saved);
            })}>Show saved preview</button>
            <button disabled className={buttonClass}>Publish (requires EX-09)</button>
            {product.state === "Published" && <button disabled={busy} className={buttonClass} onClick={() => transition("Unpublished")}>Unpublish</button>}
            {product.state !== "Archived" && <button disabled={busy} className={buttonClass} onClick={() => transition("Archived")}>Archive</button>}
            {product.state === "Draft" && <button disabled={busy} className={buttonClass} onClick={() => {
              if (!discard() || !window.confirm("Delete this draft? Referenced drafts cannot be deleted; its slug remains reserved.")) return;
              void run(async () => {
                await publishingWrite("DELETE", `products/${product.id}`, undefined, revisionHeader(product.revision));
                setProducts((current) => ({ ...current, items: current.items.filter((item) => item.id !== product.id) }));
                selectProduct(null); setNotice("Draft deleted; its slug remains reserved.");
              });
            }}>Delete draft</button>}
          </div>
        </section>}
        {preview && <ProductPreview product={preview} />}
        <section className="flex flex-col gap-3 border-t pt-6" aria-label="Categories">
          <h3 className="text-xl font-semibold">Categories</h3>
          <p>Categories referenced by products cannot be deleted. Reassign products first.</p>
          <ul className="flex flex-wrap gap-2">
            {categories.items.map((item) => <li key={item.id}><button disabled={busy} className={buttonClass} onClick={() => run(async () => {
              selectCategory(await publishingGet<Category>(`categories/${item.id}`));
            })}>{item.name}{category?.id === item.id ? " (selected)" : ""}</button></li>)}
          </ul>
          {categories.nextCursor && <button disabled={busy} className={`${buttonClass} w-fit`} onClick={() => run(async () => {
            const page = await publishingGet<Page<Category>>(`categories?limit=50&cursor=${encodeURIComponent(categories.nextCursor!)}`);
            setCategories((current) => ({ items: [...current.items, ...page.items.filter((item) => !current.items.some((old) => old.id === item.id))], nextCursor: page.nextCursor }));
          })}>Load more categories</button>}
          <button disabled={busy} className={`${buttonClass} w-fit`} onClick={() => selectCategory(null)}>New category</button>
          <form onSubmit={(event) => {
            event.preventDefault();
            void saveCategory(new FormData(event.currentTarget));
          }} key={`${category?.id ?? "new"}:${categoryEditorVersion}`}>
            <fieldset disabled={busy} onChange={() => setCategoryDirty(true)} className="flex flex-col gap-3">
              <legend>{category ? `Edit category (revision ${category.revision})` : "Create category"}</legend>
              <label>Name<input name="name" required maxLength={80} defaultValue={category?.name} className="w-full rounded border px-3 py-2" /></label>
              <label>Slug<input name="slug" required maxLength={100} pattern="[a-z0-9]+(-[a-z0-9]+)*" defaultValue={category?.slug} className="w-full rounded border px-3 py-2" /></label>
              <button className={`${buttonClass} w-fit`} type="submit">Save category</button>
            </fieldset>
          </form>
          {category && <div className="flex flex-wrap gap-3">
            <button disabled={busy} className={buttonClass} onClick={() => run(async () => {
              selectCategory(await publishingGet<Category>(`categories/${category.id}`));
            })}>Reload saved category</button>
            <button disabled={busy} className={buttonClass} onClick={() => {
              if (!window.confirm(`Delete category "${category.name}"?`)) return;
              void run(async () => {
                await publishingWrite("DELETE", `categories/${category.id}`, undefined, revisionHeader(category.revision));
                setCategories((current) => ({ ...current, items: current.items.filter((item) => item.id !== category.id) }));
                setCategory(null); setCategoryDirty(false); setCategoryEditorVersion((version) => version + 1);
                setNotice("Category deleted.");
              });
            }}>Delete category</button>
          </div>}
        </section>
      </>}
    </section>
  );
}
