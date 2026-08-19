import { getCurrentUser } from "@/lib/auth";
import { logout } from "@/lib/actions/auth";

export default async function DashboardPage() {
  const user = await getCurrentUser();
  if (!user) return <p className="mt-20 text-center">Non authentifié.</p>;

  return (
    <div className="max-w-sm mx-auto mt-20 space-y-4">
      <h1 className="text-xl font-bold">Bienvenue, {user.fullName}</h1>
      <p>{user.email}</p>
      <p className="text-sm text-gray-500">Rôle : {user.roles.join(", ") || "Aucun"}</p>
      <form action={logout}>
        <button type="submit" className="bg-black text-white px-4 py-2 w-full">Se déconnecter</button>
      </form>
    </div>
  );
}