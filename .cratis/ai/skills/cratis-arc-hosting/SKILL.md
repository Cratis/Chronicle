---
name: cratis-arc-hosting
description: Bootstrap and configure a Cratis Arc .NET host — choose AddCratis versus AddCratisArc or ArcApplication, activate Chronicle and storage integrations, disable controllers for trimming/NativeAOT, supply JSON metadata, and check identity/discovery and embedded viewer exposure. Use when creating a host, diagnosing missing endpoints, or preparing a trimmed publish. Do not use for command/query design, running a Chronicle server, or Kotlin/TypeScript hosting.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-hosting/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Host Arc for .NET

Choose the host and integrations before copying a bootstrap. Arc is standalone
CQRS; Chronicle is optional and its client connects to a separate event store.
Nothing in `AddCratis()` runs the Chronicle engine in your application.

Read references/connection-resolution.md when MongoDB or EF Core connections
must resolve per tenant or use separate runtime and schema identities.

## Verified product sources

| Package | Purpose |
| --- | --- |
| `Cratis`, `Cratis.Arc`, `Cratis.Arc.Core` | Composition, ASP.NET hosting and standalone host |
| `Cratis.Arc.Chronicle`, `Cratis.Arc.MongoDB`, `Cratis.Arc.EntityFrameworkCore` | Optional client/storage integrations |
| `Cratis.Arc.Screenplay.Embedded` | Optional embedded event-model explorer |

Source: [Arc](https://github.com/Cratis/Arc/tree/main/Source/DotNET).
Check signatures against the consumer's installed version.

## Choose one bootstrap

| Setup | Configure before Build | Activate after Build |
| --- | --- | --- |
| ASP.NET, full event-sourced stack (`Cratis`) | `builder.AddCratis(...)` | `app.UseCratis()` |
| ASP.NET, Arc only (`Cratis.Arc`) | `builder.AddCratisArc(...)` | `app.UseCratisArc()` |
| Standalone HTTP host (`Cratis.Arc.Core`) | `ArcApplication.CreateBuilder(args)`, then `builder.AddCratisArc(...)` | `app.UseCratisArc()` |

Full-stack ASP.NET bootstrap (requires the `Cratis` package and configured
Chronicle connection; it deliberately does not opt into unsigned headers):

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.AddCratis();
var app = builder.Build();
app.UseCratis();
app.Run();
```

`AddCratis` composes Arc, `WithChronicle` and the Microsoft Identity Platform
header scheme. For your own authentication use `AddCratisArc` plus
`WithChronicle`, not `AddCratis`. For Arc without event sourcing use
`AddCratisArc` and your storage/services. On the standalone host the listen URL
comes from `ArcOptions.Hosting.ApplicationUrl`, not an argument to `UseCratisArc`.
Read [integrations and publish boundaries](references/integrations-and-publish.md)
when selecting storage, disabling MVC, adding generated JSON contracts, or
configuring the explorer.

## Keep trust and discovery explicit

- Forwarded identity headers are ignored by default. Set
  `Cratis:Arc:TrustForwardedIdentityHeaders=true` only behind an authenticating,
  header-stripping ingress that prevents direct backend access. `AddCratis`
  registering a scheme does not make the headers trusted.
- Discovery is anonymous only in Development by default. Elsewhere it requires
  authentication, or is unmapped (404) if the host cannot authenticate.
  `Introspection.RequireAuthentication` unset chooses that default; assigning
  false deliberately exposes it everywhere. `Roles` implies authentication and
  cannot combine with false. Disable both `Introspection.Enabled` and
  `Introspection.IdentityDiscovery` to remove all discovery; `/.cratis/me` remains.
- `/.cratis/me` uses the authenticated principal, not a readable identity cookie.
  Configure a real authentication mechanism before treating protected endpoints
  as usable. See `cratis-arc-authentication-authorization-and-identity` for the
  full ingress, identity and authorization contract.

Source: [secure defaults](https://github.com/Cratis/Arc/blob/main/Documentation/upgrading/secure-defaults.mdx).

## Verify the host, not just the compilation

1. Build in the actual shipping configuration and publish mode, zero warnings.
2. Start against the intended Chronicle/storage services. Confirm activation
   maps one intended command and query; a successful Build alone proves neither.
3. Exercise anonymous and authenticated requests in Development and Production.
   Check discovery switches and protected invocation independently.
4. For trimmed/NativeAOT output, run that published binary and exercise JSON
   responses (including each `CommandResult<TResponse>`), not only an untrimmed
   Debug host. Missing metadata or service support is a failed publish check.
5. Confirm MVC and the embedded explorer are absent wherever they are not intended.

## Route near misses

- Define the command and its proxy: `cratis-arc-command`.
- EF schema/migrations: `cratis-arc-ef-core-migration`.
- Authentication and identity: `cratis-arc-authentication-authorization-and-identity`.
- A standalone Chronicle client or running store: the Chronicle client/CLI skills.
- Several Arc services with their own event stores: `cratis-multi-service-architecture`; running them together locally: `cratis-multi-service-local-orchestration`.
