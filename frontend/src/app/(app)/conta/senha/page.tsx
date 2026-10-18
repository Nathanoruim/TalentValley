"use client";

import { ChangePasswordForm } from "@/components/account/ChangePasswordForm";

// Any signed-in role may change its own password: the (app) layout already requires a session.
export default function ChangePasswordPage() {
  return (
    <div className="flex flex-1 items-start justify-center px-5 py-12 sm:items-center">
      <div className="w-full max-w-[440px]">
        <ChangePasswordForm />
      </div>
    </div>
  );
}
