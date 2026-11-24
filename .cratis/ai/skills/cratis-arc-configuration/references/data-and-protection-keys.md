<!-- cratis-ai-managed: skills/cratis-arc-configuration/references/data-and-protection-keys.md -->
# Data and protection keys

## Contents

- MongoDB
- EF Core
- Read protection

## MongoDB

Section `Cratis:MongoDB`, read by `WithMongoDB()`.

| Key | Default | Change it when |
| --- | --- | --- |
| `Server` | none, required | Always. Keep credentials in secret configuration. Missing `Server` or `Database` fails host startup. |
| `Database` | none, required | Always. |
| `DirectConnection` | not set (the connection string's setting is kept) | Force or forbid a direct connection. |
| `EnableResilience` | `true` | Keep on for Cosmos DB and change streams opened before a collection exists; it treats "collection not found" as an empty result on async calls. |
| `LogCommandBodies` | `false` | Diagnosing only. Bodies can contain personal data; never in production. |
| `ConnectionResolution:*` | resolver `Default` | Per-tenant connections. |
| `Sinks:<name>:*` | none | Named sinks. |

## EF Core

Section `Cratis:EntityFrameworkCore`, read by `WithEntityFrameworkCore()`.

| Key | Default | Change it when |
| --- | --- | --- |
| `ConnectionString` | empty | Always. While empty, Arc does not auto-discover `DbContext` types. |
| `AutoDiscoverDbContexts` | `true` | You register contexts yourself. |
| `ConnectionResolution:*` | resolver `Default` | Per-tenant connections. |

`JsonConverters` is code-only; other options properties are not read from
configuration.

## Read protection

Root-level `ReadProtection` section (no `Cratis:` prefix). The environment form
is `ReadProtection__MaximumMode`. `Cratis:Arc:ReadProtection:*` has no effect.

| Key | Default | Change it when |
| --- | --- | --- |
| `ReadProtection:MaximumMode` | `On` | Lower the effective mode at run time without a rebuild; it is a ceiling that can only lower. |
| `ReadProtection:StartupAdmissionCheck` | `Default` | `Log` or `Off` while adopting protection. |
| `ReadProtection:RequireGeneratedMetadata` | `false` (always required where dynamic code is unavailable) | Fail instead of falling back to reflection for read-model key lookup. |
| `ReadProtection:MeasureSampleRate` | `0.1` | Fraction of commands scanned by Measure, 0 to 1. |
| `ReadProtection:MeasureTailTimeout` | 250 ms | 1 to 5000. |
| `ReadProtection:MeasureScanTimeout` | 5000 ms | 1 to 5000. |
| `ReadProtection:MeasureScanLimit` | 500 events | 1 to 500. |

The mode itself is the `CratisReadProtection` MSBuild property, not a
configuration key. Arc 23 defaults it to `On`: leave it unset. Set `Measure` or
`Off` only as a temporary adoption step, or `Off` for a justified project-wide opt-out; prefer
`[Unprotected("why")]` on the read or the command. (Arc 22 defaulted to `Off`.)
