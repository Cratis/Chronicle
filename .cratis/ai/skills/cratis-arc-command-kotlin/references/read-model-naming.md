<!-- cratis-ai-managed: skills/cratis-arc-command-kotlin/references/read-model-naming.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Share Arc and Chronicle collection names

Source: `Arc.Kotlin`: `ChronicleArcAutoConfiguration.arcReadModelNamingPolicy`,
its Kotlin/Java conformance tests and `Documentation/guides/read-model-naming.mdx`.
The integration depends on Chronicle.Kotlin.

With the MongoDB integration present, one Arc `NamingPolicy` bean is adapted
into Chronicle's `ReadModelNamingPolicy`. Chronicle's starter passes it to the
options it builds: a projected `Author` lands in `Authors`, where Arc reads it.
The adapter is absent when the Arc policy is missing/ambiguous or an application
already declares a Chronicle `ReadModelNamingPolicy` bean. The application bean
wins; keep its names consistent with Arc's reader.

The policy changes the storage container, not the read-model identifier or
observer identity. An explicit `@ReadModel(id = ...)` no longer determines the
collection name under the adapter. Without Arc's Mongo naming policy,
Chronicle retains identifier-based naming. An application that constructs its
own `ChronicleOptions` must call `withReadModelNamingPolicy(policy)` itself.

The old `pluralizeReadModelNames = false` workaround is no longer needed for
this JVM integration. Remove it only after verifying your affected type names
and reader/projection pairing. Existing documents do not move with the name:
replay affected projections into the new collections and verify their data;
drop the old collections only when no longer needed and deletion is authorized.
Do not claim a .NET-written collection uses the JVM inflector: Arc's default
policy uses Evo Inflector, while .NET uses Humanizer. Override measured disagreements.
