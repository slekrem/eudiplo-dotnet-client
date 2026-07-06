# Eudiplo.Client

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
    options.EudiploBaseUrl = "https://your-eudiplo-instance.example.com";
    options.RootClientId = "...";
    options.RootClientSecret = "...";
});
```

Or bind from configuration (`appsettings.json`):

```json
{
  "Eudiplo": {
    "EudiploBaseUrl": "https://your-eudiplo-instance.example.com",
    "RootClientId": "...",
    "RootClientSecret": "..."
  }
}
```

```csharp
services.AddEudiploClient(configuration.GetSection("Eudiplo"));
```

## What's covered

Client-credentials OAuth2 auth (with token caching + 401-retry) plus typed wrappers for:

- Presentation offers & session polling (`/api/verifier/offer`, `/api/session/{id}`)
- Issuer credential configs & offers (`/api/issuer/*`)
- OAuth2 client management (`/api/client/*`)
- Tenant management (`/api/tenant/*`, requires a root client)
- Verifier configs (`/api/verifier/config/*`)
- Trust lists (`/api/trust-list/*`)
- Key chains (`/api/key-chain/*`)
- Registrar integration (`/api/registrar/*`)
- Schema metadata signing (`/api/schema-metadata/sign`)

## Origin

Extracted from [Entryix](https://entryix.com), a B2B ticketing platform built on EUDI Wallet
credentials, where this client has been used in production against a self-hosted EUDIPLO
instance since mid-2026.

## License

Apache-2.0, matching EUDIPLO's own license.
