import { authFetch } from "@/lib/api";

export type Claim = { type: string; value: string };
export type CurrentUser = {
  email: string | null;
  fullName: string | null;
  roles: string[];
};

const ROLE_CLAIM_TYPE = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export async function getCurrentUser(): Promise<CurrentUser | null> {
  const res = await authFetch("/api/auth/me");
  if (!res.ok) return null;

  const data: { email: string | null; claims: Claim[] } = await res.json();
  const fullName = data.claims.find((c) => c.type === "fullName")?.value ?? null;
  const roles = data.claims.filter((c) => c.type === ROLE_CLAIM_TYPE).map((c) => c.value);

  return { email: data.email, fullName, roles };
}

export function isAdmin(user: CurrentUser | null): boolean {
  return Boolean(user?.roles.includes("Admin"));
}