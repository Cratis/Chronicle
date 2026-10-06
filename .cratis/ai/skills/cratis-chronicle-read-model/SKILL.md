---
name: cratis-chronicle-read-model
description: Create a Chronicle read model and its query surface - past-tense event types, the read-model record and its key, choosing a projection or a reducer to build it, snapshot versus observable queries, and reading an instance directly through IReadModels. Use when creating a read model from scratch; for changing how an existing model is populated use the projection or reducer skill instead.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-read-model/SKILL.md -->

# Chronicle read models

A read model is derived state built from events. The path is:

```text
event types -> read-model record -> a projection or a reducer -> a query surface
```

Chronicle owns the events, the projection or reducer, and the stored instance.
The **query surface is Arc's**, and the two halves are separable: a read model
that only exists to be read from inside the backend needs no query surface at
all.

## Verified product sources

This skill is verified against these exact sources:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `18.3.0` | `Cratis.Chronicle.Events`, `Cratis.Chronicle.Keys`, `Cratis.Chronicle.ReadModels` |
| `Cratis.Arc.Core` | current | `Cratis.Arc.Queries.ModelBound.ReadModelAttribute` and `PathAttribute` |
| `Cratis.Arc.MongoDB` | current | the `Observe` family of `IMongoCollection<T>` extensions |

Reverify product sources before claiming support for another version. Confirm
the exact Arc package version in the consuming project before citing one.

## Step 1 — Define the events

Events are the source of truth. Each is a `record` carrying
`Cratis.Chronicle.Events.EventTypeAttribute`, named as a past-tense fact.

```csharp
using Cratis.Chronicle.Events;

/// <summary>Emitted when <description>.</summary>
[EventType]
public record <EntityName><PastTenseVerb>(<ConceptType> <PropertyName>);
```

- One clear purpose per event; do not mix concerns into one fact.
- **Avoid nullable properties.** Analyzer `CHR0012` warns on them. Model an
  optional fact as a separate event.
- Prefer strongly typed concept properties over raw primitives.
- An event never carries its own event-source id as a payload property — the id
  is in the event context.
- `[EventType]` takes `(string id = "", uint generation = 1)`. Pass neither for a
  new event: the id defaults to the CLR type name and the generation to `1`.

## Step 2 — Define the read-model record

```csharp
using Cratis.Chronicle.Keys;

public record <ReadModelName>(
    [property: Key] <IdType> Id,
    <PropertyType> <PropertyName>);
```

- `Cratis.Chronicle.Keys.KeyAttribute` takes no arguments and targets a property
  or a record constructor parameter, never the class.
- When the model's identity **is** the event-source id, that is already the key
  and `[Key]` adds nothing. Placing `[Key]` on an `EventSourceId<T>`-derived
  value is flagged by analyzer `CHR0026`.
- One read model per use case. Specialization beats a shared model reused across
  conflicting scenarios.
- Do not give read-model properties default values that could mask a projection
  that was never wired.
- Never name a property `_subject`, `__subject`, or `__subjects` — Chronicle
  reserves those for compliance-subject tracking, and analyzer `CHR0035`
  rejects them.
- `Cratis.Chronicle.ReadModels.IndexAttribute` (`[Index]`) marks a property for
  indexing in the sink.

### Shape the model for its consumer

Prefer a slim, purpose-built projection over assembling a screen by repeatedly
joining broad collections at query time. A summary needs summary fields, not
every detail and historical entry. This is a design convention, **not a
one-collection framework requirement**: bounded query composition remains valid,
including when separate security or compliance boundaries must be preserved.

