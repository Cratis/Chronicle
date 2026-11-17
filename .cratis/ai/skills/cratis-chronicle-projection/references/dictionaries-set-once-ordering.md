<!-- cratis-ai-managed: skills/cratis-chronicle-projection/references/dictionaries-set-once-ordering.md -->
# Dictionary keys, set once and children ordering (.NET)

Read this when a projection keeps a first value, writes one entry per key into a dictionary property, or keeps children in a chosen order.
Requires Chronicle 19.150.0 (check the installed `Cratis.Chronicle` package).

## Fluent

```csharp
builder
    .From<ItemSold>(_ => _
        .SetOnce(m => m.OriginalTitle).To(e => e.Title)
        .SetOnce(m => m.Created).ToEventContextProperty(c => c.Occurred)
        .Add(m => m.UnitsByCategory, e => e.Category).With(e => e.Quantity)
        .Increment(m => m.SalesByCategory, e => e.Category)
        .Count(m => m.SalesByUser, c => c.CausedBy.Subject)
        .SetOnce(m => m.FirstSaleByCategory, e => e.Category).ToEventContextProperty(c => c.Occurred))
    .From<ItemReturned>(_ => _
        .Subtract(m => m.UnitsByCategory, e => e.Category).With(e => e.Quantity)
        .Decrement(m => m.SalesByCategory, e => e.Category))
    .From<CategoryRetired>(_ => _.RemoveKey(m => m.UnitsByCategory, e => e.Category))
    .Children(m => m.Comments, children => children
        .IdentifiedBy(c => c.CommentId)
        .Prepend()
        .From<CommentPosted>(_ => _.UsingKey(e => e.CommentId)))
    .Children(m => m.Tasks, children => children
        .IdentifiedBy(t => t.TaskId)
        .OrderByDescending(t => t.Priority)
        .From<TaskAdded>(_ => _.UsingKey(e => e.TaskId)));
```

## Model-bound

| Need | Attribute |
| --- | --- |
| Keep the first value | `SetOnce = true` on `[SetFrom<E>]` or `[SetFromContext<E>]` |
| Newest child first | `[ChildrenFrom<E>(key: ..., Prepend = true)]` |
| Sorted children | `[ChildrenFrom<E>(key: ..., OrderBy = nameof(Child.Prop))]` or `OrderByDescending = ...` |

A collection takes one ordering; different orderings or an unknown sort property fail at build time.

## Semantics

- A dictionary property must be a dictionary (or a property-less object); keys are strings. `Add`, `Subtract`, `Increment`, `Decrement` and `Count` use 64-bit integers.
- An event-content key must be non-empty, contain no `.`, `[`, `]` or NUL, and not start with `$`; a bad key fails the event and the partition shows the failure. Context and `CausedBy` keys are not checked (`john.doe` nests), so prefer `CausedBy.Subject`. A null or missing key skips the mapping; a key path the event type lacks fails at registration.
- `SetOnce` assigns again after the property was cleared to null.
- Equal sort values tie-break by child identifier ascending.
- Choose a projection over a reducer when the only state is a per-key map, a first value, or child order, with no branching on prior state.
