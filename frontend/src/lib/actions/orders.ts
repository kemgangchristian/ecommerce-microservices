"use server";

import { redirect } from "next/navigation";
import { authFetch } from "@/lib/api";
import { getCurrentUser } from "@/lib/auth";

export async function createOrder(formData: FormData) {
  const productId = formData.get("productId") as string;
  const productName = formData.get("productName") as string;
  const unitPrice = Number(formData.get("unitPrice"));
  const quantity = Number(formData.get("quantity"));

  const user = await getCurrentUser();
  if (!user?.email) {
    redirect(`/products?error=${encodeURIComponent("Utilisateur non authentifié.")}`);
  }

  const res = await authFetch("/api/orders", {
    method: "POST",
    body: JSON.stringify({
      customerEmail: user.email,
      items: [{ productId, productName, quantity, unitPrice }],
    }),
  });

  if (!res.ok) {
    const err = await res.text();
    redirect(`/products?error=${encodeURIComponent(err || "Échec de la commande")}`);
  }

  redirect("/orders");
}