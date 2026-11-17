---
name: cratis-chronicle-configuration
description: Use when choosing, changing or diagnosing Chronicle client or kernel settings, defaults, environment variables or the local development loop. Not for production topology (use cratis-chronicle-hosting) or client wiring (use the client skills).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-configuration/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Configure Chronicle

Most Chronicle problems that look like bugs are a setting: a message size that
differs between client and kernel, a development default left in a deployed
environment, a port or TLS switch that does not match the other side. Find the
setting, its default and the side that owns it before changing code.

Two surfaces exist and they are configured differently:

| Surface | Where it lives | How to set it |
| --- | --- | --- |
| Client (`ChronicleOptions`) | The application | Code, `Cratis:Chronicle` configuration section, or the connection string |
| Kernel (server options) | The `cratis/chronicle` container or process | `chronicle.json` mounted at `/app/chronicle.json`, or `Cratis__Chronicle__*` environment variables |

A setting on one side never changes the other. Where both sides enforce a limit
(gRPC message size), change both.

## Workflow

1. Name the symptom and the side. Read [symptoms](references/symptoms.md) first;
   it maps error text to the setting that usually causes it.
2. Look up the setting: [client options](references/client-options.md) or
   [kernel options](references/kernel-options.md). Both list the default and when
   changing it is justified.
3. For a container or CI deployment, translate the JSON path to an environment
   variable with [environment variables](references/environment-variables.md).
4. For a local developer machine, follow the
   [development inner loop](references/development-inner-loop.md) instead of
   copying production settings.
5. Change one setting, restart the side that owns it, and reproduce the symptom.

## Rules that prevent the common mistakes

- **Do not state a default from memory.** Read the reference; if a setting is
  not listed, read the product options type or documentation before answering.
- **`DEVELOPMENT` is a compile-time symbol of the image**, not an environment
  variable. `latest-development` images behave as development kernels;
  `ASPNETCORE_ENVIRONMENT` does not turn a production image into one. The
  runtime environment (`Development`) separately controls webhook and capture
  `AllowInsecureHttp` and the loopback exemption. The `Validation` default
  (`Warn` versus `BlockLocalAddresses`) follows the image, not the runtime
  environment.
- **Certificate validation is skipped by default on the client.** That suits the
  self-signed development image. A deployed client must opt in to validation.
- **`Authentication:Authority` is an external identity provider's issuer URL.**
  A non-empty value disables the built-in issuer. It is never the kernel's own
  URL.
- **Secrets go in `*File` variants or secret management**, not in committed
  `chronicle.json`, images or logs. Do not quote or invent credential values.
- **Caller-set namespace resolvers are not trusted on their own.** The default
  HTTP header resolver (`NamespaceHttpHeader`) and the subdomain resolver both
  read values the caller sets. Strip or overwrite the header at a trusted
  gateway, or resolve from an authenticated claim, before it selects a tenant.
- A settings change is not authorization to restart, redeploy or alter a shared
  environment. State the change and its blast radius first.

## References

- Read [client options](references/client-options.md) for `ChronicleOptions`,
  ASP.NET Core options and the connection-string switches.
- Read [kernel options](references/kernel-options.md) for ports, TLS, features,
  observers, authentication, clustering and storage keys.
- Read [environment variables](references/environment-variables.md) for the
  naming rule, list indexes and secret `*File` variants.
- Read [development inner loop](references/development-inner-loop.md) for the
  development image, port, credentials, first Workbench sign-in and TLS switches.
- Read [symptoms](references/symptoms.md) when starting from an error or odd
  behavior rather than a setting.

## Related skills

- Use `cratis-chronicle-hosting` for production topology, certificates,
  MongoDB replica sets, clustering and backup order; this skill does not repeat
  them.
- Use `cratis-chronicle-client-dotnet` for client wiring and lifecycle;
  its connection-options reference holds connection-string forms.
- Use `cratis-chronicle-multi-tenancy` for namespace resolution design.
- Use `cratis-arc-configuration` for Arc's own settings.
