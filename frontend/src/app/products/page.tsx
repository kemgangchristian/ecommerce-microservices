import { cookies } from "next/headers";
import { authFetch } from "@/lib/api";
import type { Product } from "@/lib/types";
import { createOrder } from "@/lib/actions/orders";
import { createProduct, updateProduct, deleteProduct } from "@/lib/actions/products";
import { getCurrentUser, isAdmin } from "@/lib/auth";

export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const { error } = await searchParams;
  const res = await authFetch("/api/products");
  const products: Product[] = await res.json();
  const cookieStore = await cookies();
  const isAuthenticated = Boolean(cookieStore.get("token")?.value);
  const user = isAuthenticated ? await getCurrentUser() : null;
  const admin = isAdmin(user);

  return (
    <div className="max-w-3xl mx-auto mt-12 space-y-6">
      <h1 className="text-2xl font-bold">Produits</h1>
      {error && <p className="text-red-600 text-sm">{decodeURIComponent(error)}</p>}

      {admin && (
        <form action={createProduct} className="border p-4 space-y-2">
          <h2 className="font-semibold">Ajouter un produit</h2>
          <input name="name" placeholder="Nom" required className="border p-2 w-full" />
          <input name="description" placeholder="Description" required className="border p-2 w-full" />
          <input name="price" type="number" step="0.01" placeholder="Prix" required className="border p-2 w-full" />
          <input name="stockQuantity" type="number" placeholder="Stock" required className="border p-2 w-full" />
          <button type="submit" className="bg-black text-white px-4 py-2">Créer</button>
        </form>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {products.map((product) => (
          <div key={product.id} className="border p-4 space-y-2">
            <h2 className="font-semibold">{product.name}</h2>
            <p className="text-sm text-gray-600">{product.description}</p>
            <p className="font-bold">{product.price} €</p>
            <p className="text-sm">
              {product.stockQuantity > 0 ? `${product.stockQuantity} en stock` : "Rupture de stock"}
            </p>

            {isAuthenticated && product.stockQuantity > 0 && (
              <form action={createOrder} className="flex gap-2 items-center">
                <input type="hidden" name="productId" value={product.id} />
                <input type="hidden" name="productName" value={product.name} />
                <input type="hidden" name="unitPrice" value={product.price} />
                <input type="number" name="quantity" min={1} max={product.stockQuantity} defaultValue={1} className="border p-1 w-16" />
                <button type="submit" className="bg-black text-white px-3 py-1">Commander</button>
              </form>
            )}

            {admin && (
              <details className="text-sm">
                <summary className="cursor-pointer text-gray-600">Modifier</summary>
                <form action={updateProduct} className="space-y-2 mt-2">
                  <input type="hidden" name="id" value={product.id} />
                  <input name="name" defaultValue={product.name} className="border p-1 w-full" />
                  <input name="description" defaultValue={product.description} className="border p-1 w-full" />
                  <input name="price" type="number" step="0.01" defaultValue={product.price} className="border p-1 w-full" />
                  <input name="stockQuantity" type="number" defaultValue={product.stockQuantity} className="border p-1 w-full" />
                  <button type="submit" className="bg-black text-white px-3 py-1">Enregistrer</button>
                </form>
                <form action={deleteProduct} className="mt-2">
                  <input type="hidden" name="id" value={product.id} />
                  <button type="submit" className="text-red-600">Supprimer</button>
                </form>
              </details>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}