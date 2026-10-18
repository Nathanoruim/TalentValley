import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import Page from "@/app/(app)/conta/senha/page";

vi.mock("@/components/account/ChangePasswordForm", () => ({ ChangePasswordForm: () => <p>change password form</p> }));

describe("change password page", () => {
  it("shows the password form without its own role restriction, since the (app) layout already requires a session", () => {
    render(<Page />);

    expect(screen.getByText("change password form")).not.toBeNull();
  });
});
