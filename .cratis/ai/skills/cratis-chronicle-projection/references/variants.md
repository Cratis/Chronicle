<!-- cratis-ai-managed: skills/cratis-chronicle-projection/references/variants.md -->
# Variants — mutually exclusive read models for one entity

Verified against `Cratis.Chronicle` `19.1.0` (first version carrying this
feature; the skill's own baseline in `SKILL.md` is `18.3.0` — variants need at
least `19.1.0`).

Reach for variants when a single logical entity has genuinely different shapes
across its lifecycle — a work item that is a backlog entry, then a pull
request, then closed — and modeling it as one read model would mean a `Status`
column plus an ever-growing set of properties that are only meaningful in one
stage. A single read model with a `Status` flag is still correct when the
stages differ only by a flag or two; reach for variants only when the shapes
themselves diverge. See `choosing-a-read-model-style` in the Chronicle docs for
the wider decision.

Each variant is an ordinary projection — model-bound or fluent — with its own
shape. Entering one variant automatically removes the entity from every other
variant in the same group, so at any point an entity exists in exactly one
variant. There is no CLR relationship required between the variants or the
identity type; the identity type's only job is to anchor the group.

## Declaring a group — model-bound

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record IssueCreated(string Title);

[EventType]
public record PullRequestCreated(string PullRequestUrl);

// Anchors the group. Does not need to be a read model itself, and does not
// need a common CLR base type with any of the variants.
public class WorkItem;

[VariantOf<WorkItem>]
[EntersOn<IssueCreated>]
public record BacklogItem([property: Key] Guid Id, string Title);

[VariantOf<WorkItem>]
[EntersOn<PullRequestCreated>]
public record PullRequestItem([property: Key] Guid Id, [property: SetFrom<PullRequestCreated>] string PullRequestUrl);
```

`[VariantOf<TIdentity>]` targets a class or struct, is not repeatable, and
lives in `Cratis.Chronicle.Projections.ModelBound`. `TIdentity` is what groups
variants — every type marked `[VariantOf<WorkItem>]` is mutually exclusive with
every other. `[EntersOn<TEvent>]` targets a class or struct, **is repeatable**
(a variant can be entered by more than one event type), and takes an optional
`key` constructor argument naming the event property that identifies the
instance — it defaults to the event source id.

When `IssueCreated` is processed, a `BacklogItem` is created. When
`PullRequestCreated` is processed for the same entity, `PullRequestItem` is
created **and `BacklogItem` is removed automatically** — no `[RemovedWith<T>]`
needed, the group handles it. Variants do not have to be entered in order: if
`PullRequestCreated` arrives for an entity that was never a `BacklogItem`,
`PullRequestItem` is still created.

## Declaring a group — fluent (`IProjectionFor<T>`)

```csharp
using Cratis.Chronicle.Projections;

// Anchors the group, same rules as above.
public class WorkItem;

public record BacklogItem(Guid Id, string Title);
public record PullRequestItem(Guid Id, string PullRequestUrl);

public class BacklogItemProjection : IProjectionFor<BacklogItem>
{
    public void Define(IProjectionBuilderFor<BacklogItem> builder) => builder
        .VariantOf<WorkItem>(_ => _.Id)
        .EntersOn<IssueCreated>();
}

