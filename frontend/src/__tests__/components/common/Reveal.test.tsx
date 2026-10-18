import { render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { Reveal } from "@/components/common/Reveal";
import { stubMotionMedia } from "../auth/motion/motionMedia";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("Reveal", () => {
  it.each(["reduced", "touch", "pointer"] as const)("always renders its content (%s)", (policy) => {
    stubMotionMedia(policy);
    render(
      <Reveal index={3} className="h-full">
        <p>Dentro</p>
      </Reveal>,
    );
    expect(screen.getByText("Dentro")).toBeTruthy();
  });

  it("passes the class name to its wrapper", () => {
    stubMotionMedia("reduced");
    const { container } = render(
      <Reveal className="h-full">
        <p>Dentro</p>
      </Reveal>,
    );
    expect(container.firstElementChild?.className).toBe("h-full");
  });
});
