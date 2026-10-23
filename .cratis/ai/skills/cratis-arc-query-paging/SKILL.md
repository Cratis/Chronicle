---
name: cratis-arc-query-paging
description: Add server-side paging and sorting to a Cratis Arc read-model query — which return shapes Arc pages, what a query author must not write (Skip, Take, OrderBy), how MongoDB pages and sorts without LINQ, the exact query-string keys, sorting pitfalls per storage provider, and the paged frontend hooks. Use when a list query can grow large enough that returning every row is wasteful, or needs server-side sorting, or a paged query fails with `Expression not supported`. Do not use for general query creation or as a performance review.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-arc-query-paging/SKILL.md -->

# Page and sort an Arc query

Arc applies paging and sorting for you. **A query never pages or sorts itself:**
it does not write `Skip`, `Take`, `Limit` or `OrderBy` to serve `page`, `pageSize`
or `sortby`, and it does not read the query string. It chooses a **return shape**
Arc can narrow and, for MongoDB, builds that shape with the native driver API (see
the MongoDB rule) rather than LINQ.

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Verified product sources

| Package | Purpose |
| --- | --- |
| `Cratis.Arc.Core` | `QueryableQueryRenderer`, `Paging`, `Sorting`, `PagingInfo`, `IQueryContextManager` |
| `Cratis.Arc.MongoDB` | `Observe` helpers that page and sort at the source with native `Find` |
| `Cratis.Arc.EntityFrameworkCore` | `DbSet<T>` observe helpers that page at the source |
| `@cratis/arc.react` | `useWithPaging`, `useSuspenseWithPaging` |

Do not transfer a provider's ordering guarantee to another. A small, bounded result
set does not need any of this — `IEnumerable<T>` is fine.

## The wire contract

| Query-string key | Effect |
| --- | --- |
| `pageSize` | **Required to page at all** on the query string; parseable values enroll paging validation (`page >= 0`, `pageSize > 0`) — the JSON body reader, by contrast, treats `pageSize <= 0` as not paged |
| `page` | Zero-based. Defaults to `0` when `pageSize` is present |
| `sortby` | Field name; PascalCased before use, so `?sortby=name` sorts on `Name` |
| `sortDirection` | `asc`/`ascending` or `desc`/`descending`, case-insensitive; unknown values are `malformedRequest` (HTTP 400) |

Two consequences that surprise people:

- **`page` alone does nothing.** Without `pageSize` the request is not paged.
- **Sorting needs both keys.** `sortby` without `sortDirection` is ignored
  entirely.

GET paging/sorting keys are matched case-insensitively. `QueryPipeline.ValidatePaging`
runs the page validators whenever `paging.IsPaged`: negative page and zero or
negative size fail validation. This was already true. A missing sort
direction parses as ascending, but GET sorting still needs both nonempty keys.
Sources: `Arc.Core/Queries/QueryStringQueryRequestReader.cs`,
`SortDirections.cs` and `QueryPipeline.cs` under `Source/DotNET`.

## Which return shapes page

A query is an eligible public or internal static method on the `[ReadModel]`
type, discovered by its return type. Internal visibility does not hide an
endpoint; keep helpers outside that type. Read **cratis-chronicle-read-model**'s
query reference for explicit `[Query]` discovery and older-version limits.

| Return | Paged? | How |
| --- | --- | --- |
| `T`, `T?`, `IEnumerable<T>`, `List<T>`, `T[]` | No | Nothing narrows the result. `Find(...).ToListAsync()` lands here |
| `IQueryable<T>` | **Yes** | The query renderer counts, orders, then applies `Skip`/`Take` through LINQ |
| `Task<IQueryable<T>>` | **Yes** | Awaited first, then rendered as the queryable it unwraps to |
| `ISubject<IEnumerable<T>>` from `collection.Observe(...)` | **Yes, at the source** | `Observe` reads the ambient query context and applies sort, `Skip` and `Limit` to a native `Find`, and counts with `CountDocumentsAsync` |
| `ISubject<IEnumerable<T>>` you build yourself | **No** | The helper does the paging, not the renderer; a custom subject does not page automatically |
| `ISubject<IQueryable<T>>` | **No** | No `Observe` overload returns it, and the renderer matches on the outer type |

There is exactly one query renderer, and it is for `IQueryable`. Everything else
that pages does so inside its storage provider.

### MongoDB: page without LINQ

The house preference for MongoDB is the native driver API, not `AsQueryable()`.
The `IQueryable` shape is rendered with LINQ, so MongoDB translates the
renderer's `Skip`/`Take` and the sort the client asked for, and any expression the
translator cannot handle fails at request time as HTTP 500 with
`Expression not supported`. A native `Find` has no translation step, so nothing
can fail there.

For a paged MongoDB list, return what `Observe` returns and pass a
`FilterDefinition<T>`. Do not add paging or ordering:

```csharp
[ReadModel]
public record <ReadModel>(<...>)
{
    // Everything; Arc applies the client's sort, page and size.
    public static ISubject<IEnumerable<<ReadModel>>> All(IMongoCollection<<ReadModel>> collection) =>
        collection.Observe();

    // Narrow with a filter definition; Arc pages on top of the filtered set.
    public static ISubject<IEnumerable<<ReadModel>>> Active(IMongoCollection<<ReadModel>> collection) =>
        collection.Observe(Builders<<ReadModel>>.Filter.Eq(item => item.<IsArchived>, false));
}
```

