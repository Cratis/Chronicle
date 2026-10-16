<!-- cratis-ai-managed: skills/cratis-chronicle-event-type-migration/references/pinned-generations.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Migrations and pinned delivery

## Contents

- What a pinned consumer receives
- PDL pins are resolved once, when saved
- Verify delivery through a real client
- Product sources

Requires Chronicle 19.41.0 or later for reactor/reducer opt-in and 19.48.0 or later for typed materialized projection opt-in.
Configure `ChronicleOptions.EventGenerationDelivery` before connecting; see
[standalone and host examples](../../cratis-chronicle-client-dotnet/references/pinned-generation-delivery.md).

## What a pinned consumer receives

The kernel selects the pinned generation's content and hash, and releases
protected values using **that generation's schema**. A reactor or reducer pins
its handler's CLR event generation. A typed materialized projection uses the
CLR generation each clause declares, including joins, children, nested objects,
removals, event-content keys, and parent keys. Incoming live, catch-up, and replay
events and stored join/parent-event lookups use the same release boundary.

`EventContext.EventType.Generation` is the delivered generation;
`EventContext.AppendedGeneration` is the original append generation, or null for
legacy events where it is unknown. Do not infer the append generation from the
handler type.

If the pinned generation's content is missing from storage, Chronicle releases
the known appended generation (or the highest stored generation for legacy
events), then runs the adjacent `EventTypeMigration` upcast/downcast chain to the
pin **for this delivery only**. It neither persists that result nor caches
plaintext. The delivered hash is `EventHash.NotSet` because no durable hash
exists for that migrated content. Erased PII stays erased through migration.
An unavailable registered pin or an unreachable migration target fails with
`PinnedEventTypeGenerationUnavailable`, rather than silently substituting a
schema.

Revised events use their **latest revision**, migrating it to the pin when
needed. The revision's stored hash is retained only when its generation already
matches the pin; otherwise the delivered hash is `EventHash.NotSet`. SQL
revisions from before this release cannot be distinguished from original
content and retain existing generation-selection behavior until revised again.

Every observer/projection may pin **only one generation per event identifier**
across all handlers/clauses, including children and nested objects. Different
identifiers may pin different generations. See the client reference for the
client and kernel registration exception names. Changing a reactor/reducer's
pin or delivery mode does not itself trigger replay; request replay when prior
state needs rebuilding.

## PDL pins are resolved once, when saved

For a PDL definition saved with `GenerationDelivery = Pinned`, a bare reference
such as `from PersonRegistered` resolves to the **highest registered generation
at registration time**. The definition persists that resolved pin. Join and
removal references, including children and nested references, follow the same
rule.

Registering a later generation does **not** advance an existing saved pin.
Re-saving the declaration resolves bare names again against the generations
registered at that time. Keep this distinction when deploying migrations: a
saved declaration is not a moving "latest" subscription.

PDL is owned by Screenplay and has **no generation-qualified event reference
syntax**. Never invent `PersonRegistered+2` or `Event+N`. When an explicit pin is
needed, use .NET fluent or model-bound builders with that generation's CLR event
type: for example, a `From<TEvent>` clause or `[FromEvent<TEvent>]` whose
`TEvent` carries the intended `[EventType]` or `[EventTypeGenerationFor<T>]`.
Do not mix two generations of the same identifier in different clauses.

Compatibility PDL retains bare-name generation **1** and identifier-only
dispatch. Immediate/passive and wildcard projections remain Compatibility;
ad-hoc queries are not opted in by this change. Do not use their results as
proof of pinned delivery.

## Verify delivery through a real client

`ReadModelScenario` and `ReactorScenario` do **not** emulate kernel pinned
migration and protected-value release. The projection scenario preserves a
seeded event's generation, but that is not the kernel selecting or migrating
stored content. A scenario that feeds the desired CLR type only proves local
handling/mapping for that type.

Keep local decision/mapping specifications, and add **real-client integration**
against a matching kernel to prove generation delivery:

- Register both generations and their adjacent migration; use one pin per
  identifier. Assert that conflicting pins are rejected.
- Append another generation and assert delivered content and
  `EventContext.EventType.Generation` on reactors/reducers, plus projected state
  for typed materialized projections. Check `AppendedGeneration` separately.
- Cover live delivery, catch-up, and replay; exercise contributing joins,
  children, removals, and event-content/parent keys when the projection uses
  them. Include missing stored generations and protected values when relevant.
- For saved PDL, verify that adding a generation preserves the old pin and that
  re-saving resolves the bare reference anew.

Kernel specs provide product-level proof; in-process application scenarios are
not a replacement for real-client integration of this option.

## Product sources

`Source/Kernel/Core/Events/EventGenerationRelease.cs` selects stored content,
releases with the selected schema, and migrates missing/revised generations.
`Source/Kernel/Core/Projections/ProjectionGenerationDeliveryValidation.cs`
validates all clauses; `Engine/DefinitionLanguage/ProjectionEventTypeReference.cs`
resolves PDL bare names. `Source/Clients/Testing/ReadModels/ProjectionReadModelProcessor.cs`
and `Reactors/ReactorScenario.cs` define the scenario boundary.

See [Chronicle's event type migration documentation](https://github.com/Cratis/Chronicle/blob/main/Documentation/concepts/event-type-migrations.mdx#opt-in-to-pinned-observer-delivery).