Embed children only when their cardinality and payload are bounded by the use
case. Keep growing activity, message, or attempt histories in separately paged
timeline models; embedding all history makes every parent read and emission
larger. Evaluate variants when lifecycle shapes genuinely diverge, not merely
because a status changes. See the projection skill for children and variants,
and [query cost and composition](references/queries.md#cost-and-composition).

## Step 3 — Choose a projection or a reducer

| | Projection | Reducer |
| --- | --- | --- |
| Shape | the model is mostly a mapping of event fields | the next state is a function of the current state |
| Expressed as | model-bound attributes, or the fluent builder | C# control flow in a handler method |
| Reach for it | first, and second | only after both projection forms fail |

A reducer is a legitimate style when the transition genuinely depends on prior
state — it is not a failure mode. It *is* a failure mode when it re-implements
something an attribute already expresses. Work down the ladder: model-bound
attributes, then the fluent builder, then a reducer.

Both are discovered by convention. Nothing is registered.

Use the projection skill for the attribute and builder surface, and the reducer
skill for handler signatures and the nullable-current rule.

## Step 4 — Add a query surface, if the model is read from outside

Queries are **static** methods on the read-model record, discovered by Arc.
Marking the record with `[ReadModel]` from `Cratis.Arc.Queries.ModelBound` is
what gives it that surface.

```csharp
using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Keys;
using MongoDB.Driver;

[ReadModel]
public record <ReadModelName>([property: Key] <IdType> Id, <PropertyType> <PropertyName>)
{
    public static async Task<IEnumerable<<ReadModelName>>> All<PluralName>(
        IMongoCollection<<ReadModelName>> collection) =>
        await collection.Find(Builders<<ReadModelName>>.Filter.Empty).ToListAsync();

    public static ISubject<IEnumerable<<ReadModelName>>> Observe<PluralName>(
        IMongoCollection<<ReadModelName>> collection) =>
        collection.Observe();
}
```

The rules Arc's discovery actually enforces:

- The method must be **static**. Instance methods are never discovered.
- The declared return type, after unwrapping one `Task<>`, must be the read
  model, an array of it, something assignable to `IEnumerable<T>` of it,
  `IAsyncEnumerable<T>` of it, `ISubject<T>` of it, or `ISubject<>` of a
  collection of it.
- **The declared type must be `ISubject<...>`, never `Subject<...>` or
  `BehaviorSubject<...>`.** The check matches the open generic definition
  exactly, so a concrete subject type fails discovery silently. Construct a
  concrete subject inside if you like; declare `ISubject<...>`.
- Open generic methods are rejected.
- Use `[Path("<route>")]` from `Cratis.Arc.Queries.ModelBound` for a custom
  route. It targets the class or the method. It is Arc's attribute — do not
  reach for ASP.NET's `[Route]`.

Read [references/queries.md](references/queries.md) for the `Observe` family and
the difference between snapshot and observable queries.

## Step 5 — Or read the instance directly

Inside the backend, read an instance through
`Cratis.Chronicle.ReadModels.IReadModels`, reached from `IEventStore.ReadModels`:

```csharp
Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null);
Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null);
```

`ReadModelKey` converts implicitly from `string`, `Guid`, and `EventSourceId`,
so an id can be passed directly. **The declared return type is not nullable even
though no instance may exist** — check the result before dereferencing it.

Neighbouring members include `GetInstances<T>`, `GetSnapshotsById<T>`,
`Watch<T>`, `Release<T>`, and `Register`. **There is no predicate or `IQueryable`
surface on `IReadModels`** — filtered reads go through the sink, which is what
the Arc query methods above do.

### `[Passive]` when the read is a command-side decision

`Cratis.Chronicle.ReadModels.PassiveAttribute` on the read-model type makes it
passive: no sink, nothing materialized, and the instance is computed on demand
from the events at the moment it is read. Reach for it when a decision needs the
model **strongly consistent** rather than eventually consistent.

**`IMongoCollection<T>` reads only the sink.** It cannot compute passive state:
a fresh passive model has no maintained collection, and a formerly active one
may leave stale documents. Use Chronicle's keyed read API above (or supported
read-model parameter injection) when you need its computed state. Do not remove
`[Passive]` merely to make a sink query work when the model contains secrets.
Keep secret-bearing state on the protected access path; project only safe
metadata into a separate list/search model.

The number of passive types is not itself a performance smell. Review event
history length, invocation frequency, and the consistency/security requirement;
repeated reconstruction of large histories on a hot path is the concern.
Passivity does not itself authorize access or encrypt secrets.

This access distinction was checked against Chronicle **v19.31.2**
([passive projections](https://github.com/Cratis/Chronicle/blob/v19.31.2/Documentation/projections/declarative/passive.mdx),
[passive reducers](https://github.com/Cratis/Chronicle/blob/v19.31.2/Documentation/reducers/passive-reducers.mdx)).

`[Passive]` on its own does **not** make a read model a projection. It is
deliberately excluded from the annotations that do; it changes how an existing
projection is registered.

## Step 6 — Build and verify

Build, fix every error, then prove the behavior: drive the contributing events
through the read-model specification for this model and assert the projected
state. Seed each contributing stream with its own event source when the model
spans streams.

## Route near misses

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (the root holds a committed `.play` file (`git ls-tree -r --name-only HEAD -- <root>` lists it), or the project set
  `mcpServers.screenplay.root` in `.cratis/ai.json`; an empty directory, install
  output, an uncommitted `.play` draft or a `.play` file outside the root does not count; master definition:
  `cratis-screenplay-modeling-lifecycle`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Verify

- Every event is a past-tense fact with one purpose and no nullable properties.
- No event carries its own event-source id.
- The read model has exactly one key, and no `[Key]` sits on an
  `EventSourceId<T>` value.
- No reserved compliance-subject property name is used.
- Read-model properties carry no defaults that could hide unwired projection.
- Query methods are `static` and declare `ISubject<...>` rather than a concrete
  subject type.
- A direct `GetInstanceById` result is checked for absence before use.
- Passive access goes through Chronicle, not a sink collection; consistency,
  access frequency, and non-materialization requirements justify the choice.
- Summary payloads and embedded children are bounded; growing histories have
  a separate paged surface, and no secret was materialized to fix a query.
- The project builds clean and the read-model specifications pass against the
  verified package versions.
