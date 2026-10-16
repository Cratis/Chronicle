<!-- cratis-ai-managed: skills/cratis-chronicle-event-type-migration/references/explicit-migrations.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Explicit migration endpoints

Requires Chronicle 19.52.0. Check the client package version; older clients need
marked events and `EventTypeMigration<TUpgrade, TPrevious>` discovery.

Register plain or aliased event generations through
`ChronicleOptions.ExplicitArtifacts.RegisterEventType<T>(configure)` before
creating stores, then `RegisterMigration<TMigration>()` (or its `Type` form).
Migration nomination does not register missing endpoints. Both endpoints must
be registered, share one id and have adjacent generations.

A typed migration over unmarked or aliased endpoints uses the additive protected
`EventTypeMigration<TUpgrade, TPrevious>(IEventTypes)` constructor. Pass the
owning registry to `base(eventTypes)`; the client binds it to the store's finalized
catalog rather than another registry in ordinary DI. The parameterless base
keeps declaration-only semantics; listing it explicitly does not change those.

A nominated plain concrete closed class declares these public synchronous
instance methods (signature fragments):

```csharp
void Upcast(IEventMigrationBuilder<TUpgrade, TPrevious> builder)
void Downcast(IEventMigrationBuilder<TPrevious, TUpgrade> builder)
void MapValues(IEventValueMapBuilder<TUpgrade, TPrevious> builder)
```

`Upcast` and `Downcast` are required; `MapValues` is optional. Use the same
property/value builders as typed migrations. No static, generic, async-void,
overloaded or ambiguous roles; one ordinary parameter per role. Plain migration
definers must not implement disposal interfaces, because the provider does not
own disposal. Invalid recipes fail preparation even when no discovered upgrade
would otherwise select them. There is no new Native AOT/trimming guarantee.

For concurrency restricted to an aliased event, resolve
`store.EventTypes.GetEventTypeFor(typeof(TEvent))` and pass that `EventType` to
`WithEventType`, rather than a declaration-only generic helper that cannot know
the store-local alias. Custom schema generators resolving explicit compensation
targets need the registry-aware overloads or throw
`RegistryAwareEventTypeSchemasNotSupported`.

Run `dotnet build -c Debug` and migration specs with `dotnet test`; assert both
directions and real observer delivery where pinning or protected-value release
matters. A successful nomination alone does not prove a generation can be read.