public class PullRequestItemProjection : IProjectionFor<PullRequestItem>
{
    public void Define(IProjectionBuilderFor<PullRequestItem> builder) => builder
        .VariantOf<WorkItem>(_ => _.Id)
        .EntersOn<PullRequestCreated>();
}
```

`VariantOf<TIdentity>(Expression<Func<TReadModel, object?>> keyAccessor)` and
`EntersOn<TEvent>()` are members of `IProjectionBuilderFor<TReadModel>` itself
(`Cratis.Chronicle.Projections`), alongside `FromEventSequence`,
`ContainerName`, `NotRewindable`, and `Passive`. Unlike the model-bound
attribute, the fluent `VariantOf` call takes the key accessor explicitly —
there is no separate `[Key]` to infer it from. AutoMap still applies exactly as
for any other fluent projection.

## Only the entering event can create a variant

Every other `.From<TEvent>()` (fluent) or `[SetFrom<T>]` (model-bound) the
variant declares works exactly as it would on an ordinary projection — with one
difference: because the event is not the one named in `[EntersOn<T>]` /
`.EntersOn<T>()`, Chronicle automatically reclassifies that mapping into an
**update-only join** on the variant's own key before the definition reaches the
projection engine. It can update an already-active instance; it can never
create or resurrect one.

```csharp
// Model-bound
[VariantOf<WorkItem>]
[EntersOn<PullRequestCreated>]
public record PullRequestItem(
    [property: Key] Guid Id,
    [property: SetFrom<PullRequestCreated>] string PullRequestUrl,
    [property: SetFrom<BuildCompleted>] string BuildStatus); // update-only: BuildCompleted is not the entering event

// Fluent
public class PullRequestItemProjection : IProjectionFor<PullRequestItem>
{
    public void Define(IProjectionBuilderFor<PullRequestItem> builder) => builder
        .VariantOf<WorkItem>(_ => _.Id)
        .EntersOn<PullRequestCreated>()
        .From<BuildCompleted>(); // update-only, same reason
}
```

**Why this matters:** without the reclassification, an out-of-order or
replayed `BuildCompleted` could resurrect a `PullRequestItem` for an entity
that has since moved to another variant — or one that was never a pull request
at all. Declaring `[EntersOn<T>]` / `.EntersOn<T>()` is what makes that
structurally impossible; you do not have to reason about event ordering
yourself.

## Sharing handlers across variants

**Model-bound** has a dedicated mechanism: `[GlobalFor<TIdentity>]` on a type
carrying the shared mappings as ordinary members. Every mapping is merged into
every variant of `TIdentity`:

```csharp
[EventType]
public record TitleChanged(string Title);

[GlobalFor<WorkItem>]
public record WorkItemSharedHandlers([property: SetFrom<TitleChanged>] string Title);
```

Every variant in the group **must** have the member a shared mapping targets —
a variant missing it is a declaration error caught when the projection is
discovered, not a silently skipped mapping at runtime. A shared handler is
always update-only, exactly like any non-entering event, so it is safe to
share across variants that enter at different times: it can never create or
resurrect one on its own.

**Fluent has no equivalent of `[GlobalFor<T>]`.** Each `IProjectionFor<T>` is
its own class producing its own definition, so a mapping every variant needs is
declared with `.From<TEvent>()` on each variant's builder individually. If that
repetition becomes a maintenance concern, that is itself a reason to reach for
model-bound projections for that group instead.

## Startup-crash trap

**A variant with no `[EntersOn<T>]` / `.EntersOn<T>()` throws
`VariantMustDeclareEntersOnEvent` at startup.** A variant that could never be
entered could never be created — and because every other handler on a variant
is update-only, it would silently never be written to at all, so Chronicle
refuses to start rather than let that pass silently.

## Best practices

1. **Pick an identity type that means something on its own** — not a marker
   interface with no purpose beyond grouping. It is the type every variant (and
   every shared handler) points back to.
2. **Give every variant only the properties that stage of the entity actually
   has.** A property every stage needs is either a `[GlobalFor<T>]` handler
   (model-bound) or repeated on each variant's builder (fluent) — never a
   reason to fall back to one shared shape with nullable columns.
3. **Reach for variants only when the shapes genuinely diverge.** A single read
   model with a `Status` property is still correct when every stage shares
   almost all of its properties and differs only in a flag or two.
4. **Each variant is its own collection; there is no query that spans a whole
   group.** A caller that needs "this entity, whichever stage it's in" queries
   each variant explicitly rather than assuming a single combined collection.
