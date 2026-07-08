# Eudiplo.Client

[![CI](https://github.com/slekrem/eudiplo-dotnet-client/actions/workflows/ci.yml/badge.svg)](https://github.com/slekrem/eudiplo-dotnet-client/actions/workflows/ci.yml)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

An **unofficial** .NET client for [EUDIPLO](https://github.com/openwallet-foundation-labs/eudiplo) — the
OpenWallet Foundation's middleware for backend integration with EUDI Wallets (OpenID4VCI /
OpenID4VP). There is currently no official .NET SDK for EUDIPLO's HTTP API; this project fills
that gap.

Not affiliated with or endorsed by the OpenWallet Foundation or the EUDIPLO maintainers.

## Status

Early / pre-1.0. EUDIPLO's own API surface is still evolving, so this package tracks it on a
best-effort basis and may introduce breaking changes between `0.x` releases as EUDIPLO itself
changes. See the [EUDIPLO API reference](https://openwallet-foundation.github.io/eudiplo/latest/api/)
for the authoritative source of truth on what the server actually supports.

## Install

```bash
dotnet add package Eudiplo.Client
```

## Quick start

```csharp
using Eudiplo.Client;

var services = new ServiceCollection();
services.AddEudiploClient(options =>
{
    options.BaseUrl = "https://your-eudiplo-instance.example.com";
    options.ClientId = "...";
    options.ClientSecret = "...";
});
```

Or bind from configuration (`appsettings.json`, section name `"Eudiplo"`):

```json
{
  "Eudiplo": {
    "BaseUrl": "https://your-eudiplo-instance.example.com",
    "ClientId": "...",
    "ClientSecret": "..."
  }
}
```

```csharp
services.AddEudiploClient(configuration);
```

Working, runnable examples (against a real EUDIPLO instance in Docker, not a mock) live in
[`eudiplo-dotnet-client-samples`](https://github.com/slekrem/eudiplo-dotnet-client-samples),
referencing this package from NuGet.

## What's covered

Client-credentials OAuth2 auth (with token caching + 401-retry) plus nearly the entire
EUDIPLO backend-management API: tenants, OAuth2 clients, human users, key-chains (incl. KMS
provider config), issuer configuration (credential configs, attribute providers, webhook
endpoints, issuance config), issuance offers and deferred-credential completion, trust lists
(incl. version history), registrar integration, schema metadata (incl. full
versioning/cataloging), sessions (incl. Server-Sent Events for live status), status lists,
cache administration, generic file storage, audit log, and verifier/presentation configs.

Deliberately **not** covered: the wallet-facing OID4VCI/OID4VP protocol endpoints
(`.well-known/*`, `/authorize`, `/credential`, etc.) — an EUDI Wallet calls those directly;
a backend integration never does, so they don't belong in a management-API client.

## Origin

Extracted from [Entryix](https://entryix.com), a B2B ticketing platform built on EUDI Wallet
credentials, where this client has been used in production against a self-hosted EUDIPLO
instance since mid-2026.

## License

Apache-2.0, matching EUDIPLO's own license.
