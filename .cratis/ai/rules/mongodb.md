---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
profile: application
---
<!-- cratis-ai-managed: rules/mongodb.md -->

# MongoDB Instructions

> **Applies only to code that reads or writes MongoDB** — read-model queries, repositories, and
> projection or reducer sinks that use `IMongoCollection<T>`. Every line is tagged **[contract]**
> (binding) or **[convention]** (the house default) per the Three Levels of Authority in
> [`general.md`](./general.md).

## Use the native driver API, not LINQ

- **[convention] Query MongoDB through the driver's native API, not LINQ.** Build filters, sorts,
  projections and updates with `Builders<T>` (`Filter`, `Sort`, `Projection`, `Update`), read with
  `Find` or an `Aggregate` pipeline, and use `FilterDefinition<T>` / `PipelineDefinition<,>` for
  anything composed. Do not call `collection.AsQueryable()`.
- **[convention] Why.** The native API exposes everything the server can do (`$lookup`, `$facet`,
  `$group`, `$unionWith`, window and text operators), and it leaves no expression-tree translation
  step between your code and the server. LINQ is translated by the driver at execution time: an
  expression it cannot translate compiles fine and fails at runtime with `Expression not supported`.
  Concept-typed members, casts, and operators the translator does not know are where that shows up.
- **[convention] A member selector inside a `Builders<T>` call is acceptable** —
  `Builders<T>.Filter.Eq(item => item.Status, status)` names a field, it is not a LINQ pipeline.
  Anything with `Select`, `GroupBy`, `Join`, `Cast`, or a method call on a member goes to an
  aggregation pipeline or a filter definition instead. To avoid translation altogether, address the
  field by its BSON element name: `new StringFieldDefinition<T, TValue>("_id")` is what Arc's own
  `FindById` uses.
- **[convention] Filter in the database, not after it.** Put the predicate in the `Find` filter or a
  `$match` stage. Never `ToList()` and then filter, sort or slice in memory.

```csharp
// One document
await collection.Find(Builders<Project>.Filter.Eq(project => project.Id, id)).FirstOrDefaultAsync();

// A filtered list, composed from definitions
var filter = Builders<Project>.Filter.And(
    Builders<Project>.Filter.Eq(project => project.IsArchived, false),
    Builders<Project>.Filter.Eq(project => project.Owner, owner));
await collection.Find(filter).ToListAsync();

// Anything beyond filter/sort/project is an aggregation pipeline; field names are BSON element names
var perOwner = await collection.Aggregate()
    .Match(filter)
    .Group(new BsonDocument { { "_id", "$owner" }, { "count", new BsonDocument("$sum", 1) } })
    .ToListAsync();
```

## Paging and sorting belong to Arc

- **[convention] A query method does not page or sort for the client.** Never write `Skip`, `Take`,
  `Limit` or `OrderBy` to implement `page`, `pageSize` or `sortby`; Arc reads those from the request
  and applies them for the shapes it supports. For which MongoDB shapes that is, how the sort field is
  resolved, and what a query author must not do, use the **cratis-arc-query-paging** skill.
- **[convention] A list that can grow is returned through a shape Arc pages.** For MongoDB that is
  `ISubject<IEnumerable<T>>` from the `Observe` helpers, which take a `FilterDefinition<T>`. A
  `Find(...).ToListAsync()` result is not paged, so it is for bounded sets only.

## Related

- Read-model queries and the `Observe` family: the **cratis-chronicle-read-model** skill.
- Paging and sorting: the **cratis-arc-query-paging** skill.
- Query cost and scalability review: the **cratis-performance-review** skill.
