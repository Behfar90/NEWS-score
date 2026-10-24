import type {
  FieldErrors,
  NewsScoreRequest,
  NewsScoreResponse,
  ValidationProblem,
} from "./types";

export type CalculateResult =
  | { kind: "success"; score: number }
  | { kind: "invalid"; fieldErrors: FieldErrors }
  | { kind: "failed" };

const FIELD_ERROR_KEY = /^measurements\[(\d+)\]\.value$/;

export async function calculateNewsScore(request: NewsScoreRequest): Promise<CalculateResult> {
  try {
    const response = await fetch("/api/news-score", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });

    if (response.ok) {
      const body = (await response.json()) as NewsScoreResponse;
      return { kind: "success", score: body.score };
    }

    if (response.headers.get("Content-Type")?.includes("application/problem+json")) {
      const problem = (await response.json()) as ValidationProblem;
      const fieldErrors = toFieldErrors(problem.errors, request);
      if (fieldErrors) {
        return { kind: "invalid", fieldErrors };
      }
    }

    return { kind: "failed" };
  } catch {
    return { kind: "failed" };
  }
}

function toFieldErrors(
  errors: ValidationProblem["errors"],
  request: NewsScoreRequest,
): FieldErrors | null {
  if (!errors) {
    return null;
  }

  const fieldErrors: FieldErrors = {};

  for (const [key, messages] of Object.entries(errors)) {
    const match = FIELD_ERROR_KEY.exec(key);
    const measurement = match ? request.measurements[Number(match[1])] : undefined;

    if (!measurement || !messages[0]) {
      return null;
    }

    fieldErrors[measurement.type] = messages[0];
  }

  return Object.keys(fieldErrors).length > 0 ? fieldErrors : null;
}
