---
name: cratis-arc-query-paging
description: Add server-side paging and sorting to a Cratis Arc read-model query — which return shapes Arc pages, the exact query-string keys, sorting pitfalls per storage provider, and the paged frontend hooks. Use when a list query can grow large enough that returning every row is wasteful, or needs server-side sorting. Do not use for general query creation or as a performance review.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-query-paging/SKILL.md -->

# Page and sort an Arc query

Arc applies paging and sorting for you. You never write `Skip`/`Take` or read
the query string — you choose a **return shape** that the framework can narrow.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Arc.Core` | `22.10.4` | `QueryableQueryRenderer`, `Paging`, `Sorting`, `PagingInfo` |
| `Cratis.Arc.MongoDB` | `22.10.4` | `Observe` helpers that page at the source |
| `Cratis.Arc.EntityFrameworkCore` | `22.10.4` | `DbSet<T>` observe helpers that page at the source |
| `@cratis/arc.react` | `22.10.4` | `useWithPaging`, `useSuspenseWithPaging` |

Reverify before claiming support for another version. A small, bounded result
set does not need any of this — `IEnumerable<T>` is fine.

## The wire contract

| Query-string key | Effect |
| --- | --- |
| `pageSize` | **Required to page at all.** Must be greater than 0 |
| `page` | Zero-based. Defaults to `0` when `pageSize` is present |
| `sortby` | Field name; PascalCased before use, so `?sortby=name` sorts on `Name` |
| `sortDirection` | `desc` (case-insensitive) sorts descending; **anything else, including a missing value, is ascending** |

Two consequences that surprise people:

- **`page` alone does nothing.** Without `pageSize` the request is not paged.
- **Sorting needs both keys.** `sortby` without `sortDirection` is ignored
  entirely.

Paging is validated before the query runs: `page` must be `>= 0` and `pageSize`
must be `> 0`, and a violation comes back as a validation failure rather than a
crash.

## Which return shapes page

A query is a public static method on the `[ReadModel]` type, discovered by its
return type.

| Return | Paged? | How |
| --- | --- | --- |
| `T`, `T?`, `IEnumerable<T>`, `List<T>`, `T[]` | No | Nothing narrows the result |
| `IQueryable<T>` | **Yes** | The query renderer counts, orders, then applies `Skip`/`Take` |
| `Task<IQueryable<T>>` | **Yes** | Awaited first, then rendered as the queryable it unwraps to |
| `ISubject<IEnumerable<T>>` | **Yes** | Not by the renderer — the storage `Observe()` helpers read the ambient query context and page at the source, reapplying on every change |
| `ISubject<IQueryable<T>>` | **No** | No `Observe` overload returns it, and the renderer matches on the outer type |

There is exactly one query renderer, and it is for `IQueryable`. Everything else
that pages does so inside its storage provider.

```csharp
[ReadModel]
public record <ReadModel>(<...>)
{
    public static IQueryable<<ReadModel>> All(<Collection> collection) =>
        collection.AsQueryable();

    // Filter before returning; Arc pages on top of the filtered queryable.
    public static IQueryable<<ReadModel>> Active(<Collection> collection) =>
        collection.AsQueryable().Where(_ => !_.<IsArchived>);

    // A default order so paging is stable when the caller sends no sortby.
    public static IQueryable<<ReadModel>> AllByName(<Collection> collection) =>
        collection.AsQueryable().OrderBy(_ => _.<Name>);
}
```

For a live paged list, return what `Observe` returns:

```csharp
public static ISubject<IEnumerable<<ReadModel>>> AllLive(<Collection> collection) =>
    collection.Observe(_ => _.Find(item => !item.<IsArchived>));
```

`ARC0001` is an **error** when a query method's return type is none of the
allowed shapes for its read model, and `ARC0014` is an error when a
query-shaped method on a read model is generic — a generic method is registered
and routed but can never be invoked, because there is nothing to close its type
parameters with.

## Page the source, not the answer

- Do not `.ToList()` before returning an `IQueryable<T>` — that defeats
  `Skip`/`Take`.
- Do not hard-code `Take(n)`; it fights `pageSize`.
- ⚠️ `(await ...).AsQueryable()` **pages correctly and costs everything.**
  LINQ-to-objects honours `Skip`/`Take`, so the rows are right and the whole set
  was read to produce them. Nothing in the build or the specs will tell you.

Note also that the renderer calls `Count()` on the queryable on **every**
request to fill `totalItems`, before applying the order and the window. That is
a second round trip to the store per query.

## Sorting behaves differently per shape

⚠️ This is the sharpest edge here.

- For `IQueryable<T>`, the sort field is resolved with `ElementType.GetProperty(field)`
  and the result is dereferenced without a guard. **An unknown `sortby` is a
  server error (HTTP 500), not an ignored parameter.** Validate or constrain the
  field before it reaches the query if callers can choose it.
- The MongoDB and Entity Framework Core observe paths deliberately **degrade**:
  an unknown sort field leaves the result unsorted rather than failing.

Because `sortby` is PascalCased, the client sends the camelCase property name
the generated proxy uses and the server resolves the CLR property.

Give any list with a natural order a default `OrderBy`. Paging without one is
unstable across storage providers, which return unordered results in different
orders.

## Consume it from the frontend

```tsx
const [result, perform, setSorting, setPage, setPageSize] =
    <QueryName>.useWithPaging(<pageSize>);

// Observable queries have no perform:
const [liveResult, setSorting2, setPage2, setPageSize2] =
    <ObservableQueryName>.useWithPaging(<pageSize>);
```

⚠️ **The tuple shapes differ.** A plain query's `useWithPaging` returns five
elements — result, `perform`, `setSorting`, `setPage`, `setPageSize`. An
observable query's returns four; there is nothing to re-perform because it is
already streaming. Destructuring one as the other silently binds the wrong
functions.

`useSuspenseWithPaging` is the suspense variant of each, with the same shapes.

`result.paging` is `{ page, size, totalItems, totalPages }`. `page` is
**zero-based** — render `page + 1` in a label and pass zero-based values to
`setPage`. `totalPages` is computed from `totalItems / size`, and is `0` when the
result is not paged.

## Specify the data contract, not the paging

Paging is the framework's responsibility. A specification should pin which rows
the query selects and in what order.

⚠️ A query method that takes a storage collection type cannot be reached from a
read-model scenario: the scenario materializes read models in memory and has no
collection to hand such a method. Specify the projection through the scenario
and assert on the materialized instances; if the selection logic itself is worth
pinning, keep it in a method a specification can supply arguments to.

## Verify

- The query returns `IQueryable<T>` / `Task<IQueryable<T>>` for one-shot paging,
  or `ISubject<IEnumerable<T>>` for a live paged list — never
  `ISubject<IQueryable<T>>`.
- Nothing materializes the set before returning it.
- A meaningful default order exists wherever the data has one.
- A caller-supplied `sortby` cannot reach an `IQueryable<T>` query as an unknown
  field.
- The frontend destructures the tuple shape that matches the query kind.
- `page` is treated as zero-based everywhere.
- `dotnet build` is clean in Debug and Release with `ARC0001` and `ARC0014`
  silent.

## Route near misses

- Creating the read model or its projection: the Chronicle read-model guidance.
- Inspecting an observable query over HTTP: `cratis-arc-observable-query-http`.
- Whole-slice performance work: the performance review guidance.
