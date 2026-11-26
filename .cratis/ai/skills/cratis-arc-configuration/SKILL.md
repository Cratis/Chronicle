---
name: cratis-arc-configuration
description: Use when choosing, changing or diagnosing C# Arc settings, defaults, ArcOptions, appsettings keys, read protection, MongoDB or EF Core keys, or React provider props. Not for command design, hosting setup or Chronicle kernel settings.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-configuration/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Configure Cratis Arc

Arc settings fail quietly: a route prefix that differs between the build and
the host returns 404 with no warning, a development-only default turns on
exception details or open discovery, and `AddCratis()` authenticates nobody
until a trust setting is flipped. Find the key, its default and the side that
owns it before changing code.

## Where settings live

| Section | Read by | Holds |
| --- | --- | --- |
| `Cratis:Arc` | `AddCratisArc`, `AddCratis` | `ArcOptions`: core, commands, queries, routes, identity, tenancy |
| `Cratis:Chronicle` | `AddCratis`, `WithChronicle()` | Chronicle client options (see `cratis-chronicle-configuration`) |
| `Cratis:MongoDB` | `WithMongoDB()` | MongoDB connection and resilience |
| `Cratis:EntityFrameworkCore` | `WithEntityFrameworkCore()` | EF Core connection and discovery |
| `ReadProtection` (no `Cratis:` prefix) | Startup and run time | Read-protection ceiling and measuring |
| Build properties | Proxy generator package | `CratisProxies*` and `CratisReadProtection` |
| React `<Arc>` props | Frontend | `apiBasePath`, `origin`, query transport |

Environment variables replace `:` with `__`:
`Cratis:Arc:Tenancy:HttpHeader` is `Cratis__Arc__Tenancy__HttpHeader`; list
items take an index. Keys are case-insensitive. The `configureOptions` callback
passed to `AddCratisArc` runs after binding and wins over configuration.

## Workflow

1. Start from the symptom in [symptoms](references/symptoms.md) when there is
   an error or odd behavior; it names the setting.
2. Look up the key in [Arc options](references/arc-options.md), or the
   MongoDB, EF Core and read-protection keys in
   [data and protection keys](references/data-and-protection-keys.md).
3. For the browser side, use [frontend and routes](references/frontend-and-routes.md),
   which also covers the settings that must match between build and host.
4. Change one setting and restart; reproduce the symptom.

## Rules that prevent the common mistakes

- **Never state a default from memory.** Read the reference; for a key that is
  not listed, read the options type.
- **Route settings exist twice**: as MSBuild properties for the generator and
  as `GeneratedApis` keys for the host. Arc does not compare them; a mismatch
  is a 404. Change both in one commit. The skip and include forms are
  inverses.
- **`ExposeExceptionDetails` defaults from `ASPNETCORE_ENVIRONMENT` alone.**
  `DOTNET_ENVIRONMENT=Development` does not turn it on. Leave it off in
  production.
- **Discovery (introspection) is anonymous only in Development.** Outside
  Development it needs a signed-in user unless `Introspection:Roles` narrows it
  or `Introspection:Enabled` / `IdentityDiscovery` is `false`. Do not switch a
  deployed host to Development to restore access.
- **`AddCratis()` authenticates nobody** until `TrustForwardedIdentityHeaders`
  is `true`, and that is correct only when every request passes through an
  ingress that authenticates callers and replaces the identity headers.
- **`ReadProtection` keys are root-level.** `Cratis:Arc:ReadProtection:*` has no
  effect. The protection mode itself is the `CratisReadProtection` build
  property, not configuration; on Arc 23 leave it unset (default `On`).
- **Do not put connection strings or credentials in committed appsettings.**
  Use secret configuration or environment variables.
- Do not change a setting on a shared or deployed environment without stating
  the change and its blast radius first.

## References

- Read [Arc options](references/arc-options.md) for `Cratis:Arc` keys,
  defaults and when to change them.
- Read [data and protection keys](references/data-and-protection-keys.md) for
  `Cratis:MongoDB`, `Cratis:EntityFrameworkCore` and `ReadProtection`.
- Read [frontend and routes](references/frontend-and-routes.md) for React
  provider props and the generator-versus-host route settings.
- Read [symptoms](references/symptoms.md) when starting from an error.

## Related skills

- Use `cratis-arc-command` for the proxy generator build package and its
  properties.
- Use `cratis-arc-authentication-authorization-and-identity` for identity
  design.
- Use `cratis-arc-hosting` for creating the host.
- Use `cratis-chronicle-configuration` for Chronicle client and kernel settings.
