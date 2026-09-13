# CRM → Legacy Membership Integration

An HTTP-triggered Azure Function that takes a member registration in the CRM's JSON shape and returns the payload the legacy membership system's API expects: renamed and nested fields, a reformatted date of birth, and a membership-type-to-plan-code lookup.

## Architecture

```
 CRM system
     │  POST JSON (member registration)
     ▼
 Azure Function (HTTP trigger)
     │  delegates to
     ▼
 MemberRegistrationHandler          (Functions/)
     │
     ├─▶ RegistrationValidator  ──▶  invalid → 400 { errors: [...] }
     │        (Core/Validation)
     │
     └─▶ LegacyPayloadMapper    ──▶  valid   → 200 { member: {...} }  (legacy system's shape)
              (Core/Mapping)
```

Every trigger is a thin adapter: parse the request, delegate to Core, shape the HTTP response. All the actual rules — what's required, what a valid email looks like, how a date gets reformatted, which plan code a membership type maps to — live in Core, which has no Azure Functions dependency and is fully unit tested without the Functions host.

There are three HTTP endpoints (`src/CrmLegacyIntegration.Functions/Functions/`):

| Endpoint | Auth | Purpose |
|---|---|---|
| `POST /api/member-registrations` | Functions-level function key | The main mapping endpoint |
| `POST /api/secure/member-registrations` | Bearer JWT + role check | Same mapping; demonstrates app-level auth/authz (see below) |
| `POST /api/login` | none (demo only) | Mints a bearer token for the secure endpoint |

The two mapping endpoints share the same `MemberRegistrationHandler` above — they only differ in how the caller is checked. The bearer-token flow looks like this:

```
 POST /api/login  ──▶  JwtAccessTokenIssuer  ──▶  bearer token (role: member-registrations.write)
   (Core/Security)                                          │
                                                              ▼
 POST /api/secure/member-registrations  ──▶  JwtBearerAuthenticator  ──▶  MemberRegistrationHandler
   Authorization: Bearer <token>            (Core/Security)              (same as above)
```

Project layout:

```
src/
  CrmLegacyIntegration.Core/        Pure C#: models, validation, mapping, JSON contract, JWT auth. No Azure Functions dependency.
  CrmLegacyIntegration.Functions/   The Azure Functions host (isolated worker): HTTP triggers + Swagger/OpenAPI wiring.
tests/
  CrmLegacyIntegration.Core.Tests/  xUnit + FluentAssertions tests against Core.
```

## Running it

Requires the .NET SDK. This repo targets `net8.0` (the current Azure Functions isolated-worker LTS); `Directory.Build.props` sets `RollForward=LatestMajor` so it also runs on a machine that only has a newer major SDK/runtime installed.

```bash
dotnet build
dotnet test
```

To run the function itself locally, install the [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local), copy `src/CrmLegacyIntegration.Functions/local.settings.json.example` to `local.settings.json` in that same folder, then:

```bash
cd src/CrmLegacyIntegration.Functions
func start
```

With the host running, open `http://localhost:7071/api/swagger/ui` for a Swagger UI you can use to send a request body and see the response directly, without curl — the raw OpenAPI document is at `http://localhost:7071/api/openapi/v3.json` (or `v2.json`) if you want to import it elsewhere (e.g. Postman).

