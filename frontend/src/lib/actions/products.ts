"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { authFetch } from "@/lib/api";

function productErrorRedirect(message: string): never {
  redirect(`/products?error=${encodeURIComponent(message)}`);
}

export async function createProduct(formData: FormData) {
  const name = formData.get("name") as string;
  const description = formData.get("description") as string;
  const price = Number(formData.get("price"));
  const stockQuantity = Number(formData.get("stockQuantity"));

  const res = await authFetch("/api/products", {
    method: "POST",
    body: JSON.stringify({ name, description, price, stockQuantity }),
  });

  if (!res.ok) {
    const err = await res.text();
    productErrorRedirect(err || "Échec de la création du produit.");
  }

  revalidatePath("/products");
}

export async function updateProduct(formData: FormData) {
  const id = formData.get("id") as string;
  const name = formData.get("name") as string;
  const description = formData.get("description") as string;
  const price = Number(formData.get("price"));
  const stockQuantity = Number(formData.get("stockQuantity"));

  // PATCH : mise à jour partielle côté backend, on envoie tous les champs du formulaire.
  const res = await authFetch(`/api/products/${id}`, {
    method: "PATCH",
    body: JSON.stringify({ name, description, price, stockQuantity }),
  });

  if (!res.ok) {
    const err = await res.text();
    productErrorRedirect(err || "Échec de la mise à jour du produit.");
  }

  revalidatePath("/products");
}

export async function deleteProduct(formData: FormData) {
  const id = formData.get("id") as string;

  const res = await authFetch(`/api/products/${id}`, { method: "DELETE" });

  if (!res.ok) {
    productErrorRedirect("Échec de la suppression du produit.");
  }

  revalidatePath("/products");
}