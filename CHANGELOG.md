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
