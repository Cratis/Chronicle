<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/options.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Chronicle options

Verified at `Chronicle.Kotlin v6.13.0`: `ChronicleOptions.kt` and
`readModels/DefaultReadModelNamingPolicy.kt`. `ChronicleOptions` is a data class
with `@JvmOverloads` constructors.

| Property | Default |
| --- | --- |
| `connectionString` | required |
| `programIdentifier` | `"Unknown"` |
| `defaultSinkTypeId` | `CHRONICLE_SINK_TYPE` env var, else `WellKnownSinkTypes.MONGODB` |
| `autoDiscoverAndRegister` | `true` |
| `artifacts` | `ClientArtifacts.default` (scans the classpath) |
| `artifactActivator` | `ArtifactActivator` |
| `openTelemetry` | `null` (uses globally registered API) |
| `readModelNamingPolicy` | `DefaultReadModelNamingPolicy`: `@ReadModel(id)` or simple name |

Set `readModelNamingPolicy` with `withReadModelNamingPolicy(policy)`, **not a
constructor argument**. It names the storage collection/table, not the
read-model identifier. `copy()` drops it: set it again afterward.
`withoutAutoRegistration()` and `withArtifactsFrom(...)` preserve it.

Two `@JvmStatic` factories: `ChronicleOptions.fromConnectionString(String)`
and `ChronicleOptions.development()`. Use `withoutAutoRegistration()` for
explicit registration or `withArtifactsFrom(vararg packages)` to avoid scanning
the entire classpath in a large application.
