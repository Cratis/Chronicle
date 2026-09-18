---
title: Every major version, and what it meant
description: What each Chronicle major changed, so you can tell what a jump from one version to another actually costs you.
---

One row per major boundary, oldest recorded first. Read the rows between where you are and
where you are going — that set, in order, is your upgrade.

Each row says what broke and what you do about it. Where a release left no record, this page
says so rather than guessing.

:::caution[Two releases to step over, not onto]
**14.0.0** shipped broken NuGet binaries; its own release notes point at **14.0.1**. **17.0.0**
was cut by accident from a routine chore commit 125 releases behind the branch it should have
come from, and registry immutability means the number cannot be reclaimed — the real baseline
for that major is **17.0.1**. Land on the `.0.1` in both cases.
:::

## At a glance

Find your current version and your target, and read every row between them. The **Touches**
column is what decides how much work a jump is: compile-time breaks accumulate harmlessly,
stored state does not.

| Boundary | Touches | In one line |
|---|---|---|
| [6 → 7](#6-to-7) | Stored state | Inbox observer keys changed; stored state needs rewriting |
| [7 → 8](#7-to-8) | Compile-time, transport | Namespaces renamed wholesale; client moved off Orleans to REST |
| [8 → 9](#8-to-9) | Compile-time | Startup and configuration only; wire-compatible both directions |
| [9 → 10](#9-to-10) | Unknown | No release notes exist for this major |
| [10 → 11](#10-to-11) | Stored state | Job state enum values changed; in-flight jobs unreliable |
| [11 → 12](#11-to-12) | Compile-time | Package renamed; internal APIs hidden; CDC API removed |
| [12 → 13](#12-to-13) | Compile-time | `GetEventStore()` became async |
| [13 → 14](#13-to-14) | Stored state, compile-time | Read-model formalization; collections and definitions moved |
| [14 → 15](#14-to-15) | Compile-time, config | Rules retired; `Content` type changed; TLS required in production |
| [15 → 16](#15-to-16) | Config, endpoints | One port `35000`, always TLS; management port removed |
| [16 → 17](#16-to-17) | Wire | Every gRPC service regenerated; method names and envelopes changed |
| [17 → 18](#17-to-18) | Wire | Append `Content` back to a plain JSON string |
| [18 → 19](#18-to-19) | Compile-time | One .NET parameter `bool` → `bool?`; rebuild |

### Reading the Touches column

- **Compile-time** — it breaks the build and you fix it once, against the version you land on.
- **Stored state** — it concerns data already written. Ordered: skipping past it does not skip it.
- **Wire** — kernel and clients must move together across it.
- **Config, endpoints** — deployment and configuration change, not your code.

## The boundaries

### 6 to 7

**Stored observer state** — released 2023-01-10.

The key for Inbox observers changed to include the source microservice id, so existing stored
observer state for inboxes is not read back correctly. The release notes carry the remediation:
rewrite the `_id` of the affected document in the `observers` collection to
`<event sequence id> : <observer id> : <source microservice id>`.

**You do:** migrate that stored state before starting 7.x, or accept that inbox observers
restart from their default position.

### 7 to 8

**Namespaces, transport and several APIs** — released 2023-01-23.

The largest boundary in this list. The client stopped using an Orleans client to reach the
kernel and moved to a REST-based approach. Namespaces were renamed wholesale
(`Aksio.Cratis.Events.Store` → `Aksio.Cratis.Events`, `…Events.Projections` →
`…Projections`, `…Events.Observation` → `…Observation`, event sequences to
`…EventSequences`, `…Events.Schemas` → `…Schemas`). `CommandResult` and `QueryResult` moved
to Fundamentals. `ITenants` began returning a `Tenant` object rather than an id.
`IImmediateProjections` began returning `ImmediateProjectionResult`, so reads became
`.Model`. `DefineState` on `RuleFor` became abstract and required.

**You do:** expect compile errors across the codebase and work through them; update
`cratis.json` / `cluster.json`, where single-cluster mode changed from `local` to `single`.

### 8 to 9

**Repository split and startup** — released 2023-07-19.

Non-Chronicle code moved out into separate Fundamentals, ApplicationModel and MongoDB
repositories. From a usage perspective only startup and configuration changed: `.UseCratis()`
became a fluent configuration surface, and an ASP.NET Core application using a `Startup` class
must call `.UseCratis()` on `IApplicationBuilder`.

**Notable:** this boundary was explicitly wire-compatible both directions — pre-9 clients
could talk to a 9 kernel and vice versa.

### 9 to 10

**No recorded change** — released 2024-07-30.

**This release has no notes**, and the tag sits on a dependency-update merge. No breaking
change is recorded for it. Treat the major as unexplained rather than assuming it was
deliberate, and test your own usage across the boundary.

### 10 to 11

**Stored job state** — released 2025-03-12.

`JobState` and `JobStepState` enum values changed, and the release explicitly does not
guarantee backward compatibility for the persisted values.

**You do:** expect in-flight job state from 10.x to be unreliable. Drain jobs before
upgrading if their completion matters.

### 11 to 12

**Internalized APIs and a renamed package** — released 2025-06-04.

Kernel APIs that were reachable through dependencies were internalized. The
`Cratis.Chronicle.Orleans.InProcess` package became `Cratis.Chronicle.InProcess`. The
client-side Change Data Capture API was removed.

**You do:** rename the package reference; stop using anything from
`Cratis.Chronicle.Contracts` or `Cratis.Chronicle.Infrastructure` that you were reaching into.

### 12 to 13

**One method became async** — released 2025-06-23.

`ChronicleClient.GetEventStore()` is async, and now calls `DiscoverAll()` and `RegisterAll()`
for you.

**You do:** `await` it. Opt out with `AutoDiscoverAndRegister = false` on `ChronicleOptions`
or the `skipDiscovery` argument if you were managing discovery yourself.

### 13 to 14

**Read-model formalization and stored layout** — released 2025-08-15.

"Model" became **ReadModel** throughout. Projections and reducers now reference formalized
read models rather than carrying their own definitions. The collection holding event types was
renamed from `schemas` to `event-types`, projection definitions are stored differently, and
observer definition and state were split because definitions are shared across namespaces
while state is per namespace. Default naming for read models and properties became
configurable rather than fixed.

**You do:** this one touches stored layout as well as APIs. Read the release notes in full,
and go to **14.0.1**, not 14.0.0.

### 14 to 15

**Rules retired, TLS, content type** — released 2026-02-03.

**Rules** were removed as a building block in favor of FluentValidation and standard ASP.NET
Core mechanisms. `AppendedEvent.Content` in the .NET client became `object` — the actual
deserialized type — instead of `ExpandoObject`. TLS became required for the kernel in
production. `[Passive]` moved to the `ReadModels` namespace. The `Cratis.Chronicle.XUnit`
package became `Cratis.Chronicle.Testing`, with namespaces to match.

**You do:** replace Rules usage; fix code that treated `Content` as `ExpandoObject`; configure
TLS for production; rename the testing package reference.

### 15 to 16

**One port, always TLS** — released 2026-07-09.

The server consolidated all traffic onto port `35000` and that port always uses TLS, with a
self-signed certificate generated automatically in development. The `managementPort` server
option and client `ManagementPort`, the separate `workbench.tls` configuration, and the Aspire
management endpoint were all removed. The Workbench moved to `https://localhost:35000`.

**You do:** remove management-port configuration, point tooling and bookmarks at the single
TLS port, and configure a certificate for production.

### 16 to 17

**The generated wire contract** — released 2026-08-25.

Every gRPC service became generated from Arc `[Command]`/`[ReadModel]` artifacts in Core
instead of being hand-written, completing a migration begun in 16.x: method names and payload
envelopes changed across the surface. The kernel also gained a single server-side compatibility
check that every client language uses.

**You do:** take a matching client. Go to **17.0.1** — 17.0.0 is the accidental release
described above and carries a pre-migration contract.

### 17 to 18

**Append content back to a string** — released 2026-09-08.

`Content` on `AppendRequest`, `EventToAppend`, `EventForEventSourceId` and `ReviseRequest`
changed from `map<string, JsonNode>` back to a plain JSON string, matching how event content
is represented everywhere else on the wire.

**You do:** nothing, if you use a published client at a matching version. A client built
directly against the raw gRPC contracts must send and receive `Content` as a string.

### 18 to 19

**A .NET signature** — released 2026-09-17.

One public .NET method parameter moved from `bool` to `bool?`. Nothing changed on the wire.
See [Upgrading from 18 to 19](18-to-19.md).

**You do:** rebuild.

## Skipping several majors at once

Nothing prevents jumping from, say, 14 to 19 directly. What that costs is the union of every
row in between, so read them in order and collect the work before starting.

Two kinds of change behave differently when you skip:

- **Compile-time changes accumulate but do not interact.** Renamed namespaces, moved types and
  changed signatures pile up without interacting; you fix them once against the version you
land on.
- **Stored-state changes are ordered.** 6 → 7 (observer keys), 10 → 11 (job state) and
  13 → 14 (read-model and event-type layout) each concern data already written. Skipping past
  one does not skip its consequence.

Where you are crossing a stored-state boundary, take a backup you have restored from at least
once before, and rehearse the upgrade against a copy.

## Wire compatibility across a boundary

Chronicle verifies that the current build still serves **every released minor of the major it
is on**. That guarantee stops at a major boundary — which is exactly what a major means. So a
client and kernel differing only in minor are expected to interoperate, while a client and
kernel on different majors are not, unless a boundary above explicitly says otherwise (8 → 9
did).

Upgrade the kernel and the clients together across a major boundary, rather than assuming a
skew will be tolerated.