`Observe` is also what serves a one-shot request: a plain `GET` or `QUERY` on an
observable query returns the current value, and the observable-query HTTP skill
covers the wait and streaming forms. The frontend uses the observable query's
`useWithPaging`, which has no `perform`.

`IQueryable<T>` stays the right shape for a LINQ-native provider such as Entity
Framework Core. If a MongoDB query still returns `IQueryable<T>`, treat it as a
known translation risk and move it to `Observe`.

⚠️ **`Expression not supported ... Skip(0).Take(30).Cast()` on a paged MongoDB
query.** Arc up to 22.50.5 appended LINQ `Cast<object>()` to the paged queryable
while running read-model interceptors, and MongoDB cannot translate it, so every
paged `IQueryable` request on MongoDB returned HTTP 500 whatever the query
looked like. Arc 22.50.6 enumerates before casting. Upgrade first; the guidance
above is what keeps the next unsupported expression from reaching the translator.

`ARC0001` is an **error** when a query method's return type is none of the
allowed shapes for its read model, and `ARC0014` is an error when a
query-shaped method on a read model is generic — a generic method is registered
and routed but can never be invoked, because there is nothing to close its type
parameters with.

Paged MongoDB observations re-read the page and total on each
change; unpaged observations retain incremental membership. The watch does not
always fetch the whole collection. MongoDB observation breaks sort ties on `_id`
and orders unsorted pages on `_id`. See the read-model skill's [versioned query-cost guidance](../cratis-chronicle-read-model/references/queries.md#cost-and-composition).

## Page the source, not the answer

- Do not `.ToList()` before returning an `IQueryable<T>` — that defeats
  `Skip`/`Take`.
- Do not hard-code `Take(n)`, `Skip(n)` or `Limit(n)`; it fights `pageSize`.
- Do not apply `OrderBy`/`ThenBy`/`Sort` to serve client-controlled sorting. On
  `IQueryable<T>` the renderer applies the client's `OrderBy` after your query;
  on `Observe` the helper applies the sort.
- ⚠️ `(await ...).AsQueryable()` **pages correctly and costs everything.**
  LINQ-to-objects honours `Skip`/`Take`, so the rows are right and the whole set
  was read to produce them. Nothing in the build or the specs will tell you.

Note also that the renderer calls `Count()` on the queryable on **every**
request to fill `totalItems`, before applying the order and the window. That is
a second round trip to the store per query.

## Sorting behaves differently per shape

⚠️ This is the sharpest edge here. The client's `sortby`/`sortDirection` is the
only sort Arc applies; the query does not supply one.

- **MongoDB `Observe`.** The sort field is looked up in the document's BSON class
  map by member name and applied by its **BSON element name**, so concept-typed
  members sort by the stored value and renamed elements work. An unknown field
  degrades to unsorted rather than failing. Ties break on `_id` ascending, and a
  paged request with no sort is ordered by `_id`, so page boundaries are stable
  without a default order.
- **`IQueryable<T>`.** The sort field is resolved with
  `ElementType.GetProperty(field)` and dereferenced without a guard. **An unknown
  `sortby` is a server error (HTTP 500), not an ignored parameter.** Constrain the
  field if callers can choose it. The renderer applies the client's sort with
  `Queryable.OrderBy`, which starts a new primary ordering rather than a
  `ThenBy`, so an order you wrote is not combined with it. The Entity
  Framework Core observe path degrades like MongoDB.
- **No server-side default order.** No `Observe` overload takes a sort, and a
  paged request without `sortby` is ordered by `_id`. Do not add an `OrderBy` to
  fake one. If a screen needs a meaningful initial order, pass it from the
  frontend as the initial `sorting` argument to `useWithPaging(pageSize, args,
  sorting)`; otherwise the order is `_id`.

`sortby` is PascalCased on the server, so the client sends the camelCase
property name the generated proxy uses and the server resolves the CLR property.
Generated proxies provide field-named sort helpers.

For composed MongoDB/EF observations, pass `ignoreQueryContext: true` to
auxiliary sources so only the primary consumes client paging/sorting and writes
totals. Single-instance observations emit `default` when absent, deleted or
outside the filter; guard `hasData`/`isReady` in React.
Source: [MongoDB observations](https://github.com/Cratis/Arc/blob/main/Documentation/backend/csharp/mongodb/observing-collections.md).

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

Paging and sorting are the framework's responsibility. A specification should pin
which rows the query selects, not the page window or the client's order.

⚠️ A query method that takes a storage collection type cannot be reached from a
read-model scenario: the scenario materializes read models in memory and has no
collection to hand such a method. Specify the projection through the scenario
and assert on the materialized instances; if the selection logic itself is worth
pinning, keep it in a method a specification can supply arguments to.

## Verify

- A MongoDB list returns `collection.Observe(<FilterDefinition>)`, not
  `AsQueryable()`; a LINQ-native provider returns `IQueryable<T>` /
  `Task<IQueryable<T>>`. Never `ISubject<IQueryable<T>>`.
- The query contains no `Skip`, `Take`, `Limit`, `OrderBy` or `Sort` serving the
  client's page or sort.
- Nothing materializes the set before returning it.
- Any default order a screen needs is the frontend's initial `sorting`, not a server `OrderBy`.
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
