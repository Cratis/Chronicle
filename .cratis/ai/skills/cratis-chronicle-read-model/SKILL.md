---
name: cratis-chronicle-read-model
description: Create a Chronicle read model and its query surface - past-tense event types, the read-model record and its key, choosing a projection or a reducer to build it, snapshot versus observable queries (including why Arc does not discover a query declared to return Subject or BehaviorSubject), and reading an instance directly through IReadModels. Use when creating a read model from scratch; for changing how an existing model is populated use the projection or reducer skill instead.
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

Sources:

| Package | Purpose |
| --- | --- |
| `Cratis.Chronicle` | `Cratis.Chronicle.Events`, `Cratis.Chronicle.Keys`, `Cratis.Chronicle.ReadModels`; style and discovery (`Source/Clients/DotNET/Reducers/{SelfReducerMethods,RegisteredReducerReadModel}.cs`, `ReadModels/ReadModelBuilderOwnership.cs`, `DefaultClientArtifactsProvider.cs`) |
| `Cratis.Arc.Core` | `Cratis.Arc.Queries.ModelBound.ReadModelAttribute` and `PathAttribute` |
| `Cratis.Arc.MongoDB` | the `Observe` family of `IMongoCollection<T>` extensions |

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
every detail and past entry. This is a design convention, **not a
one-collection framework requirement**: bounded query composition remains valid,
including when separate security or compliance boundaries must be preserved.

