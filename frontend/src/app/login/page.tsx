"use client";

import { useActionState } from "react";
import { login } from "@/lib/actions/auth";

export default function LoginPage() {
  const [state, formAction, pending] = useActionState(login, undefined);

  return (
    <form action={formAction} className="max-w-sm mx-auto mt-20 space-y-4">
      <h1 className="text-xl font-bold">Connexion</h1>
      <input name="email" type="email" placeholder="Email" required className="border p-2 w-full" />
      <input name="password" type="password" placeholder="Mot de passe" required className="border p-2 w-full" />
      {state?.error && <p className="text-red-600 text-sm">{state.error}</p>}
      <button type="submit" disabled={pending} className="bg-black text-white px-4 py-2 w-full">
        {pending ? "Connexion..." : "Se connecter"}
      </button>
    </form>
  );
}