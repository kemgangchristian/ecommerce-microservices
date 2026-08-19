import { authFetch } from "@/lib/api";
import type { Order } from "@/lib/types";
import { ORDER_STATUS_LABEL } from "@/lib/types";

export default async function OrdersPage() {
  const res = await authFetch("/api/orders");
  if (!res.ok) return <p className="mt-20 text-center">Impossible de charger les commandes.</p>;
  const orders: Order[] = await res.json();

  return (
    <div className="max-w-3xl mx-auto mt-12 space-y-6">
      <h1 className="text-2xl font-bold">Commandes</h1>
      {orders.length === 0 && <p>Aucune commande.</p>}
      <div className="space-y-4">
        {orders.map((order) => (
          <div key={order.id} className="border p-4 space-y-2">
            <div className="flex justify-between text-sm text-gray-600">
              <span>{order.customerEmail}</span>
              <span>{new Date(order.createdAt).toLocaleString("fr-FR")}</span>
            </div>
            <p className="font-semibold">{ORDER_STATUS_LABEL[order.status]}</p>
            <ul className="text-sm space-y-1">
              {order.items.map((item) => (
                <li key={item.id} className="flex justify-between">
                  <span>{item.productName} × {item.quantity}</span>
                  <span>{item.total} €</span>
                </li>
              ))}
            </ul>
            {order.items.some((i) => i.cancellationReason) && (
              <p className="text-red-600 text-sm">
                {order.items.find((i) => i.cancellationReason)?.cancellationReason}
              </p>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}