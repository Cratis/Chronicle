<!-- cratis-ai-managed: skills/cratis-chronicle-client-kotlin/references/variants.md -->
## Variants — mutually exclusive read models for one entity's lifecycle

Source: `Cratis/Chronicle.Kotlin` (`projections/VariantOf.kt`,
`EntersOn.kt`, `GlobalFor.kt`, `ProjectionsService.kt`).

Requires Chronicle.Kotlin 6.5.0 for end-to-end variant key support. Earlier definitions
stored `$eventSourceId` keys empty: replay affected projections once on upgrade.
Take the version from Maven Central, not the checked-in source.

Some entities do not have one shape for their whole lifetime — a work item is a
backlog entry until a pull request exists for it, then it is a pull request
until it merges. **Variants** let several read models share one logical
identity and be mutually exclusive: entering one variant removes the entity
from every other variant in the group, and only the event named by
`@EntersOn`/`entersOn(...)` can create or resurrect a variant — every other
event it handles is automatically reclassified into an update-only join.

**Model-bound** (the annotations are ordinary Java annotations, so Kotlin and
Java use the identical shape):

```kotlin
class WorkItem  // anchors the group; not itself a read model

@ReadModel
@VariantOf(WorkItem::class, key = "id")
@EntersOn(IssueCreated::class)
@FromEvent(IssueCreated::class)
data class BacklogItem(
    @FromEventSourceId val id: String = "",
    @SetFrom("title", IssueCreated::class) val title: String = ""
)

@ReadModel
@VariantOf(WorkItem::class, key = "id")
@EntersOn(PullRequestCreated::class)
@FromEvent(PullRequestCreated::class)
@FromEvent(BuildCompleted::class)   // not the entering event -> update-only join
data class PullRequestItem(
    @FromEventSourceId val id: String = "",
    @SetFrom("pullRequestUrl", PullRequestCreated::class) val pullRequestUrl: String = "",
    @SetFrom("buildStatus", BuildCompleted::class) val buildStatus: String = ""
)
```

`@VariantOf(identity, key)` — **`key` is always an explicit property-name
string**, on both Kotlin and Java, because reflection resolves it rather than a
compile-time property reference. `@EntersOn(eventType, key = "EventSourceId")`
is repeatable — a variant may enter on more than one event — and its `key`
names an *event* property (not the read model's), defaulting to the event
source id. A mapping shared by every variant of an identity goes on a type
annotated `@GlobalFor(identity)` instead of being repeated per variant; every
variant it targets must actually have the member it maps, or
`GlobalHandlerPropertyNotOnVariant` is thrown at registration.

**Declarative** — `variantOf` and `entersOn` are members of
`IProjectionBuilderFor<TReadModel>` itself:

```kotlin
class PullRequestItemProjection : IProjectionFor<PullRequestItem> {
    override fun define(builder: IProjectionBuilderFor<PullRequestItem>) {
        builder
            .variantOf(WorkItem::class, PullRequestItem::id)   // or variantOf(WorkItem::class, "id")
            .entersOn(PullRequestCreated::class)
            .from(BuildCompleted::class)                        // update-only, same reason
    }
}
```

Java calls the same builder with a `Class` and a string key — there is no
property-reference overload in Java:

```java
builder.variantOf(WorkItem.class, "id").entersOn(PullRequestCreated.class);
```

**A variant that declares no `@EntersOn`/`entersOn(...)` throws
`VariantMustDeclareEntersOnEvent`** at registration — a variant that could
never be entered could never be written to at all, since every other handler
on it is update-only.
