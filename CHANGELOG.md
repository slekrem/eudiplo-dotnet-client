# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); this project follows
[Semantic Versioning](https://semver.org/) once it reaches `1.0.0` — until then, `0.x`
releases may contain breaking changes as the API settles.

## [Unreleased]

### Added
- `EudiploApiClient` — HTTP client covering nearly the entire EUDIPLO backend-management
  API surface: OAuth2 client-credentials auth (with token caching and 401-retry), tenants,
  OAuth2 clients, human users, key-chains (incl. KMS provider config), issuer configuration
  (credential configs, attribute providers, webhook endpoints, issuance config), issuance
  offers and deferred-credential completion, trust lists (incl. versioning), registrar
  integration, schema metadata (incl. full versioning/cataloging), sessions (incl. Server-Sent
  Events for live status via `SubscribeToSessionEventsAsync`), status lists, cache
  administration, generic file storage, audit log, and verifier/presentation configs.
- `AddEudiploClient` dependency-injection extensions (`IConfiguration`,
  `IConfigurationSection`, or `Action<EudiploClientOptions>` overloads).
- Deliberately out of scope: the wallet-facing OID4VCI/OID4VP protocol endpoints
  (`.well-known/*`, `/authorize`, `/credential`, etc.) — those are called by an EUDI
  Wallet, never by a backend integrator, so they don't belong in a management-API client.
- 193 unit tests, 93% line coverage, against a hand-rolled fake `HttpMessageHandler`
  (no real EUDIPLO server needed to run the test suite).
- `samples/Eudiplo.Client.Sample` — a runnable console sample against a **real** EUDIPLO
  instance (via a provided `docker-compose.yml`, minimal/SQLite profile), demonstrating the
  multi-tenant flow: authenticate as the root client, create an isolated tenant, then use
  that tenant's auto-generated admin client to create a key-chain. The shared
  `docker-compose.yml` also starts EUDIPLO's own admin UI (`localhost:4200`).
- `samples/Eudiplo.Client.Sample.AccessControl` — the "Access Control System" pattern from
  EUDIPLO's own architecture diagram, now built as three real tiers: a Lit 3 + TypeScript
  UI (`Frontend/`) talks only to an ASP.NET Core backend (`Backend/`), which is the only
  piece using `Eudiplo.Client`. The backend provisions its gate tenant once at startup
  (not per-request), opens a presentation request, and streams the verified result to the
  browser via Server-Sent Events (`SubscribeToSessionEventsAsync`) instead of polling.
  Uncovered a real, previously-undocumented requirement while first building this as a
  console sample: a tenant needs an access key-chain before it can create a presentation
  offer, or EUDIPLO returns 404. Verified against a **real EUDI Wallet holding a real
  German PID** (via `EudiploApiClient.Registrar.cs`'s fully-API-driven registrar
  enrollment — no manual dashboard cert-request flow needed) — this surfaced that the
  real DE-PID has no `age_over_18`/`age_equal_or_over` claim, only `birthdate`; the sample
  now requests that and checks the 18-year threshold itself, server-side.

### Changed
- Repository restructured per current .NET OSS conventions: `Endpoints/` subfolder for the
  per-EUDIPLO-area partial-class files, central package version management, `.editorconfig`,
  CI (`dotnet format --verify-no-changes` + build + test on net8.0/net9.0).
- `EudiploApiClient` now implements `IDisposable` (releases its internal token-refresh
  lock; never disposes the caller-owned `HttpClient`).

### Fixed
- `CreateKeyChainAsync`'s `type` parameter is documented as EUDIPLO's `KeyChainType` enum
  (`"standalone"` / `"internalChain"`), not a cryptographic algorithm — found by running
  the new sample against a real EUDIPLO instance, which rejected the previously-undocumented
  assumption immediately.
- `SubscribeToSessionEventsAsync` sent the access token via the `Authorization` header and
  got a 401 from EUDIPLO's session-events endpoint — that endpoint only accepts the token
  via a `?token=` query parameter (browsers' `EventSource` can't send custom headers, so
  EUDIPLO's SSE controller was built to check the query string instead). Found by building
  a real SSE-consuming backend against a live server; the existing unit test only checked
  the request path, not the auth mechanism, so it couldn't have caught this. Both the
  client and its test are fixed now.
