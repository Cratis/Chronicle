---
title: Upgrade Cratis Chronicle
description: Plan a Cratis Chronicle upgrade by checking version boundaries, client compatibility, deployment settings and stored-state changes before you update.
---

Upgrading Cratis Chronicle can affect client APIs, the protocol between clients and the
kernel, deployment settings or stored state. Start with your current and target versions,
then read the changes between them to find the work your application needs.

## Guides

Start with [Every major version](major-versions.md) and read each row between your current
and target versions. The table distinguishes code changes from stored-state and deployment
changes, so you can identify what needs attention before updating packages or the kernel.

| Your upgrade | Where to start |
|---|---|
| [Across major versions](major-versions.md) | Check the recorded boundaries from 6 → 19, including the replacement releases for 14.0.0 and 17.0.0. |
| [18 → 19](18-to-19.mdx) | Check the .NET binary compatibility change, rebuild requirements and analyzers included from 19.4.8. |

For changes within a major, review the release notes and the deployment guidance below.
In particular, check [reverse-proxy trust](#reverse-proxies-after-the-forwarded-header-security-change)
before upgrading a deployment behind a proxy, and [decimal mappings](#exact-decimals-and-precise-event-schemas)
before relying on exact values in existing SQL read models.

## How to read a Chronicle version

Chronicle follows semantic versioning. A major version permits breaking changes to a
published contract; it does not mean every application needs code changes. Check which
kind of change the upgrade guide describes:

- Wire-contract changes affect the gRPC messages and services clients use to communicate with the kernel.
- Published API changes affect callers of that API, such as a .NET method whose signature changed.
- Observable behavior changes affect how an operation works, even when its signature stays the same.

The kernel, .NET client and contracts packages (.NET, npm, Maven and Hex) are published
from this repository at one version. A breaking change to a .NET API therefore raises the
kernel and contracts package major too, even when the wire contract is unchanged.

The Kotlin/Java, TypeScript and Elixir clients have independent releases in
[Chronicle.Kotlin](https://github.com/Cratis/Chronicle.Kotlin),
[Chronicle.TypeScript](https://github.com/Cratis/Chronicle.TypeScript) and
[Chronicle.Elixir](https://github.com/Cratis/Chronicle.Elixir). Their version numbers need
not match the kernel's. Read the release notes in your client's repository alongside the
Chronicle upgrade guide, and check its contracts package dependency. Compatibility with
the kernel depends on that wire contract, not matching version numbers; see
[the wire-compatibility handshake](../building-a-client/clustering-and-connection-lifecycle.md#the-wire-compatibility-handshake).

Minor and patch releases must preserve compatibility with existing published contracts,
but they can still change what your build reports. The .NET client's
[code analysis rules](../code-analysis/index.md) can gain diagnostics in any release.
They first reached consuming builds through the client package in 19.4.8.

## Exact decimals and precise event schemas

Upgrade the kernel before expecting the .NET client to register precise schemas for defaulted
concept properties. The client negotiates this support and retains its legacy event schema against
older kernels. Adding metadata for personally identifiable information (PII) or encryption affects
only subsequent appends; review and [redact historical plaintext](../events/redaction.mdx) if necessary.

Replay existing SQL read models that carry decimal properties before relying on the new decimal
column mapping. The migrator does not alter their existing columns in place. See
[Exact decimal values](../events/schema-representation.mdx#exact-decimal-values) for provider mappings, legacy doubles and the
SQL Server scale limit.

## Reverse proxies after the forwarded-header security change

Chronicle now trusts `X-Forwarded-For` and `X-Forwarded-Proto` only from loopback or configured
proxy addresses. Previous builds accepted those headers from any peer. What you need to change
depends on the listener:

- With a TLS main listener behind a non-loopback proxy, the kernel still sees HTTPS, so HTTPS checks
  continue to pass. Without a trusted-proxy setting, `RemoteIpAddress` becomes the proxy's IP;
  this affects client-IP logs and telemetry only. Set `forwardedHeaders.knownProxies` to the
  immediate proxy IP or `forwardedHeaders.knownNetworks` to its restricted source CIDR if you
  need the original client IP.
- With a cleartext HTTP/2 (h2c) main listener, or a non-exclusive cleartext health port behind an
  HTTPS proxy, configure trust for the immediate proxy before upgrading. Otherwise forwarded HTTPS is ignored and
  Workbench cookie mutations can fail antiforgery validation because the request looks like HTTP.

The proxy must overwrite incoming forwarded headers rather than pass through values supplied by
clients. Trust `X-Forwarded-Proto` only from a proxy whose public side is HTTPS. See
[Trusted reverse proxies](../hosting/configuration/tls.md#trusted-reverse-proxies) for the
configuration and environment-variable names.

The documented Docker Compose deployment connects directly to Chronicle, so it needs no proxy
setting. Aspire Composition runs YARP in a container and exposes **HTTP** at `localhost:9876`.
Do not trust its forwarded `http` scheme while it fronts Chronicle's TLS listener: that would
make Workbench cookie mutations fail antiforgery validation. Its container source IP matters
only if you later put an HTTPS endpoint in front of YARP.

## Resource name checks

Chronicle checks event store and namespace names for overlaps in storage. Existing names keep working; see [Resource name checks](resource-names.md) for the rollout.

## Upgrading across several majors

Read every intervening major boundary in order, even if you plan to jump directly to your
target version. Skipping a release does not skip its stored-state changes; follow the
instructions for each affected boundary in [Every major version](major-versions.md).

Chronicle's wire-compatibility gate checks that a build still serves released minor-version
contracts within its major's declared compatibility range. Compatibility across a major
boundary is not covered by that check, so review the guide before changing the kernel and clients.

## Events you already stored

Changing your application's event schemas is a separate task from upgrading Chronicle.
Use [Migrations](../migrations/index.md) to define schema generations and the transformations
between them. For stored-layout changes required by a Chronicle upgrade, follow the
version-specific guide rather than treating them as application event migrations.
