import Link from "next/link";

export default function HomePage() {
  return (
    <div className="max-w-sm mx-auto mt-20 space-y-4 text-center">
      <h1 className="text-2xl font-bold">E-Commerce Microservices</h1>
      <div className="flex gap-4 justify-center flex-wrap">
        <Link href="/login" className="bg-black text-white px-4 py-2">Se connecter</Link>
        <Link href="/register" className="border px-4 py-2">S&apos;inscrire</Link>
        <Link href="/products" className="border px-4 py-2">Produits</Link>
        <Link href="/orders" className="border px-4 py-2">Commandes</Link>
      </div>
    </div>
  );
}