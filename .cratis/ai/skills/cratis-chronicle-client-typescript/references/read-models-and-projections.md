<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/references/read-models-and-projections.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Read models and projections

Verified at `Cratis/Chronicle.TypeScript` **v6.51.0**:
`Source/readModels/IReadModels.ts`, `indexDecorator.ts`,
`Source/projections/modelBound/**`, `declarative/**`, and
`Source/events/constraints/**`.

`store.readModels` exposes:

| API | Meaning |
| --- | --- |
| `getInstanceById(type, key, sessionId?)` | Canonical, not deprecated. An empty missing document yields a prototype-only instance; JSON null may construct default fields |
| `findInstanceById(type, key, sessionId?)` | Returns **null when absent**; use for existence decisions |
| `getInstances(type, eventCount?)`, `getSnapshotsById(type, key)` | Instances and snapshots; `materialized` offers paged sink access |
| `watch(type)` | Async iterable, opens on first `next()`; no empty initial changeset |
| `createWatcher(type, options?)` | Starts immediately, exposes readiness and disposal, opt-in resume and overflow-failure limit. Resumption does not replay outage gaps |
| `register()` / `register(type)` | Register discovered models or one type |
| `release`, `releaseMany`, `releaseDocument` | Release protected values through Chronicle |
| `dehydrateSession` | Dehydrate a read-model session |

### Projection styles

- **Model-bound** — decorators on the read model, from
  `@cratis/chronicle/projections`: `fromEvent`, `fromEvery`, `setFrom`,
  `setFromContext`, `setValue`, `join`, `addFrom`, `subtractFrom`, `increment`,
  `decrement`, `count`, `childrenFrom`, `nested`, `clearWith`, `removedWith`,
  `removedWithJoin`, `noAutoMap`, `notRewindable`, `passive`.
- **Declarative** — `@projection(id?, readModelType?, eventSequenceId?)` on a class
  implementing `IProjectionFor<TReadModel>` with
  `define(builder: IProjectionBuilderFor<TReadModel>): void`.

Chronicle's `@readModel()` is deprecated since 6.7.0: infer the read model from
mappings or the type supplied to projection/reducer registration. A custom id
uses `static readonly readModelId = 'custom-id'`. This does **not** deprecate
Arc's separate `@readModel()` query marker.

- `@index()` from `@cratis/chronicle/readModels` declares an ascending, non-unique
  property index, including dotted nested/typed-collection paths.
- `@childrenFrom(Event, { childType, key, identifiedBy, parentKey })` supplies
  typed children and their keys. Use `@noAutoMap` on children/nested properties
  when explicit mappings should replace AutoMap.
- `@fromAllEvents` / fluent `fromAll()` subscribe to every event type.
  `@fromEvery` maps only already-subscribed events; `@fromAll` is its deprecated
  restricted-mapping alias, **not** all-event subscription.
- Composite key parts can use `set(target).toEventContextProperty(...)`,
  `.toEventSourceId()`, or `.toValue(...)`; `usingKeyFromContext(...)` selects a
  context key.
- `ChronicleOptions.readModelNamingPolicy` changes the storage container name,
  not the model id. Align it with Arc MongoDB's naming policy when integrating;
  Arc's `withMongoDB` + Arc-owned `withChronicle` do this by default.

### Append-time constraints

`@unique()` and repeatable `@removeConstraint(name)` are exported from
`@cratis/chronicle` and `@cratis/chronicle/events` since 6.8.0.
`@unique(name?, message?)` or `@unique({ name, message, eventSequences })`
marks a property unique across sources, or a class unique per source.
Same-named declarations merge; conflicting scopes throw.

Use `@constraint()` plus `IConstraint.define(builder)` for composite, scoped,
case-insensitive or multi-type rules. The builder offers `unique(...)`,
`uniqueFor(...)`, `perEventSourceType`, `perEventStreamType`, and
`perEventStreamId`. These protect appends, not command pre-validation.