Sample request (matches the exercise's example exactly):

```bash
curl -s -X POST http://localhost:7071/api/member-registrations \
  -H "Content-Type: application/json" \
  -d '{
        "firstName": "Alex",
        "lastName": "Nguyen",
        "dateOfBirth": "1990-04-12",
        "email": "alex.nguyen@example.com",
        "membershipType": "Single",
        "registeredAt": "2026-06-01T09:00:00Z"
      }'
```

```json
{
  "member": {
    "given_name": "Alex",
    "family_name": "Nguyen",
    "dob": "12/04/1990",
    "contact": { "email": "alex.nguyen@example.com" },
    "plan_code": "S",
    "source": "MILKYWAY"
  }
}
```

A request missing a required field, with an implausible email, or with an unrecognized `membershipType` gets a `400` with a body like `{ "errors": ["email is not a valid email address."] }` listing every problem found, not just the first.

## Design decisions

- **Validation returns a result, it doesn't throw.** `RegistrationValidator.Validate(...)` collects every failing rule into a `ValidationResult` instead of stopping at the first one, so a single 400 response tells the caller everything that's wrong at once. Exceptions are reserved for genuinely unexpected failures, not for expected bad input.
- **`DateOnly`, not `DateTime`, for a date of birth.** There's no time-of-day or time zone to a birth date, so the type says so. `DateOfBirthParser` tries the documented `yyyy-MM-dd` format first, then a small set of tolerated fallbacks (`yyyy/MM/dd`, `MM/dd/yyyy`, `dd-MM-yyyy`); a date that matches none of them is a validation error, never a thrown `FormatException`.
- **An unrecognized extra field in the request is ignored**, and that's pinned down by a test (`JsonContractTests`), not left as an accident of `System.Text.Json`'s default behavior.
- **One shared `JsonSerializerOptions` set (`LegacyJsonSerialization`, in `Core/Json`)** is used by both the Function and the tests, so the wire format is defined and verified in exactly one place: case-insensitive camelCase in (`CrmRead`), `JsonNamingPolicy.SnakeCaseLower` out for the legacy contract specifically (`LegacyWrite` — `given_name`, `family_name`, `plan_code`, ...), and ordinary camelCase for everything else this API returns, like login tokens and error lists (`ApiDefault`) — no per-property JSON attributes.
- **The plan-code lookup is a small closed dictionary** (`Single`→`S`, `Couple`→`C`, `Family`→`F`) shared between validation and mapping, so the two can't drift apart.
- **The function doesn't log the raw email address** — only the outcome and membership type — since the request payload is personal data.
- **The HTTP trigger uses `AuthorizationLevel.Function`**, the least-privilege default for a real deployment, even though this exercise doesn't deploy it. Azure Functions Core Tools doesn't enforce that key locally, so it doesn't get in the way of local testing.
- **The trigger itself isn't unit tested.** Isolated-worker HTTP types (`HttpRequestData`/`HttpResponseData`) are awkward to fake convincingly, and the trigger is intentionally a thin pass-through over Core. It's exercised manually instead (via `func start` + curl/Swagger) — same cases, just over real HTTP.

## Auth/authz example: `POST /api/secure/member-registrations`

`MapMemberRegistrationFunction` relies solely on the Functions-level function key (`AuthorizationLevel.Function`) for access control — simple, but the "auth" is entirely outside the code. `MapMemberRegistrationSecureFunction` is the same operation exposed at `secure/member-registrations`, added to show what authenticating and authorizing a caller in application code looks like:

- **Authentication** — the request must carry `Authorization: Bearer <token>`, a JWT with a valid signature and an unexpired `exp` claim. An invalid or missing token gets a `401`.
- **Authorization** — the token's claims must include a role of `member-registrations.write` (configurable via `Jwt:RequiredRole`). A validly-signed token missing that role gets a `403`.

Both functions share the same `MemberRegistrationHandler` for the actual parse/validate/map logic — they only differ in how the caller is checked — and the validation itself (`JwtBearerAuthenticator`, in `Core/Security`) is host-free and unit tested like everything else in Core. It's a small hand-rolled HS256 implementation (base64url-encode header/payload, HMAC-SHA256 sign, constant-time compare) rather than a JWT library, since the whole point here is a self-contained, dependency-light demo — a real deployment should use a maintained library and, per the note below, an actual identity provider.

This uses a shared HMAC secret (`Jwt:SigningKey` in `local.settings.json`) purely so the example runs from local configuration alone. A real deployment would validate against an identity provider's published signing keys instead (e.g. Entra ID via JWKS, through `Microsoft.Identity.Web`), the same way the main note on outbound auth below calls out doing real outbound auth to the legacy system.

The easiest way to get a token locally is `POST /api/login` (`LoginFunction`, backed by `JwtAccessTokenIssuer` in `Core/Security`): it takes no body and checks no credentials — it just mints a token for the required role, signed with the same `Jwt:SigningKey`. It exists purely so this example is exercisable without standing up a real identity provider; a real login/token endpoint would authenticate the caller (username/password, client credentials, ...) before issuing anything.

```bash
token=$(curl -s -X POST http://localhost:7071/api/login | jq -r .accessToken)

curl -s -X POST http://localhost:7071/api/secure/member-registrations \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json" \
  -d '{ "firstName": "Alex", "lastName": "Nguyen", "dateOfBirth": "1990-04-12", "email": "alex.nguyen@example.com", "membershipType": "Single" }'
```

In Swagger UI itself you don't even need to copy/paste: calling Login → Try it out → Execute automatically authorizes the page with the returned token (via a small injected script, `SwaggerAutoAuthorizeScript` in `Functions/OpenApi`, watching for that response and calling swagger-ui's own `preauthorizeApiKey`) — the padlock icon flips to "locked" and every other operation's Try it out now sends that token, no manual Authorize step needed. This pokes at swagger-ui-dist internals that aren't part of the OpenAPI extension's public config surface, so a future upgrade of `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` could break it — the manual Authorize-button flow (paste `accessToken`, no `Bearer ` prefix) always still works as a fallback.

