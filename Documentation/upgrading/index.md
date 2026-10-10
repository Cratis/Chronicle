---
title: Upgrading Chronicle
description: What a Chronicle major version means for you, and where to find the guide for each one.
---

Every major version of Chronicle has an upgrade guide, and every guide answers the same
question first: **does anything change for you?**

Often the answer is no. Chronicle publishes the kernel, the .NET client and the contracts
packages (.NET, npm, Maven and Hex) from one repository at one version, so a breaking
change to any of them raises the major for the kernel, the .NET client and every
contracts package. A major can therefore be driven by something you never call, on a
surface you never touch — and upgrading is then a version bump and a rebuild.

The Kotlin/Java, TypeScript and Elixir clients are released from their own repositories
([Chronicle.Kotlin](https://github.com/Cratis/Chronicle.Kotlin),
[Chronicle.TypeScript](https://github.com/Cratis/Chronicle.TypeScript) and
[Chronicle.Elixir](https://github.com/Cratis/Chronicle.Elixir)) with their own version
numbers, so a language client's version does not match the kernel's and a Chronicle major
does not raise theirs. What ties a language client to a kernel is the wire contract, which
the client checks when it connects — see
[the wire-compatibility handshake](../building-a-client/clustering-and-connection-lifecycle.md#the-wire-compatibility-handshake).
Each language client depends on a specific version of the contracts package, so a Chronicle
major reaches a language client when that client moves to the new contracts version.
Everything below about Chronicle versions applies to the kernel, the .NET client and the
contracts packages; to see what changed in a language client, read the release notes in its
own repository.

That is worth saying out loud rather than leaving you to work it out from release notes.
A major version that silently changes nothing is far more common here than one that
rewrites your code, and not knowing which you are facing is its own cost.

:::note[A major is permission, not a promise]
Raising the major grants the project permission to break something. It does not promise
that anything was broken, and it does not mean every surface changed. Read the guide for
the version you are moving to before planning work.
:::

## Exact decimals and precise event schemas

Upgrade the kernel before expecting the .NET client to register precise schemas for defaulted
concept properties. The client negotiates this support and retains its legacy event schema against
older kernels. Adding PII or encryption metadata affects only subsequent appends; review and
[redact historical plaintext](../events/redaction.mdx) if necessary.

Replay existing SQL read models that carry decimal properties before relying on the new decimal
column mapping. The migrator does not alter their existing columns in place. See
[Exact decimal values](../events/schema-representation.mdx#exact-decimal-values) for provider mappings, legacy doubles and the
SQL Server scale limit.

## Guides

Start with [Every major version](major-versions.md). It has one entry per major boundary —
what broke and what you do — so you can read only the rows between the version you are on and
the one you are going to, including when that spans several majors at once.

| From → to | What it means for you |
|---|---|
| [Every major version](major-versions.md) | The whole history, 6 → 19, including which two releases to step over rather than onto. |
| [18 → 19](18-to-19.mdx) | Nothing, for almost everyone. One .NET method signature changed; rebuild and move on. |

## How to read a Chronicle version

Chronicle follows semantic versioning, with the major raised for a breaking change to any
published surface:

- **The wire contract** — the gRPC messages and services every language client speaks.
  These changes affect you whichever client you use, and the upgrade guide will say so
  plainly.
- **A published API** — for example a public .NET method signature. These affect only
  the clients that expose that surface, and often only at compile time.
- **Observable behavior** — something that worked one way and now works another.

The label on the pull request that cut the release records which of these applied. When a
major was raised for a compile-time break on one client, users of the other clients are
unaffected beyond taking the new kernel.

A minor or patch release does not change a published surface, but it can still change what
your build reports. The .NET client's [code analysis rules](../code-analysis/index.md) can
gain new diagnostics in any release, and they reached consuming builds for the first time in
19.4.8.

## Reverse proxies after the forwarded-header security change

Chronicle now trusts `X-Forwarded-For` and `X-Forwarded-Proto` only from loopback or configured
proxy addresses. Previous builds accepted those headers from any peer. What you need to change
depends on the listener:

- **TLS main listener behind a non-loopback proxy:** the kernel still sees HTTPS, so HTTPS checks
  continue to pass. Without a trusted-proxy setting, `RemoteIpAddress` becomes the proxy's IP;
  this affects client-IP logs and telemetry only. Set `forwardedHeaders.knownProxies` to the
  immediate proxy IP or `forwardedHeaders.knownNetworks` to its restricted source CIDR if you
  need the original client IP.
- **h2c main listener, or a non-exclusive cleartext health port behind an HTTPS proxy:** configure
  trust for the immediate proxy before upgrading. Otherwise forwarded HTTPS is ignored and
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

## Upgrading across several majors

Take them one at a time and read each guide. Chronicle's wire compatibility is verified
within a major: every released minor of the major you are on is checked to still be
served by the current build. Crossing a major boundary is where a protocol change is
permitted to have happened, so it is where an upgrade deserves attention.

## Events you already stored

None of this concerns your stored events. Evolving an event's own schema is a separate,
deliberate mechanism with its own generation and migration model — see
[Migrations](../migrations/index.md). Upgrading Chronicle does not rewrite, re-shape or
re-interpret events you have already appended.
