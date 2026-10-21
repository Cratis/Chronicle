<!-- cratis-ai-managed: skills/cratis-chronicle-client-typescript/references/variants.md -->
## Variants — mutually exclusive read models for one entity's lifecycle

Requires `@cratis/chronicle` 6.2.0 or later.

Source: `Source/projections/VariantReclassifier.ts` and model-bound decorators.
Read models are inferred; use `static readModelId` for a custom id.

Some entities do not have one shape for their whole lifetime — a work item is a
backlog entry until a pull request exists for it, then it is a pull request
until it merges. **Model-bound** — `@variantOf(identity, key)` and
`@entersOn(eventType, key?)`, alongside the ordinary `@fromEvent`/`@setFrom`
decorators:

```typescript
class WorkItem {}   // anchors the group; not itself a read model

@variantOf(WorkItem, 'id')
@entersOn(IssueCreated)
@fromEvent(IssueCreated)
class BacklogItem {
    id = '';
    @setFrom(IssueCreated, 'title') title = '';
}

@variantOf(WorkItem, 'id')
@entersOn(PullRequestCreated)
@fromEvent(PullRequestCreated)
@fromEvent(BuildCompleted)          // not the entering event -> update-only join
class PullRequestItem {
    id = '';
    @setFrom(PullRequestCreated, 'pullRequestUrl') pullRequestUrl = '';
    @setFrom(BuildCompleted, 'buildStatus') buildStatus = '';
}
```

`entersOn` is repeatable (a variant may enter on more than one event) and its
`key` argument names an *event* property, defaulting to the event source id. A
mapping shared by every variant of an identity goes on a class decorated
`@globalFor(identity)` instead of being repeated on each variant — every
variant it targets must actually declare the property it maps, or
`GlobalHandlerPropertyNotOnVariant` is thrown when the group is built. A class
carrying only `@globalFor` is never itself registered as a projection.

**Declarative** — `variantOf` and `entersOn` are members of
`IProjectionBuilderFor<TReadModel>` itself:

```typescript
@projection()
class PullRequestItemProjection implements IProjectionFor<PullRequestItem> {
    define(builder: IProjectionBuilderFor<PullRequestItem>): void {
        builder
            .variantOf(WorkItem, m => m.id)
            .entersOn(PullRequestCreated)
            .from(BuildCompleted);   // update-only, same reason
    }
}
```

**A variant that declares no `@entersOn`/`entersOn(...)` throws
`VariantMustDeclareEntersOnEvent`** when the group is built — a variant that
could never be entered could never be written to at all, since every other
handler on it is update-only.