Alternatively, mint a token by hand with the same signing key as `local.settings.json` (`local-dev-only-signing-key-replace-me-32bytes` by default) using the jwt.io debugger: set the payload to `{ "role": "member-registrations.write", "exp": 4102444800 }` and the HS256 secret to that signing key.

## Wiring this into a real outbound call

This exercise stops at producing the legacy payload; actually forwarding it to the legacy system's API would need:

- **Auth** — the legacy system predates the CRM and almost certainly doesn't share its identity provider. A client-credentials (OAuth2) token or a static API key, held in Key Vault / Function app settings (never in source), is the likely fit; cache the token for its lifetime rather than fetching one per request.
- **Retries** — a short exponential backoff with jitter (e.g. via `Microsoft.Extensions.Http.Resilience` or Polly) for transient failures (timeouts, 5xx, connection resets), capped at a few attempts. Only retry when the call is known to be safe to repeat.
- **Timeouts** — an `HttpClient` timeout well under the Function's own execution limit, plus a `CancellationToken` threaded through so a slow legacy call doesn't hang the request.
- **Idempotency** — registrations can be retried (by us, or by the CRM re-delivering), so the legacy call should be safe to send twice: either the legacy API accepts an idempotency key, or the outbound call is deduplicated by CRM registration id on our side before sending.
- **Failure handling** — if the legacy call fails after retries, the registration shouldn't just be dropped: queue it (e.g. an Azure Storage Queue or Service Bus dead-letter) for reprocessing rather than losing it silently.

## A note on this environment

This project was written without a live NuGet feed available to actually run `dotnet build` / `dotnet test` and confirm a clean compile, so please run both locally before treating this as final. Two areas carry the most risk of needing a small fix-up given that constraint:

1. **The `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` attribute usage** (`[OpenApiOperation]`, `[OpenApiRequestBody]`, `[OpenApiResponseWithBody]`, `[OpenApiSecurity]`, and the `IOpenApiConfigurationOptions`/`IOpenApiCustomUIOptions` wiring in `Program.cs`) — this extension's exact namespaces and attribute signatures have shifted across major versions, and I wasn't able to verify the pinned package version (`1.5.1`) against what's actually on NuGet right now.
2. **`SwaggerAutoAuthorizeScript`** — the auto-authorize convenience script is, by its own admission in the code comments, the single least-verified piece here, since it depends on swagger-ui-dist internals rather than the extension's public API.

Everything in `CrmLegacyIntegration.Core` (validation, mapping, the JSON contract, and the JWT issuer/authenticator) has no such dependency risk and is exercised directly by the test suite.
