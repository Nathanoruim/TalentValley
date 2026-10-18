import { useEffect } from "react";
import { render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { stubMotionMedia } from "@/__tests__/components/auth/motion/motionMedia";
import AppTemplate from "@/app/(app)/template";

const nav = vi.hoisted(() => ({ pathname: "/recrutador" }));

vi.mock("next/navigation", () => ({ usePathname: () => nav.pathname }));

const mounted = vi.fn();

function Probe() {
  useEffect(() => {
    mounted();
  }, []);
  return <p>conteúdo</p>;
}

function template() {
  return (
    <AppTemplate>
      <Probe />
    </AppTemplate>
  );
}

describe("app template", () => {
  beforeEach(() => {
    stubMotionMedia("reduced");
    nav.pathname = "/recrutador";
    mounted.mockClear();
  });
  afterEach(() => vi.unstubAllGlobals());

  it("renders the page content", () => {
    render(template());

    expect(screen.getByText("conteúdo")).not.toBeNull();
  });

  it("keeps the content filling the area next to the menu", () => {
    render(template());

    const wrapper = screen.getByText("conteúdo").parentElement as HTMLElement;
    expect(wrapper.className).toContain("flex-1");
    expect(wrapper.className).toContain("flex-col");
  });

  it("replays the entrance when the address changes, even inside the same section", () => {
    const { rerender } = render(template());

    nav.pathname = "/recrutador/favoritos";
    rerender(template());

    expect(mounted).toHaveBeenCalledTimes(2);
  });

  it("does not replay the entrance while the address stays the same", () => {
    const { rerender } = render(template());

    rerender(template());

    expect(mounted).toHaveBeenCalledTimes(1);
  });
});
