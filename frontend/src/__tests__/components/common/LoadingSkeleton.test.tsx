import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LoadingSkeleton } from "@/components/common/LoadingSkeleton";

describe("LoadingSkeleton", () => {
  it("announces the loading state to screen readers", () => {
    render(<LoadingSkeleton />);
    expect(screen.getByRole("status").textContent).toContain("Carregando");
  });

  it("draws 3 placeholders by default", () => {
    const { container } = render(<LoadingSkeleton />);
    expect(container.querySelectorAll(".MuiSkeleton-root")).toHaveLength(3);
  });

  it.each(["stats", "cards", "list"] as const)("draws the requested number of placeholders for %s", (variant) => {
    const { container } = render(<LoadingSkeleton variant={variant} count={5} />);
    expect(container.querySelectorAll(".MuiSkeleton-root")).toHaveLength(5);
  });

  it("uses the requested height for the placeholders", () => {
    const { container } = render(<LoadingSkeleton count={1} height={190} />);
    expect(container.querySelector<HTMLElement>(".MuiSkeleton-root")?.style.height).toBe("190px");
  });

  it("hides the placeholder shapes from screen readers", () => {
    const { container } = render(<LoadingSkeleton count={2} />);
    expect(container.querySelector("[aria-hidden='true'] .MuiSkeleton-root")).not.toBeNull();
  });
});
