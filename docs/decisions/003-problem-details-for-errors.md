# ADR 003: ProblemDetails for errors

## Context

The spec asks for structured errors. The frontend needs to know which field each error belongs to, so it can point the user to it.

Out of the box, ASP.NET Core was inconsistent. Validation errors from our endpoint were fine, but a body that could not be read as JSON (for example `"type": 1`) produced a plain-text stack trace in Development and an empty `400` in Production. A request with the wrong content type gave an empty `415`.

## Decision

Every error response uses ProblemDetails (RFC 9457) with the `application/problem+json` content type.

- **Validation errors** use `ValidationProblemDetails`, keyed by field path: `measurements[0].value`, `measurements[1].type`, or `measurements` for a missing type.
- **Unreadable JSON** gets the same shape. Request binding is set to throw in every environment (`ThrowOnBadRequest`). An `IExceptionHandler` maps the JSON error's path (`$.measurements[0].type`) to the same key style, or to `body` for a syntax error.
- **Framework errors** (404, 405, 415) get a ProblemDetails body from `UseStatusCodePages`, and unexpected exceptions get a generic `500` from `UseExceptionHandler`, without a stack trace.
- `AddProblemDetails()` adds a `traceId` to every error, so a client report can be matched to the server logs.

The endpoint tests run the app in the Production environment, because that's where the defaults were worst. A test in Development could not tell whether `ThrowOnBadRequest` is set.

## Alternatives considered

- **A custom error format.** Every client would need to learn it. ProblemDetails is a standard, and .NET supports it natively.
- **A generic `400` for unreadable JSON.** Less code, but a hand-written API call with `"type": 1` would get no hint about which field is wrong.

## Consequences

- One error shape for every failure, in every environment. Clients can handle all errors the same way.
- The frontend maps `measurements[i].value` keys back to its inputs. Any other key is treated as a general error, since the user can't fix it by editing a field. The key format is therefore part of the API contract.
- Messages are English and use the API's type codes ("TEMP must be between 32 and 42."). For a Norwegian UI, the API should return error codes and let the frontend phrase the messages.
- A few lines of setup in `Program.cs` and one exception handler class, each covered by a test that fails without it.
