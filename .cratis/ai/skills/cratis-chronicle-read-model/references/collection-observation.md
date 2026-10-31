<!-- cratis-ai-managed: skills/cratis-chronicle-read-model/references/collection-observation.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Observe a Chronicle collection

Requires Chronicle 19.54.0 for `IReadModels.Observe<T>()`. Check the client package
version; on older clients use a supported provider-specific observation and
verify its key and protected-value access path rather than assuming equivalence.

This query fragment belongs on an Arc `[ReadModel]` record `ProjectSummary`:

```csharp
public static System.Reactive.Subjects.ISubject<IEnumerable<ProjectSummary>> All(
    Cratis.Chronicle.ReadModels.IReadModels readModels) =>
    readModels.Observe<ProjectSummary>();
```

The model must have an active projection or reducer. The initial emission is the
complete stored collection (including empty); later emissions replace it.
Registered container names and key properties are honored without a property
named `Id`, on MongoDB, SQL and in-memory storage. PII and encrypted values are
released before delivery. The namespace is captured when `Observe` is called;
keep the observation in its tenant scope.

`Observe<T>(Expression<Func<T, bool>>? filter = null)` filters **client-side after
release**. It does not reduce server reads or transport and adds no server-side
sorting/paging. There is no default 50-row window: for large lists choose
Chronicle's materialized paginated observation, not a post-snapshot page.
Use `Watch<T>()` for individual changesets, removal metadata and event context.

Each call owns a separate observation. The first subscriber starts its stream;
additional subscribers share it and receive the latest snapshot. Last-subscription
disposal, subject disposal or `OnCompleted` stops it permanently. Call `Observe`
again to restart. Stream, filter and deserialization errors reach `OnError`;
there is no automatic resubscription. SQL polling can coalesce several writes.

Passive and event-target models throw `ReadModelCannotBeObserved`; an unknown
model throws `UnknownReadModel`. Custom `IReadModels` implementations must
implement or forward `Observe`; its default implementation throws rather than
pretending observation succeeded.

## Verify

Run `dotnet build -c Debug` and `dotnet test` for the owning query specs. Cover
empty, add, update, remove, filter exit and disposal.
Requires Arc 22.87.0 for Chronicle observations in `QueryScenario`.
Requires Arc 22.91.1 with Chronicle 19.59.1 for default-mode children/joins/reducers. Scenario rebuilds do
not prove database notifications or server streaming: exercise those in the
provider's integration test.
