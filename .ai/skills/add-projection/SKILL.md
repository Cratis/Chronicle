---
name: add-projection
description: Use this skill when asked to add a Chronicle projection to a Cratis-based project. Favor model-bound projections by default, and only fall back to declarative/fluent `IProjectionFor<T>` projections when model-bound attributes cannot express the behavior cleanly. Enforces the AutoMap-first rule and Chronicle-specific join semantics.
---

Add a Chronicle **projection** that populates a read model from events.

> For **reactors** (automation / translation), see the `add-reactor` skill instead.

## Projection — Model-Bound (preferred)

Put projection metadata directly on the read model using attributes. No separate class needed.

```csharp
[ReadModel]
[FromEvent<SomeEventHappened>]              // auto-maps all matching property names
public record <ReadModelName>(
    [Key] <IdType> Id,                      // marks the primary key
    <PropType> <PropName>)                  // auto-mapped from SomeEventHappened
{
    public static ISubject<IEnumerable<<ReadModelName>>> All(IMongoCollection<<ReadModelName>> collection) =>
        collection.Observe();
}
```

**Attribute reference:**
| Attribute | Purpose |
|-----------|---------|
| `[FromEvent<T>]` | Maps event `T` onto the read model; matching property names map automatically (AutoMap is on by default — never call `.AutoMap()`) |
| `[FromEvent<T>(key: nameof(T.Prop))]` | Same, but uses `Prop` as the read model key instead of EventSourceId |
| `[Key]` | Marks the primary key property |
| `[SetFrom<T>(nameof(T.Prop))]` | Explicitly maps one property from event T |
| `[AddFrom<T>(nameof(T.Prop))]` | Adds event property value to the read model property |
| `[SubtractFrom<T>(nameof(T.Prop))]` | Subtracts event property value |
| `[ChildrenFrom<T>(key: nameof(T.Prop))]` | Projects into a nested child collection |
| `[Join<T>(on: nameof(Prop), eventPropertyName: nameof(T.EProp))]` | Joins data from a related event |
| `[RemovedWith<T>]` | Marks the instance as removed when event T is appended |

**Critical rules:**
- Joins must be on Chronicle **events** — NEVER join on a read model type
- If property names between event and read model match, `[FromEvent<T>]` alone is sufficient
- Child types also support all attributes recursively

## Projection — Fluent / declarative (fallback for complex cases)

Use `IProjectionFor<T>` only when the projection logic is too complex for model-bound attributes or would become less clear if forced into attributes.

```csharp
public class <Name>Projection : IProjectionFor<<ReadModel>>
{
    public void Define(IProjectionBuilderFor<<ReadModel>> builder) =>
        builder
            .From<SomeEventHappened>(b =>
                b.UsingKey(e => e.SomeId))
            .RemovedWith<SomeThingRemoved>();
}
```

**Critical rules:**
- AutoMap is on by default — just call `.From<>()` directly. Only call `.AutoMap()` if you previously used `.NoAutoMap()`.
- Joins are on Chronicle **events** only — NEVER join on the read model
- There is NO `Identifier` / `ProjectionId` property — do not add one

## Advanced patterns & startup-crash gotchas

- **`[Nested]`** projects a single child object onto a nested type. Put `[FromEvent<T>]` on the **nested type** (or use property-level `[SetFrom<T>]` on the parent when the parent already declares the event). `[NoAutoMap]` and explicit `[SetFrom<T>]` work inside the nested type; `[Nested]` can recurse inside a `[ChildrenFrom]` item.
  - ⚠️ **Duplicate-`[FromEvent<T>]` crash:** declaring class-level `[FromEvent<T>]` (no `key:`) on **both** the parent and a nested/child type for the **same** event throws a `Key: <Event>+N` duplicate-key exception at startup. Fix: keep `[FromEvent<T>]` on the nested type only, or switch the nested type to property-level `[SetFrom<T>]`.
  - ⚠️ **Duplicate-`[SetFromContext<T>]` crash:** two properties with `[SetFromContext<SameEvent>]` on the same read model crash at startup (`Key: <Event>+1`). Merge them or use `[FromEvery]`.
- **`[FromAll]` vs `[FromEvery]`:** `[FromAll]` (class-level) subscribes to **every** event type system-wide (audit/log models — pair with `[NotRewindable]`). `[FromEvery]` is a property-level capture across the events the model **already** declares via `[FromEvent<T>]` (e.g. to stamp `EventContext` data) — it does *not* subscribe to new event types.
- **Constant-key counters:** `[Count<T>(ConstantKey="metrics")]` / `[Increment<T>(ConstantKey=...)]` route all matching events into **one** aggregating document at the constant key (distinct from `.UsingConstantKey("...")` on the fluent builder).
- **Children with different key names:** when child events use different key properties, use the fluent form — `.From<Assigned>(b => b.UsingKey(e => e.Email)).From<Updated>(b => b.UsingKey(e => e.OriginalEmail))` — which model-bound `[ChildrenFrom<T>]` (single key) can't express.
- **Source selection:** class-level `[EventSequence("name")]`, `[EventLog]`, or `[EventStore("name")]` choose where the projection reads from. ⚠️ `[FromEventSequence]` is **removed** — use `[EventSequence("name")]`.
- **Cross-stream specs:** in `ReadModelScenario<T>`, seed each contributing stream with its own `Given.ForEventSource(...)`. Don't pre-emptively `[Fact(Skip=...)]` a cross-stream assertion — only skip on a reproduced harness gap, with the reason in the skip message.

## After creating

Run `dotnet build`. Fix all errors before completing.

## Appended event metadata and filtering

Chronicle correlates appended metadata in two different ways:

- **Projections** select input through event types, joins, and event sequence configuration
- **Reducers and reactors** can additionally filter by appended tags, event source type, and event stream type

Use append metadata like this:

```csharp
await eventLog.Append(
    EventSourceId.New(),
    new OrderPlaced(42m),
    eventStreamType: "fulfillment",
    eventSourceType: "order",
    tags: ["priority"]);
```

If you need metadata-based filtering for downstream processing, pair the projection with a reducer or reactor using `[FilterEventsByTag]`, `[EventSourceType]`, or `[EventStreamType]`. Projection `[Tag]` and `[Tags]` attributes label the projection definition; they do not filter appended events.

For examples, see `Documentation/events/filtering/`.

---

For the full model-bound projection attribute reference and fluent builder API, see [references/CHRONICLE-API.md](references/CHRONICLE-API.md).
