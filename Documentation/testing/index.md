# Testing

Chronicle ships a dedicated integration testing library — `Cratis.Chronicle.XUnit.Integration` — that gives
you realistic, end-to-end test coverage of your Chronicle-based application without mocking the event store.

## Why a Dedicated Test Library

**State lives in the database.** Projections and reducers build read models from the event stream, and those
read models are stored in MongoDB. To assert on them, the test needs a live Chronicle and a live database, not
a bag of mocked objects.

The `Cratis.Chronicle.XUnit.Integration` package solves this: it spins up a full in-process or out-of-process
Chronicle and exposes a simple event collection API for asserting on what happened.

## What It Provides

| Feature | Description |
|---|---|
| Fixture base classes | `ChronicleInProcessFixture`, `ChronicleOrleansFixture` — handle startup, teardown, and database provisioning |
| `IChronicleSetupFixture` | Interface available on all fixture types; exposes `EventStore`, `Services`, `ReadModelsDatabase`, and `Collect()` |
| `IEventAppendCollection` | Scoped collector returned by `StartCollectingAppends()` — captures every event appended while it is active |
| `CollectedEvent` | Snapshot of a single append: event object, sequence number, constraint violations, concurrency violations, and errors |

## How Tests Are Structured

The library is designed around xUnit `[Collection]` groups. Tests within a collection execute sequentially,
which means each test gets a clean scope from `Collect()` with no interference from previous tests.

The typical pattern is:

1. Call `StartCollectingAppends()` on the fixture to open a new collection scope.
2. Append the event that triggers the operation under test.
3. Assert on `_scope.All` or `_scope.Last` immediately — no waiting needed.
4. Dispose the scope in `Destroy()`.

Because `EventStore.EventLog.Append(...)` is awaited, the append (and any synchronous side-effects visible
in the collection) is complete by the time the `await` returns.

## Topics

- [Event Append Collection](event-append-collection.md)
