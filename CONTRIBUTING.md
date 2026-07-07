# Contributing

Thanks for considering a contribution — this project exists because EUDIPLO doesn't have
an official .NET SDK yet, and it's meant to be useful for more than just its original use
case.

## Building and testing

```bash
dotnet restore
dotnet build
dotnet test
```

No real EUDIPLO server is needed to run the test suite — it exercises `EudiploApiClient`
against a hand-rolled fake `HttpMessageHandler` (`tests/Eudiplo.Client.Tests/TestSupport/`).

## Code style

Formatting and naming conventions are enforced via `.editorconfig`. Before committing:

```bash
dotnet format
```

CI runs `dotnet format --verify-no-changes` and will fail the build if this wasn't done.

## Adding a new EUDIPLO endpoint

Endpoint methods are grouped by EUDIPLO API area, one `partial class EudiploApiClient`
file per area under `src/Eudiplo.Client/Endpoints/` (e.g. `EudiploApiClient.Tenant.cs`).
To add a new method:

1. Add it to the matching `Endpoints/EudiploApiClient.<Area>.cs` file (or create a new one
   if the area doesn't exist yet), following the existing pattern: build the request inside
   the `SendWithAuthAsync(() => ..., ct)` delegate, and use the shared `ParseJsonArray`
   helper for list responses.
2. Add tests in the matching `tests/Eudiplo.Client.Tests/Endpoints/EudiploApiClient<Area>Tests.cs`
   file, covering at minimum: the success path, a non-success/not-found path, and a failure
   path that throws.
3. Run `dotnet test` and check coverage didn't regress (`dotnet tool run reportgenerator
   -reports:"tests/Eudiplo.Client.Tests/TestResults/**/coverage.cobertura.xml"
   -targetdir:coveragereport -reporttypes:TextSummary` after `dotnet test
   --collect:"XPlat Code Coverage"`).

## Scope

This client covers EUDIPLO's backend-management API (what a server integration calls) —
not the wallet-facing OID4VCI/OID4VP protocol endpoints (`.well-known/*`, `/authorize`,
`/credential`, etc.), which EUDIPLO exposes for wallets to call directly and which a
backend integration never touches. PRs adding those are out of scope for this package.

## Pull requests

Keep PRs focused on one endpoint area or fix at a time where possible. Update
`CHANGELOG.md`'s `[Unreleased]` section for any user-facing change.
