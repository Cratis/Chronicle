<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/fundamentals-migration.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Shared concepts, correlation and observer upgrades

## Shared concepts and correlation

Requires Chronicle.Kotlin 7.0.0 for the shared concept and correlation type migration below; check Gradle dependencies, or retain the earlier client API until deliberately migrating and recompiling.

Import `ConceptAs` from `io.cratis.fundamentals.concepts` and implement `value()`,
not a property/getter. Java record accessors satisfy this interface directly.
Import `CorrelationId` from `io.cratis.fundamentals.execution`, use `NOT_SET` and
`value()`, and recompile consumers. The old Chronicle names are warning-level
Kotlin typealiases for this major version, not a permanent alternative API.
Typed source-id overloads consume Fundamentals concepts. Follow the product's
[migration guide](https://github.com/Cratis/Chronicle.Kotlin/blob/main/Documentation/guides/migrate-to-fundamentals.md).

Requires Chronicle.Kotlin 7.1.0 and Fundamentals 0.6.0 for shared ambient correlation context; check Gradle dependencies, or pass correlation explicitly instead of relying on shared ambient state.

The module-level `correlationIdManager` delegates to Fundamentals' shared
`CorrelationIdContext`. Its own API still takes/returns UUIDs; separately
constructed managers remain independent. State is thread-bound, not
coroutine-bound: pass correlation explicitly for suspending/thread-hopping work
and clear ambient context at the boundary. Never assume a pooled thread is a
request or tenant scope.

Event/read-model enum values are written as ordinal numbers to match their
schemas. Stored names remain readable; undefined ordinals, including numeric
strings, are rejected. Do not rearrange persisted enum ordinals casually.

## Observer and transport upgrades

Requires contracts and kernel 19.70.0 for StartAtTail; check the server version, or retain initial catch-up until it is upgraded.

`@StartAtTail` applies to reactor classes in Kotlin and Java. It skips history
only on first registration; existing positions remain. `@OnceOnly` excludes
replay, not initial catch-up or redelivery. Older servers ignore the tail flag.

Requires Chronicle.Kotlin 6.14.0 and kernel 19.56.0 for fastForward; check Gradle dependencies and the server version, or do not offer this administrative operation.

`store.observers.fastForward(...)` accepts an identifier or reactor/reducer class
and returns `FastForwardResult`, with a Java bridge. It deliberately skips work;
it is not recovery or deduplication.

`ChronicleOptions.maxReceiveMessageSize` and `withMaxReceiveMessageSize()` default
to 100 MB; `null` retains grpc-java's own default. The constructor and `copy()`
have a trailing parameter; earlier constructor overloads remain. Recompile affected
calls rather than treating transport capacity as a query bound.
