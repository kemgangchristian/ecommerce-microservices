export type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
};

export type OrderStatus = 0 | 1 | 2;

export type OrderItem = {
  id: string;
  orderId: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  total: number;
  cancellationReason: string | null;
};

export type Order = {
  id: string;
  createdAt: string;
  customerEmail: string;
  status: OrderStatus;
  items: OrderItem[];
};

export const ORDER_STATUS_LABEL: Record<OrderStatus, string> = {
  0: "En attente",
  1: "Confirmée",
  2: "Annulée",
};