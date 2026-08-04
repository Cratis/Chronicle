---
name: query-paging
description: Add server-side paging and sorting to a Cratis read-model query — return IQueryable<T> for one-shot paging, ISubject<IEnumerable<T>> for observable + paged results, and consume them from React with useWithPaging. Use when a list query can grow large enough that returning all rows is wasteful, or needs server-side sorting.
---

# Query Paging

Arc applies paging and sorting automatically to any model-bound query that returns `IQueryable<T>`. The HTTP layer reads `?page=`, `?pageSize=`, `?sortby=`, `?sortDirection=` and applies them before the query materializes — you don't write skip/take/sort. If a result set is small and bounded, `IEnumerable<T>` is fine and you don't need this.

## Steps

### 1. One-shot paged query — `IQueryable<T>`

```csharp
[ReadModel]
public record Project(...)
{
    public static IQueryable<Project> AllProjects(IMongoCollection<Project> collection) =>
        collection.AsQueryable();

    // Filtered — apply the predicate before returning; Arc pages on top of it:
    public static IQueryable<Project> ActiveProjects(IMongoCollection<Project> collection) =>
        collection.AsQueryable().Where(p => !p.IsArchived);

    // Sensible default order when the caller passes no sortby:
    public static IQueryable<Project> AllByName(IMongoCollection<Project> collection) =>
        collection.AsQueryable().OrderBy(p => p.Name);
}
```

### 2. Observable + paged — `ISubject<IEnumerable<T>>`

For live updates *and* paging, return what `Observe` returns:

```csharp
public static ISubject<IEnumerable<Project>> AllProjectsLive(IMongoCollection<Project> collection) =>
    collection.Observe(_ => _.Find(p => !p.IsArchived));
```

Each emission carries page metadata alongside the data. Sorting and paging are applied **at the source** and re-applied on every change — the storage `Observe` helpers read the ambient query context themselves rather than going through the query renderer.

⚠️ **Do not wrap it as `ISubject<IQueryable<T>>`.** No `Observe` overload returns that shape, and the renderer matches on the outer type, so nothing would page it.

### 3. What does and doesn't page

| Return type | Paging? | How |
|---|---|---|
| `T`, `T?`, `IEnumerable<T>`, `List<T>`, `T[]` | No | Nothing narrows the result |
| `IQueryable<T>` | **Yes — auto-paged** | The query renderer applies `OrderBy`/`Skip`/`Take` to the queryable |
| `Task<IQueryable<T>>` | **Yes — auto-paged** | The result is awaited first, then rendered as the queryable it unwraps to |
| `ISubject<IEnumerable<T>>` | **Yes — paged and sorted** | Not by the renderer. The storage `Observe()` helpers read the ambient query context themselves and apply sorting and paging at the source, re-applying them on every change |
| `ISubject<IQueryable<T>>` | **No — not a shipped shape** | No `Observe` overload returns it, and the renderer matches on the outer type, so it never fires |

Don't `.ToList()` before returning `IQueryable<T>` (defeats skip/take) and don't hard-code `Take(n)` (conflicts with `pageSize`).

⚠️ **Returning an already-materialized collection as a queryable — `(await …).AsQueryable()` — pages *correctly* and costs everything.** LINQ-to-objects honours `Skip`/`Take`, so the results are right and the whole set was read to produce them. Page the source, not the answer.

### 4. Frontend hooks

```tsx
const [result, perform, setSorting, setPage, setPageSize] =
    AllProjects.useWithPaging(25 /* pageSize */, args?, sorting?);
// suspense: AllProjects.useSuspenseWithPaging(25)
// observable + paged: AllProjectsLive.useSuspenseWithPaging(25)
```

`result.paging` = `{ page, size, totalItems, totalPages }`. `page` is **zero-based** — show `page + 1` in labels, pass zero-based to `setPage`/`?page=`. All paging hooks support the `.when(condition)` prefix.

### 5. Spec the data contract

Paging is the framework's responsibility; the spec covers the data shape — which rows the query selects, and in what order.

⚠️ **A query method that takes `IMongoCollection<T>` cannot be reached from `ReadModelScenario<T>`.** The scenario materializes read models in memory and exposes no collection, so there is nothing to hand such a method. Spec the projection through the scenario and assert on the materialized instances; if the selection logic itself is worth pinning, keep it in a method that takes what a spec can supply.

```csharp
void Because() => _result = _scenario.Instances.Values;
[Fact] void should_only_include_active() => _result.All(p => !p.IsArchived).ShouldBeTrue();
```

## Quality gate

- [ ] Build is clean.
- [ ] Query returns `IQueryable<T>` (or `Task<IQueryable<T>>`) for one-shot paging, or `ISubject<IEnumerable<T>>` for an observable paged list. Not `ISubject<IQueryable<T>>` - nothing renders it.
- [ ] A meaningful default sort is applied where the data has a natural order - paging without one is unstable across storage providers, which order differently.
- [ ] The source is paged, not the answer: no `.ToList()`/`.AsQueryable()` that reads everything first.

## See also

- `vertical-slices.md` — read-model query return shapes.
- `react.md` — consuming paged queries (`useWithPaging`, `DataPage`).
