import { adminSend } from "./admin-client";

export type ProductState = "Draft" | "Published" | "Unpublished" | "Archived";
export type PricingMode = "Free" | "Fixed" | "PWYW" | "Suggested" | "OptionalDonation" | "FixedPlusDonation";

export interface PricingInput {
  mode: PricingMode;
  currency: "NPR";
  fixedPaisa: number | null;
  minimumPaisa: number | null;
  suggestedPaisa: number | null;
  allowZero: boolean;
  contributionEnabled: boolean;
}

export interface ProductInput {
  title: string;
  slug: string;
  summary: string;
  descriptionHtml: string;
  categoryIds: string[];
  tags: string[];
  pricing: PricingInput;
  license: { version: number; text: string };
  seo: { title: string; description: string };
}

export interface Product extends ProductInput {
  id: string;
  state: ProductState;
  revision: number;
  coverAssetId: string | null;
  assetIds: string[];
  redirectSlugs: string[];
  attachments: { id: string; kind: string; status: string; name: string; mediaType: string; sizeBytes: number }[];
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  revision: number;
}

export interface Page<T> {
  items: T[];
  nextCursor: string | null;
}

export interface PolicySetting {
  key: string;
  value: { enabled: boolean };
  revision: number;
  updatedAt: string | null;
}

export interface AuditEvent {
  id: string;
  action: string;
  actorId: string;
  entityType: string;
  entityId: string;
  at: string;
  reason: string | null;
  fromRevision: number | null;
  toRevision: number | null;
}

export class PublishingError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly fields: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

export async function publishingResult<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body: unknown = response.headers.get("content-type")?.includes("json")
      ? await response.json() : null;
    const fields: Record<string, string[]> = {};
    let title = `Request failed (${response.status}).`;
    if (typeof body === "object" && body !== null) {
      if ("title" in body && typeof body.title === "string") title = body.title;
      if ("errors" in body && typeof body.errors === "object" && body.errors !== null) {
        for (const [field, messages] of Object.entries(body.errors)) {
          if (Array.isArray(messages) && messages.every((item): item is string => typeof item === "string")) {
            fields[field] = messages;
          }
        }
      }
    }
    if (response.status === 401) title = "Your session has expired. Sign in again.";
    if (response.status === 412) title = "This item changed elsewhere. Your edits have not been saved. Reload the saved version before editing again.";
    if (response.status === 403) title = "The request was not authorized. Check that you are using the configured site origin.";
    throw new PublishingError(title, response.status, fields);
  }
  return response.json() as Promise<T>;
}

export async function publishingGet<T>(path: string): Promise<T> {
  return publishingResult<T>(await fetch(`/api/v1/admin/${path}`, { cache: "no-store" }));
}

export async function publishingWrite<T>(
  method: "POST" | "PATCH" | "PUT" | "DELETE",
  path: string,
  body?: unknown,
  headers?: Record<string, string>,
): Promise<T | undefined> {
  const response = await adminSend(method, `/api/v1/admin/${path}`, body, headers);
  if (response.status === 204) return undefined;
  return publishingResult<T>(response);
}

export function revisionHeader(revision: number) {
  return { "If-Match": `"${revision}"` };
}
