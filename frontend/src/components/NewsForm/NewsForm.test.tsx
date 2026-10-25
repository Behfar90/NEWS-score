import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import axe from "axe-core";
import { describe, expect, it, vi } from "vitest";
import { NewsForm } from "./NewsForm";

const LABELS = {
  TEMP: "Body temperature",
  HR: "Heartrate",
  RR: "Respiratory rate",
};

function mockFetch(response: Response | Error) {
  const fetchMock = vi.fn<typeof fetch>(() =>
    response instanceof Error ? Promise.reject(response) : Promise.resolve(response),
  );
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function jsonResponse(body: unknown, status: number, contentType: string) {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": contentType } });
}

function renderForm() {
  const user = userEvent.setup();
  render(<NewsForm />);
  return user;
}

async function fillForm(user: ReturnType<typeof userEvent.setup>, values: { TEMP: string; HR: string; RR: string }) {
  await user.type(screen.getByLabelText(LABELS.TEMP), values.TEMP);
  await user.type(screen.getByLabelText(LABELS.HR), values.HR);
  await user.type(screen.getByLabelText(LABELS.RR), values.RR);
}

async function submit(user: ReturnType<typeof userEvent.setup>) {
  await user.click(screen.getByRole("button", { name: "Calculate NEWS score" }));
}

const VALID_VALUES = { TEMP: "39", HR: "43", RR: "19" };

async function expectNoAccessibilityViolations() {
  const results = await axe.run(document.body, {
    runOnly: { type: "tag", values: ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"] },
    rules: { "color-contrast": { enabled: false } },
  });
  expect(results.violations).toEqual([]);
}

describe("NewsForm", () => {
  it("does not call the API when the form is empty", async () => {
    const fetchMock = mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await submit(user);

    expect(fetchMock).not.toHaveBeenCalled();
  });

  it.each([
    ["TEMP", "abc"],
    ["HR", "4x"],
    ["RR", "19.5"],
    ["TEMP", "38,9"],
  ] as const)("does not call the API when %s is %s", async (field, invalidValue) => {
    const fetchMock = mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await fillForm(user, { ...VALID_VALUES, [field]: invalidValue });
    await submit(user);

    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("sends the measurements to the API without calculating anything", async () => {
    const fetchMock = mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await fillForm(user, VALID_VALUES);
    await submit(user);

    expect(fetchMock).toHaveBeenCalledOnce();
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("/api/news-score");
    expect(init?.method).toBe("POST");
    expect(init?.headers).toEqual({ "Content-Type": "application/json" });
    expect(JSON.parse(init?.body as string)).toEqual({
      measurements: [
        { type: "TEMP", value: 39 },
        { type: "HR", value: 43 },
        { type: "RR", value: 19 },
      ],
    });
  });

  it("shows the score returned by the API", async () => {
    mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await fillForm(user, VALID_VALUES);
    await submit(user);

    expect(await screen.findByText("News score:", { exact: false })).toHaveTextContent("News score: 2");
  });

  it("shows all server validation errors as links and marks those fields invalid", async () => {
    mockFetch(
      jsonResponse(
        {
          title: "One or more validation errors occurred.",
          status: 400,
          errors: {
            "measurements[0].value": ["TEMP must be between 32 and 42."],
            "measurements[1].value": ["HR must be between 26 and 220."],
          },
        },
        400,
        "application/problem+json",
      ),
    );
    const user = renderForm();

    await fillForm(user, { TEMP: "31", HR: "300", RR: "19" });
    await submit(user);

    const alert = await screen.findByRole("alert");
    expect(within(alert).getByRole("link", { name: "TEMP must be between 32 and 42." })).toHaveAttribute(
      "href",
      "#measurement-TEMP",
    );
    expect(within(alert).getByRole("link", { name: "HR must be between 26 and 220." })).toHaveAttribute(
      "href",
      "#measurement-HR",
    );
    expect(screen.getByLabelText(LABELS.TEMP)).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByLabelText(LABELS.HR)).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByLabelText(LABELS.RR)).not.toHaveAttribute("aria-invalid");
  });

  it("keeps the error message but clears the invalid state when that field is edited", async () => {
    mockFetch(
      jsonResponse(
        { title: "", status: 400, errors: { "measurements[0].value": ["TEMP must be between 32 and 42."] } },
        400,
        "application/problem+json",
      ),
    );
    const user = renderForm();

    await fillForm(user, { TEMP: "31", HR: "43", RR: "19" });
    await submit(user);
    await screen.findByRole("alert");

    await user.type(screen.getByLabelText(LABELS.TEMP), "5");

    expect(screen.getByLabelText(LABELS.TEMP)).not.toHaveAttribute("aria-invalid");
    expect(screen.getByRole("alert")).toHaveTextContent("TEMP must be between 32 and 42.");
  });

  it.each([
    ["the API is unreachable", new TypeError("Failed to fetch")],
    ["the proxy returns a plain-text 500", new Response("Internal Server Error", { status: 500 })],
  ])("shows a general error when %s", async (_, response) => {
    mockFetch(response);
    const user = renderForm();

    await fillForm(user, VALID_VALUES);
    await submit(user);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Could not calculate the NEWS score.Please try again.",
    );
  });

  it("hides the score when a value is edited", async () => {
    mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await fillForm(user, VALID_VALUES);
    await submit(user);
    await screen.findByText("News score:", { exact: false });

    await user.type(screen.getByLabelText(LABELS.HR), "1");

    expect(screen.queryByText("News score:", { exact: false })).not.toBeInTheDocument();
  });

  it("clears the values and the score when the form is reset", async () => {
    mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await fillForm(user, VALID_VALUES);
    await submit(user);
    await screen.findByText("News score:", { exact: false });

    await user.click(screen.getByRole("button", { name: "Reset form" }));

    expect(screen.getByLabelText(LABELS.TEMP)).toHaveValue("");
    expect(screen.getByLabelText(LABELS.HR)).toHaveValue("");
    expect(screen.getByLabelText(LABELS.RR)).toHaveValue("");
    expect(screen.queryByText("News score:", { exact: false })).not.toBeInTheDocument();
  });

  it("clears the errors when the form is reset", async () => {
    mockFetch(
      jsonResponse(
        { title: "", status: 400, errors: { "measurements[0].value": ["TEMP must be between 32 and 42."] } },
        400,
        "application/problem+json",
      ),
    );
    const user = renderForm();

    await fillForm(user, { TEMP: "31", HR: "43", RR: "19" });
    await submit(user);
    await screen.findByRole("alert");

    await user.click(screen.getByRole("button", { name: "Reset form" }));

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(screen.getByLabelText(LABELS.TEMP)).not.toHaveAttribute("aria-invalid");
  });
});

describe("NewsForm accessibility", () => {
  it("has no violations when empty", async () => {
    renderForm();

    await expectNoAccessibilityViolations();
  });

  it("has no violations when showing a score", async () => {
    mockFetch(jsonResponse({ score: 2 }, 200, "application/json"));
    const user = renderForm();

    await fillForm(user, VALID_VALUES);
    await submit(user);
    await screen.findByText("News score:", { exact: false });

    await expectNoAccessibilityViolations();
  });

  it("has no violations when showing server errors", async () => {
    mockFetch(
      jsonResponse(
        {
          title: "",
          status: 400,
          errors: {
            "measurements[0].value": ["TEMP must be between 32 and 42."],
            "measurements[1].value": ["HR must be between 26 and 220."],
          },
        },
        400,
        "application/problem+json",
      ),
    );
    const user = renderForm();

    await fillForm(user, { TEMP: "31", HR: "300", RR: "19" });
    await submit(user);
    await screen.findByRole("alert");

    await expectNoAccessibilityViolations();
  });
});
