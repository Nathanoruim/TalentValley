import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { BRAND_LINK_LABEL, BrandLink } from "@/components/brand/BrandLink";

describe("BrandLink", () => {
  it("is a link to the landing page named after its destination", () => {
    render(<BrandLink />);

    const link = screen.getByRole("link", { name: BRAND_LINK_LABEL });
    expect(link.getAttribute("href")).toBe("/");
    expect(BRAND_LINK_LABEL).toBe("Talent Valley — ir para a página inicial");
  });

  it("shows the wordmark next to the mark", () => {
    render(<BrandLink />);

    expect(screen.getByRole("link").textContent).toBe("Talent Valley");
  });

  it("shows only the mark when compact, keeping the full accessible name", () => {
    render(<BrandLink compact />);

    const link = screen.getByRole("link", { name: BRAND_LINK_LABEL });
    expect(link.textContent).toBe("");
    expect(link.querySelector("svg")).not.toBeNull();
  });

  it("tells the menu when the user follows the link", async () => {
    const onNavigate = vi.fn();
    render(<BrandLink onNavigate={onNavigate} />);

    await userEvent.click(screen.getByRole("link"));

    expect(onNavigate).toHaveBeenCalledTimes(1);
  });
});
