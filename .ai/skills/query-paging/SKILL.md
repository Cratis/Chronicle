---
name: query-paging
description: Add server-side paging and sorting to a Cratis read-model query — return IQueryable<T> for one-shot paging, ISubject<IQueryable<T>> for observable + paged results, and consume them from React with useWithPaging. Use when a list query can grow large enough that returning all rows is wasteful, or needs server-side sorting.
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

### 2. Observable + paged — `ISubject<IQueryable<T>>`

For live updates *and* paging:

```csharp
public static ISubject<IQueryable<Project>> AllProjectsLive(IMongoCollection<Project> collection) =>
    collection.Observe(_ => _.AsQueryable());
```

Each emission carries page metadata alongside the data.

### 3. What does and doesn't page

| Return type | Paging? |
|---|---|
| `T`, `T?`, `IEnumerable<T>`, `List<T>`, `T[]` | No |
| `IQueryable<T>` | **Yes — auto-paged** |
| `ISubject<IEnumerable<T>>` | No (full observable set) |
| `ISubject<IQueryable<T>>` | **Yes — observable + auto-paged** |

`Task<IQueryable<T>>` and `Task<ISubject<…>>` are **not** supported — return the queryable/subject directly. Don't `.ToList()` before returning `IQueryable<T>` (defeats skip/take) and don't hard-code `Take(n)` (conflicts with `pageSize`).

### 4. Frontend hooks

```tsx
const [result, perform, setSorting, setPage, setPageSize] =
    AllProjects.useWithPaging(25 /* pageSize */, args?, sorting?);
// suspense: AllProjects.useSuspenseWithPaging(25)
// observable + paged: AllProjectsLive.useSuspenseWithPaging(25)
```

`result.paging` = `{ page, size, totalItems, totalPages }`. `page` is **zero-based** — show `page + 1` in labels, pass zero-based to `setPage`/`?page=`. All paging hooks support the `.when(condition)` prefix.

### 5. Spec the data contract

Paging is the framework's responsibility; the spec covers the data shape:

```csharp
void Because() => _result = Project.ActiveProjects(_scenario.Collection);
[Fact] void should_only_include_active() => _result.All(p => !p.IsArchived).ShouldBeTrue();
```

## Quality gate

- [ ] Build is clean.
- [ ] Query returns `IQueryable<T>` or `ISubject<IQueryable<T>>` (not `Task<…>` of either).
- [ ] A meaningful default sort is applied where the data has a natural order.

## See also

- `vertical-slices.md` — read-model query return shapes.
- `react.md` — consuming paged queries (`useWithPaging`, `DataPage`).