Embed children only when their cardinality and payload are bounded by the use
case. Keep growing activity, message, or attempt histories in separately paged
timeline models; embedding all history makes every parent read and emission
larger. Evaluate variants when lifecycle shapes genuinely diverge, not merely
because a status changes. See the projection skill for children and variants,
and [query cost and composition](references/queries.md#cost-and-composition).

## Step 3 — Choose a projection or a reducer

For application code, work down this ladder:

1. **Model-bound attributes** when the mapping reads clearly on the model.
2. **Fluent `IProjectionFor<T>`** when the definition needs structure or an
   attribute-free contract. Counters, joins, children and composite keys remain
   projection features; fluent lambdas are not arbitrary per-event code.
3. **Reducer code** when branching on prior state, calculations or guards read
   better as state transitions. A reducer is a normal choice, not a last resort.
   For new .NET code default to a **self-reducing model**: public static
   `Reduce(TEvent @event, TSelf? current[, EventContext context])` on the model,
   returning `TSelf`/`TSelf?` or `Task` of it. No marker or model activation.
   Use a separate **`IReducerFor<T>`** when the fold needs injected services,
   replay-time instance state or separation from the model.
4. **Nominated plain instance class** only when the contract/reducer assembly
   cannot reference Chronicle; configure `ChronicleOptions.ExplicitArtifacts`
   at the composition root before creating stores.

**PDL** is for definitions authored outside application code (Workbench,
Screenplay, ad-hoc queries), not the code-first default. This ladder does not
claim Stage renders static self reducers. The self form is .NET-only;
other SDKs keep their own reducer contracts.

Requires Chronicle 19.58.0 for self-reducing read models.

Default discovery finds projections, interface reducers and complete self shapes.
Explicit registration remains available, and plain instance reducers require
nomination; see [discovery](../cratis-chronicle-client-dotnet/references/discovery.md).
An existing complete static shape becomes a reducer on upgrade without adding a
marker. Near misses warn (CHR0058). A self model and a competing projection or
reducer in one catalog fail with `AmbiguousReadModelBuilder`; remove the old
builder when switching and verify storage/replay separately from output equality.

Read [projection behavior](../cratis-chronicle-projection/SKILL.md) for mappings
and [reducers](../cratis-chronicle-reducer/SKILL.md) for complete signatures,
null deletion, replay hooks and preserving admission/ordering guards. Reducers
are source-keyed and have no native joins; decision reads and Arc read protection `On`
admit only unfiltered reducers without constructor dependencies.

## Step 4 — Add a query surface, if the model is read from outside

Queries are **static** methods on the read-model record, discovered by Arc.
Marking the record with `[ReadModel]` from `Cratis.Arc.Queries.ModelBound` is
what gives it that surface. For a self-reducing model, put its `Reduce` methods
and queries on this same record. On an Arc older than 22.90.0, use
`[ReadModel(RequireQueryAttribute = true)]` plus `[Query]` on intended queries to
avoid exposing folds. Requires Arc 22.90.0 for the automatic behavior: Arc excludes methods taking the
model itself from query discovery, and Arc.Chronicle claims self models for
command injection. See the existing
[explicit query-discovery guidance](references/queries.md#discovery-rules).

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
the difference between snapshot and observable queries. Read
references/collection-observation.md when exposing a Chronicle-maintained list:
Requires Chronicle 19.54.0 for storage-independent `IReadModels.Observe<T>()`,
with registered keys and protected-value release.

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
`Watch<T>`, `Observe<T>(filter)`, `Release<T>`, and `Register`. **There is no
general predicate or `IQueryable` snapshot surface on `IReadModels`**.
The observation filter runs client-side after release; it does not narrow server
reads, transport, sorting or paging.

### `[Passive]` when the read must be current

`Cratis.Chronicle.ReadModels.PassiveAttribute` on the read-model type makes it
passive: no sink, nothing materialized, and the instance is computed on demand
from the events at the moment it is read. Reach for it when a read must be
current rather than eventually consistent.

**Passive is fresher, not safer.** A materialized read may lag the log; a passive
read is current when read. Neither captures a position or enrolls a guard, so
neither makes a rule hold when two requests race. To guard a decision use a decision
read over an admitted projection or unfiltered dependency-free reducer, a
constraint for uniqueness, or an aggregate;
see [choosing how a rule holds](../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md).

**`IMongoCollection<T>` reads only the sink.** It cannot compute passive state:
a fresh passive model has no maintained collection, and a formerly active one
may leave stale documents. Use Chronicle's keyed read API above (or supported
read-model parameter injection) when you need its computed state. Do not remove
`[Passive]` merely to make a sink query work when the model contains secrets.
Keep secret-bearing state on the protected access path; project only safe
metadata into a separate list/search model.

Requires Chronicle 19.77.3 for strongly consistent per-key projections to fold
from the start on every read: passive, stream-scoped, wildcard-key and session
reads immediately reflect revisions, redactions and joined-subject erasure.
Earlier warm reads can retain old content. Each read costs O(key history), not
an incremental suffix; prefer materialization for frequent long-history reads.
Check the deployed kernel tag before relying on this freshness.

The number of passive types is not itself a performance smell. Review event
history length, invocation frequency, and the consistency/security requirement;
repeated reconstruction of large histories on a hot path is the concern.
Passivity does not itself authorize access or encrypt secrets.

This access distinction is described in
([passive projections](https://github.com/Cratis/Chronicle/blob/main/Documentation/projections/declarative/passive.mdx),
[passive reducers](https://github.com/Cratis/Chronicle/blob/main/Documentation/reducers/passive-reducers.mdx)).

`[Passive]` on its own does **not** make a read model a projection. It is
deliberately excluded from the annotations that do; it changes how an existing
projection or reducer is read; it is not a builder by itself.

## Step 6 — Build and verify

Build, fix every error, then prove the behavior: drive the contributing events
through the read-model specification for this model and assert the projected
state. Seed each contributing stream with its own event source when the model
spans streams.

## Route near misses

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`): change the model first with
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
- The style follows the ladder; a self model has no competing builder, and Arc
  folds are excluded from query exposure for the consuming version.
- Query methods are `static` and declare `ISubject<...>` rather than a concrete
  subject type.
- A direct `GetInstanceById` result is checked for absence before use.
- A plain read, materialized or passive, is never presented as the guard of a
  write-side rule.
- Passive access goes through Chronicle, not a sink collection; consistency,
  access frequency, and non-materialization requirements justify the choice.
- Summary payloads and embedded children are bounded; growing histories have
  a separate paged surface, and no secret was materialized to fix a query.
- The project builds clean and the read-model specifications pass against the
  verified package versions.
