# [v9.13.0] - 2023-12-3 [PR: #0]()

### Added

- Added support for base types with derivative event types for projections. Projections can just use the `From()` syntax with a base type, nothing changes. Behind the covers there is a new `FromAny` in the projection definition. Internal engine does not really care about the inheritance chain, as it just observes all concrete derivatives - just as we do with observers, reducers and aggregate roots. This new feature applies to all types of projections, including immediate projections which will then also be supported by Rules and AggregateRoot states. (#1030)

# [v9.12.0] - 2023-12-3 [PR: #1029](https://github.com/aksio-insurtech/Cratis/pull/1029)

### Added

- Adding support for using base event types or interfaces in observers, reducers and aggregates. The system will identify event types from derivatives and register the event types of these for handling. (#1025)


# [v9.11.0] - 2023-11-29 [PR: #1022](https://github.com/aksio-insurtech/Cratis/pull/1022)

### Fixed

- Equality check for projection definitions is now ignoring the `LastUpdated` property, which caused it to always be changed.
- Fixing reducer pipeline to look at the flag `Replay` rather than comparing the entire value `Replay`. This caused the first event not to be considered part of replay and ended up in the sink being saved to the regular collection rather than the temporary replay collection.

# [v9.10.0] - 2023-11-24 [PR: #1020](https://github.com/aksio-insurtech/Cratis/pull/1020)

### Fixed

- Converter from JSON to ExpandoObject and back now honors null values. If a property if missing from the JSON or the ExpandoObject but is in the schema, we set it to default value for primitives and null for non primitives.


# [v9.9.1] - 2023-11-16 [PR: #1016](https://github.com/aksio-insurtech/Cratis/pull/1016)

### Fixed

- Added logging to `GetModelInstance()` for immediate projections.
- Added last updated to projection definitions.
- Immediate projection now uses the `LastUpdated` property of a projection to decide if ti should rewind the sequence number if it is newer than what the "cached" state is based on.


# [v9.9.0] - 2023-11-15 [PR: #0]()

### Added

- Adding support for `AggregateRoot`. Aggregates can either be stateless, meaning you can implement handle methods for the events you want it to possibly create state from when rehydrating an aggregate, or they can be stateful in the sense that state can be produced during rehydration based on a `IReducerFor<>` or an `IImmediateProjectionFor<>`. These 3 options are mutually exclusive. Documentation of all this will follow. There are also an early approach to writing tests for this, which will be improved upon in the future.


# [v9.8.0] - 2023-11-13 [PR: #1013](https://github.com/aksio-insurtech/Cratis/pull/1013)

### Fixed

- Fixing BSON to `ExpandoObject` converter to support collections of primitives such as `int` and `string`. It made the items `null` before.


# [v9.8.0-beta.1] - 2023-11-2 [PR: #0]()

No release notes

# [v9.7.2] - 2023-11-1 [PR: #1006](https://github.com/aksio-insurtech/Cratis/pull/1006)

### Fixed

- Children with composite keys are now recognized when creating changesets within projections. Two items with same key will now not result in a `ChildAdded` change. This accidently worked when running it with MongoDB but got highlighted as a problem when running unit tests for projections.


# [v9.7.1] - 2023-10-31 [PR: #0]()

No release notes

# [v9.7.0] - 2023-10-30 [PR: #1000](https://github.com/aksio-insurtech/Cratis/pull/1000)

### Fixed

- Fixing `InMemorySink` to set the `Id` property to the key value. This fixes a problem when using immediate projections and the read model coming back does not have a `null` for the `Id` property.


# [v9.7.0-beta.1] - 2023-10-28 [PR: #0]()

No release notes

# [v9.6.0] - 2023-10-26 [PR: #999](https://github.com/aksio-insurtech/Cratis/pull/999)

### Added

- Adding a way to simply add a child based on the content of a property on an event for projections. This is super useful when wanting to have a collection of primitive types based on a property on the event. Important to remember, the type of the event property and the element type has to match - this is handled at compile time.

```csharp
public record AccountTransactions(string Account, IEnumerable<DateTimeOffset> TransactionDates);

public class LatestTransactions : IProjectionFor<AccountTransactions>
{
    public ProjectionId Identifier => "d661904f-15e0-4a96-a0cc-c7389635e4cd";

    public void Define(IProjectionBuilderFor<DebitAccountLatestTransactions> builder) => builder
         .From<DepositPerformed>(_ => _
             .Set(m => m.Account).To(e => e.Account)
             .AddChild(m => m.TransactionDates, e => e.TransactionDate);
}
```



# [v9.5.13] - 2023-10-25 [PR: #996](https://github.com/aksio-insurtech/Cratis/pull/996)

### Added

- Trace logging to `MongoDBIdentityStore`. One should be aware that there is a compliance aspect of these logs, as the user name is present in the log. By default this log will not show up, as it is `Trace` level, but if one configures Cratis with a `Verbose` level, these logs will show up.

### Fixed

- Fixing fallback resolution to username when subject is missing. This practically never kicked in.


# [v9.5.12] - 2023-10-23 [PR: #995](https://github.com/aksio-insurtech/Cratis/pull/995)

### Fixed

- Observers ended up being in a stale state due to not getting the actual next sequence number based on the event types. This is now fixed.
- Optimizing query for getting next event sequence number greater or equal to a given sequence number by using `.In()` rather than a collection of `.Or()` statements for the event types.

# [v9.5.11] - 2023-10-23 [PR: #0]()

### Fixed

- Fixing `CatchUp` and `Replay` to not write state on the `Stop()` method, as it is not altering any `ObserverState` at that point. This fixes a problem we've seen were observers seem to be observing events multiple times. The reason for this is that the in-memory state representation of the grain is stale. Observer worker jobs share the state with the parent supervisor, but it is not synchronized and only updated when absolutely needed. This whole thing will be ripped out in an upcoming version with a complete rewrite of how observers work.ease notes

# [v9.5.10] - 2023-10-22 [PR: #993](https://github.com/aksio-insurtech/Cratis/pull/993)

### Fixed

- Moving underlying database communication from `ObserverSupervisor` to the state provider for increased stability and avoiding `Task`dead lock scenarios.
- Fixed so that the running state of an observer is reflected correctly, it could in some cases become active but showed "Subscribing" - as "Active" and subscribing to the underlying stream is the default, but was not reflected in the `RunningState` property.


# [v9.5.9] - 2023-10-19 [PR: #992](https://github.com/aksio-insurtech/Cratis/pull/992)

### Changed

- Changing number of events priming the cache with. Going from 1000 to 100. This makes much more sense based on what the cache is for (catching up). The cache is most likely going away or will change to something else in the future anyways.

### Fixed

- Log Levels have been modified for a lot of log statements from Information to Debug.
- Make sure all calls in the `MongoDBEventSequenceStorageProvider` has `.ConfigureAwait(false)` to not return to the same task context.
- Saving round trips to MongoDB for `ProjectionDefinitions` by changing from `HasFor/GetFor` to a `TryGetFor` pattern.
- Reorder startup sequence, making sure event schemas are populated first.
- Changing observers to ask the event sequence grain instead of the database - which is much more consistent and correct. Saves quite a lot of roundtrips to the database.
- Fixing a problem in the `EventSequenceCache` that caused a dead lock during startup and grain calls timing out while subscribing to the persistent stream.
- Adding more state to an event sequence. Holding a dictionary of tail sequence numbers per event type. This will be populated automatically based on an aggregation from the event sequence if this state is missing. This speeds up lookups from observers and others needing this information. It also saves on round trips to the database.
- Automatically rehydrating all event sequences at startup, making them ready with state from the start.
- "Fixing" our "magic" microservices and tenants that are added (Unspecified, NotSet, Development, Kernel) so that they have configuration data. This whole thing will be completely changed in [version 10](https://github.com/aksio-insurtech/Cratis/issues?q=is%3Aopen+is%3Aissue+milestone%3A10.0.0)
- Adding `.SortBy()` to event sequence calls for getting events, to guarantee they come in order.
- Adding concurrency locks for `RecoveringFailedPartition` supervisor on the internal collection of failed partitions.
- Adding an explicit exception if reducer content returned is invalid.
- Improved performance during registration of observers, rather than waiting invidiual `.Start()` methods on the `ClientObserver` grain, we queue them and do `Task.WhenAll()`. This is the recommended approach from the Orleans team.

### Removed

- Removing persistence of connected clients, this was a complete abuse to be able to get a nice reactive view in the workbench. This is now instead asking the `ConnectedClients` grain on a schedule and still being reactive. This saves a lot of traffic to the database.


# [v9.5.8] - 2023-10-10 [PR: #987](https://github.com/aksio-insurtech/Cratis/pull/987)

### Fixed

- Fixing server crash at startup by upgrading the dependency to `Fundamentals` package, which has a fix for what is classified as assembly referenced packages - avoiding duplicates in type discovery.


# [v9.5.7] - 2023-10-10 [PR: #986](https://github.com/aksio-insurtech/Cratis/pull/986)

### Fixed

- Adding package referenced assemblies to default artifacts discovery so that we get types from the Cratis SDK, e.g. `EventRedacted` event type.
- Register system event types, e.g. `EventRedacted` in all event stores.


# [v9.5.6] - 2023-10-9 [PR: #0]()

No release notes

# [v9.5.5] - 2023-10-9 [PR: #0]()

No release notes

# [v9.5.4] - 2023-10-9 [PR: #985](https://github.com/aksio-insurtech/Cratis/pull/985)

### Fixed

- Removing a `console.log()` from the workbench when looking at event details.
- Fixing rehydration of projections to not activate projections that are marked as non active (`IsActive=false`)


# [v9.5.3] - 2023-10-6 [PR: #983](https://github.com/aksio-insurtech/Cratis/pull/983)

### Fixed

- Adding more logging to see what is going on @ startup.
- Making sure we register projections that aren't registered with the `ProjectionManager`.


# [v9.5.2] - 2023-10-6 [PR: #981](https://github.com/aksio-insurtech/Cratis/pull/981)

### Fixed

- Projection registration internally was registering per tenant, while it should be per microservice.
- Added more logging for projection registration.


# [v9.5.1] - 2023-10-3 [PR: #975](https://github.com/aksio-insurtech/Cratis/pull/975)

### Fixed

- Skipping `_id` properties in changeset when creating update definition for Mongo, as this is implicitly set by the MongoDB C# driver and will cause an exception if its there.
- FIxing conversion check ordering to make sure we get correct types (complex types first, then value types, dictionaries, enumerables).
- Adding support for dictionary types for the entire Reducer & Projection pipelines, including the MongoDB Sink.


# [v9.5.0] - 2023-9-29 [PR: #974](https://github.com/aksio-insurtech/Cratis/pull/974)

### Added

- Exposing functionality for getting available reducers and also getting a reducer by its Clr type and get a type by the reducer Id.


# [v9.4.8] - 2023-9-26 [PR: #970](https://github.com/aksio-insurtech/Cratis/pull/970)

### Fixed

- Fixing so that primitives are directly by the sink before it considers whether or not it is an enumerable. We saw it outputting strings into MongoDB collections as an array of characters.
- Making sure the single tenancy scenario is dealt with properly throughout config and startup. This will be improved even further in a future version of Cratis.


# [v9.4.7] - 2023-9-25 [PR: #0]()

### Fixed

- Setting the default storage config for unspecified microservice and tenant scenarios.

# [v9.4.6] - 2023-9-25 [PR: #0]()

### Fixed

- Adding unspecified microservice config for when microservice is unspecified.


# [v9.4.5] - 2023-9-25 [PR: #0]()

### Fixed

- Adding unspecified tenant config for single tenant scenarios.

# [v9.4.4] - 2023-9-25 [PR: #0]()

### Fixed

- Fixing so that the `PIIMetadataProvider` get registered correctly with the IoC by the `ClientBuilder`.

# [v9.4.2] - 2023-9-18 [PR: #964](https://github.com/aksio-insurtech/Cratis/pull/964)

### Fixed

- The resolution of identity for things like caused by looks for a match on subject first and if no subject is registered with that it will try to resolve using the username claim. If already have multiple registred users with subject set to null or empty string, then this would fail on startup. This version fixes this problem.


# [v9.4.0] - 2023-9-5 [PR: #957](https://github.com/aksio-insurtech/Cratis/pull/957)

### Added

- Introducing benchmarks for the most common operations; appending and observing events. This is the baseline and we'll be expanding on it.
- Started the foundation of custom defined keys and indexing for observers.
- New type of observer called **Reducer**. Its a cross between an imperative client observer and a declarative projection. The result of a reduction is dealt with by the kernel and leveraging the same sink mechanism as declarative projections. An example below:

```csharp
[Reducer("ff449077-0adb-4c5c-90e6-15631cd9e2b1")]
public class CartReducer : IReducerFor<Cart>
{
    public Task<Cart> ItemAdded(ItemAddedToCart @event, Cart? initial, EventContext context)
    {
        initial ??= new Cart(context.EventSourceId, Array.Empty<CartItem>());
        return Task.FromResult(initial with
        {
            Items = initial.Items?.Append(new CartItem(@event.MaterialId, @event.Quantity)) ??
                new[] { new CartItem(@event.MaterialId, @event.Quantity) }
        });
    }
}
```

### Fixed

- Reorganizing internally for clarity, improved structure.
- Renaming `ProjectionSink` to `Sink` internally, giving it a wider usecase.
- Fixing WebSockets setup in Kernel for the Workbench by configuring it in the right order. ASP.NET Core is sensitive to ordering of its middlewares.




# [v9.3.7] - 2023-9-1 [PR: #956](https://github.com/aksio-insurtech/Cratis/pull/956)

### Fixed

- Replay now calls the replay completed method rather than the catchup completed method when it is done.
- Making sure the subscription information is kept in memory when reading state, so that it is not lost.
- Ignoring replay if there are no events to replay.



# [v9.3.6] - 2023-9-1 [PR: #0]()

No release notes

# [v9.3.5] - 2023-8-31 [PR: #0]()

No release notes

# [v9.3.3] - 2023-8-31 [PR: #0]()

### Fixed

- Fixing NuGet package setup for logo, it was missing the logo file as content.

# [v9.3.2] - 2023-8-31 [PR: #0]()

### Fixed

- Client Observer registrations seems to fail under some circumstances and not necessarily for all. This could be linked to an attempt of an optimization of doing a `Task.Run()` for calling the `ClientObservers` grain for registering and returning to the client as soon as possible. This seems to fail sometimes. Taking it out and it seems to be consistent.

# [v9.3.1] - 2023-8-28 [PR: #938](https://github.com/aksio-insurtech/Cratis/pull/938)

### Fixed

- Making `Causation` and `CausedBy` on the server side optional and automatically injected if not there based on context in the server. This is for backwards compability with any 9.x clients


# [v9.3.0] - 2023-8-27 [PR: #937](https://github.com/aksio-insurtech/Cratis/pull/937)

### Changed

- Internal change: Moving redactions from happening on the command handler to be a reaction to events.

### Added

- Introducing a system event sequence.
- Added a way to see system events in the workbench.
- Adding a way to get a specific event sequence in the client.
- Adding a boolean to tell if the client is multi-tenanted or not.
- Adding the ability to specify specific event sequence to observe.

### Fixed

- Fixing identity store to store in the correct database; the cluster database.
- Consistency of Kernel as its own Microservice. It was accidently using different identifiers, which caused confusion in the code.
- FIxing order of initialization on the Kernel, letting the HTTP server be ready before initiating boot procedures for Kernel


# [v9.2.1] - 2023-8-25 [PR: #936](https://github.com/aksio-insurtech/Cratis/pull/936)

### Fixed

- Dead lock situation when starting kernel with observers needing to replay. Solved this by explicitly priming the event sequence caches at startup, rather than lazily through the streaming infrastructure.
- Moving the decision of what time an event operation occurred to the owning systems (e.g. EventSequence grain), rather than letting the persistence layer do this.
- Fixing underlying problem with observer state with regards to current subscriptions, causing replays to only work once and sometimes never.
- Making redaction work in the workbench again by making causation and caused optional and setting these on server side if not set. The endpoints got a 409 with validation error messages before this change.

# [v9.2.1] - 2023-8-25 [PR: #935](https://github.com/aksio-insurtech/Cratis/pull/935)

### Fixed

- Dead lock situation when starting kernel with observers needing to replay. Solved this by explicitly priming the event sequence caches at startup, rather than lazily through the streaming infrastructure.
- Moving the decision of what time an event operation occurred to the owning systems (e.g. EventSequence grain), rather than letting the persistence layer do this.
- Fixing underlying problem with observer state with regards to current subscriptions, causing replays to only work once and sometimes never.
- Making redaction work in the workbench again by making causation and caused optional and setting these on server side if not set. The endpoints got a 409 with validation error messages before this change.

# [v9.2.0] - 2023-8-23 [PR: #933](https://github.com/aksio-insurtech/Cratis/pull/933)

### Added

- Adding capturing of what causes an event. (#277)
- Adding cause type for ASP.NET requests and automatically capturing information around this.
- Adding cause type for observers.
- Adding cause type for integration adapters (Import Operation).
- Adding a specific root cause type that captures software version, process, commit and details. (#858)
- Introducing a client API for configuring software version with commit details. (#858)
- Adding capturing of caused by with the introduction of an identity store (#278).
- Adding ASP.NET identity provider for capturing caused by, leveraging the HttpContext and the current user.


# [v9.1.0] - 2023-8-16 [PR: #912](https://github.com/aksio-insurtech/Cratis/pull/912)

### Added

- Adding support for appending many events at a time.
- Centralized all projections and registering all; integration adapters, immediate projections and rule based.
- Added persistence for all types of projections.
- Introducing the concept of active vs non active projections. Active projections will automatically observe an event sequence, while an inactive (passive) will not. Immediate projections, rules or adapters fall into the category of passive projections.
- Making it possible to use any projections for immediate projection by specifying the projection identifier you want to use.

### Fixed

- Added missing logging for when an observer invocation fails on the client.
- Adding service registration for `ITenantConfiguration` to the client builder.
- Fixing so that `ValidFrom` information is included on the Kernel receiver side when appending.
- Optimizing performance for `Importer` by using the new `AppendMany` API.
- A bug in the Kernel causing failed partition information not to be written to the event store immediately.
- Projection sinks are now only created once per projection and sink type.
- The engine representation of a Projection and its Pipeline is now managed by a manager and created only once per node.
- Changed internal implementation of how we keep projections and pipeline definitions in sync across multiple silos when changed. Leveraging Broadcast channels.



# [v9.0.2] - 2023-7-25 [PR: #908](https://github.com/aksio-insurtech/Cratis/pull/908)

### Fixed

- The `ClientOptions` type was in the wrong namespace, fixed to be in `Aksio.Cratis.Configuration` as expected.

# [v9.0.1] - 2023-7-25 [PR: #0]()

- Fixing `ClientBuilder` to use the `SingleKernelOptions` as a fallback only if none of the other options are set.
- Removing current subscription information from the Observer state being stored, as this has only an in-memory value.

# [v9.0.0] - 2023-7-19

## Summary

This is a new major release of Cratis. Primarily this release is about structure and making Cratis much more focused.
After version 9 the Cratis repository only contains the Cratis Kernel, the Workbench and clients for working with Cratis.

For this to be possible, we had to split the repository into 4 parts. That resulted in taking out a lot of non Cratis related
code and put it into the following repositories:

https://github.com/aksio-insurtech/Fundamentals
https://github.com/aksio-insurtech/ApplicationModel
https://github.com/aksio-insurtech/MongoDB

From a Cratis usage perspective only the startup and configuration has changed.
Usage is just as it was before. The REST API surface of the Kernel is also the same, meaning that
pre 9 clients can connect to version 9 of the kernel and also clients with version 9 can connect to a version 8.
Keep in mind though that new features are planned for 9 that will break this compatibility in a future version.

### Added

- Possibility to configure Cratis client from `appsettings.json` or by programatically configure the `IOptions<ClientOptions>`.
- Upgraded Kernel to .NET 7
- Upgraded Microsoft Orleans to version 7
- It is now possible to use a minimal API approach for setting up the client. Look at the **Basic** sample for more details and also getting started guides.

### Changed

- `.UseCratis()` for the `HostBuilder` or `IServiceCollection` is now relying on a fluent interface for configuring the different parts to it, so you no longer pass it anything but the optional delegate for configuring the client.
- If you're using a `Startup` class for your ASP.NET Core setup, you now need to call `.UseCratis()` extension method on the `IApplicationBuilder`. This will connect the client.
- MongoDB is now optional from a client perspective, a new package called `Aksio.Cratis.MongoDB` can be added. To use its functionality of automatically hooking up multi-tenanted `IMongoCollection<>` bindings you simply add a `.AddMongoDBReadModels()` when you configure your `IServiceCollection`.

# [v8.15.0] - 2023-4-25 [PR: #857](https://github.com/aksio-insurtech/Cratis/pull/857)

### Added

- Added support for optionally providing a default value for `useIdentity()` hook. Typically useful when a user is not logged in.


# [v8.14.1] - 2023-4-20 [PR: #851](https://github.com/aksio-insurtech/Cratis/pull/851)

## Summary

Adds IComparable to ConceptAs<>.

> **Important note**: This release is marked as a patch but the release notes indicate a change. The implementation of `IComparable` for a `ConceptAs<>` is viewed by us as an oversight. Concepts should be encapsulating primitives and primitives are comparable. However, if you have been using it for other things than primitives you should consider changing these, they might just need to regular records. If you sill feel the type should be a concept, you will have to implement `IComparable`.

### Changed

- Adds IComparable to the ConceptAs so that we can use concepts in features such as Ordering.  A concept uses the default ordering of the underlying type.  It is up to the consumer to determine if this has semantic meaning (e.g. comparison of GUIDs).


# [v8.14.0] - 2023-4-20 [PR: #841](https://github.com/aksio-insurtech/Cratis/pull/841)

### Added

- MicroserviceName introduced, optionally for now. This will in the future be required as we move configuration out of JSON into automatically configure.
- Metrics for event sequences and connected clients supporting both ApplicationInsight and OpenTelemetry directly.
- A way of influencing the model names with a `IModelNameConvention` for the automatic hookup of `IMongoCollection<>` and Projections. The convention is configured once and applies to all scenarios where a model name is used.

### Fixed

- Removing custom scavenger job for cleaning up silos and instead configuring Orleans defunct silo expiration cleanup.
- Observers now persist the subscription information letting it continue if restarted. This is especially important for projections, as they don't have an active client reconnecting.



# [v8.13.5] - 2023-4-14 [PR: #840](https://github.com/aksio-insurtech/Cratis/pull/840)

### Fixed

- Fixing so that the client can call the Kernel API out of execution context. We're for the most part very specific on providing the necessary information on API routes.


# [v8.13.4] - 2023-4-14 [PR: #839](https://github.com/aksio-insurtech/Cratis/pull/839)

### Fixed

- Adding more logging for observers for easier diagnostics.


# [v8.13.3] - 2023-4-13 [PR: #837](https://github.com/aksio-insurtech/Cratis/pull/837)

### Fixed

- Fixing so that results from the `.aksio/me` identity details endpoint does not escape JSON result during serialization.


# [v8.13.2] - 2023-4-13 [PR: #836](https://github.com/aksio-insurtech/Cratis/pull/836)

### Fixed

- Adding a `OnKernelUnavaialable()` virtual method for `RestKernelClient` implemented by the `OlreansAzureTableStoreKernelClient` which will refresh silo information before a retry.


# [v8.13.1] - 2023-4-13 [PR: #833](https://github.com/aksio-insurtech/Cratis/pull/833)

### Fixed

- Scavenger job that cleans up dead silos when using Azure Table Store. Fixing #624.


# [v8.13.0] - 2023-4-13 [PR: #831](https://github.com/aksio-insurtech/Cratis/pull/831)

### Added

- Added custom Serilog formatter for outputting a rendered compact JSON format, yet not as compact as the official one. Based on the official implementation found [here](https://github.com/serilog/serilog-formatting-compact/tree/dev/src/Serilog.Formatting.Compact/Formatting/Compact).

Usage:

```json
{
    "Serilog": {
        "Using": [
            "Serilog.Sinks.Console"
        ],
        "WriteTo": [
            {
                "Name": "Console",
                "Args": {
                    "formatter": "Aksio.Cratis.Applications.Logging.RenderedCompactJsonFormatter, Aksio.Cratis.Applications"
                }
            }
        ]
    }
}
```


# [v8.12.8] - 2023-4-13 [PR: #830](https://github.com/aksio-insurtech/Cratis/pull/830)

### Fixed

- Setting Serilog Compact Json formatter dependency to stable version, not a pre-release.


# [v8.12.7] - 2023-4-13 [PR: #829](https://github.com/aksio-insurtech/Cratis/pull/829)

### Fixed

- Fixing a problem where message batches for the event sequence could be queued before the `CreateReceiver` have been called by Orleans, which then causes observers to be faulty at startup.


# [v8.12.6] - 2023-4-12 [PR: #828](https://github.com/aksio-insurtech/Cratis/pull/828)

### Fixed

- Adding more logging to replaying


# [v8.12.5] - 2023-4-12 [PR: #827](https://github.com/aksio-insurtech/Cratis/pull/827)

### Fixed

- Added the compact JSON formatter as it was missing as a dependency.


# [v8.12.4] - 2023-4-12 [PR: #826](https://github.com/aksio-insurtech/Cratis/pull/826)

### Fixed

- Replay worker fixed, observation state was never set to `TailOfReplay` unless the `LastHandled` event sequence number was the same as the tail. This is now fixed. The problem it caused was that projections never reached the end of replay and the temporary collections never renamed.
- Fixed the replay state across projections and projection sink, so that it is not a global "isReplaying" that can change and especially since lifecycle of sinks could be transient. This results now in correct projection replays to the correct collections.
- Fixed the `RunAsSinglePageApplication()` - catch all middleware to return a 404 if there are no files matching.


# [v8.12.3] - 2023-4-11 [PR: #825](https://github.com/aksio-insurtech/Cratis/pull/825)

### Fixed

- Fixed the log message when fast forwarding, it was using the wrong sequence numbers making it hard to see why it fast forwarded.


# [v8.12.2] - 2023-4-5 [PR: #824](https://github.com/aksio-insurtech/Cratis/pull/824)

### Fixed

- Fixing so that the next event sequence number for observers that gets called with `RewindPartitionTo()` gets set to what it was after it is done.



# [v8.12.1] - 2023-4-4 [PR: #823](https://github.com/aksio-insurtech/Cratis/pull/823)

### Fixed

- Fixed the route for disconnect on both server and client.


# [v8.12.0] - 2023-4-4 [PR: #822](https://github.com/aksio-insurtech/Cratis/pull/822)

### Added

- Support for disconnecting explicltly a client.

### Fixed

- When doing redact it uses the `RewindPartitionTo()` method on observers, this was faulty and did a full replay of the observer - which it shouldn't.


# [v8.11.1] - 2023-4-3 [PR: #819](https://github.com/aksio-insurtech/Cratis/pull/819)

### Fixed

- During learning more about ESModules we accidently committed test config, this broke the MUI package. This is now fixed.


# [v8.11.0] - 2023-4-1 [PR: #818](https://github.com/aksio-insurtech/Cratis/pull/818)

### Added

- Workbench now has server side paging for event sequences.


# [v8.10.6] - 2023-3-30 [PR: #816](https://github.com/aksio-insurtech/Cratis/pull/816)

### Fixed

- FIxing so that observers gets called properly after a redaction. There was an error in the query for getting observers based on event types.


# [v8.10.5] - 2023-3-30 [PR: #813](https://github.com/aksio-insurtech/Cratis/pull/813)

### Fixed

- `EventType` now supports parsing string representation without generation info, defaulting to `EventGeneration.First` for that scenario.
- When checking if redaction has already happened, we now check only the `Id` part - we don't care about which generation at that point.


# [v8.10.4] - 2023-3-30 [PR: #812](https://github.com/aksio-insurtech/Cratis/pull/812)

### Fixed

- The ´useIdentity()` hook is now fixed to not go into an endless re-render loop when the identity cookie is available.


# [v8.10.3] - 2023-3-30 [PR: #811](https://github.com/aksio-insurtech/Cratis/pull/811)

### Fixed

- Added missing API endpoint for getting tail sequence number for a specific observer.


# [v8.10.2] - 2023-3-27 [PR: #810](https://github.com/aksio-insurtech/Cratis/pull/810)

### Fixed

- Cache was getting hammered with cache misses. Changnig behavior to be a window into the tail of the sequence.
- Observers will do a fast forward if their `NextSequenceNumber` is behind and there is no event of any of the types it is subscribing to between its `Next`and the tail of the event sequence.
- Fixing navigation in workbench.
- Removing unused obsolete API for getting an instance from projections. This is now in `ImmediateProjection`.


# [v8.10.1] - 2023-3-23 [PR: #806](https://github.com/aksio-insurtech/Cratis/pull/806)

### Fixed

- FIxing overflow of the `DataGrid` in the event types and projections views.


# [v8.10.0] - 2023-3-23 [PR: #803](https://github.com/aksio-insurtech/Cratis/pull/803)

### Added

- Workbench support for redaction of events.

### Changed

- Non-breaking change: Changed from FluentUI to MUI and got a whole new look and feel for the Workbench.


# [v8.9.5] - 2023-3-23 [PR: #804](https://github.com/aksio-insurtech/Cratis/pull/804)

### Fixed

- Moving the instantiation of the identity provider to a per request level.


# [v8.9.4] - 2023-3-21 [PR: #801](https://github.com/aksio-insurtech/Cratis/pull/801)

### Fixed

- Skip redacting if an event is already redacted. (#796)


# [v8.9.3] - 2023-3-20 [PR: #799](https://github.com/aksio-insurtech/Cratis/pull/799)

### Fixed

- Added missing API for getting an observer CLR type by its observer id.


# [v8.9.2] - 2023-3-20 [PR: #798](https://github.com/aksio-insurtech/Cratis/pull/798)

### Fixed

- Bringing back a way to get all registered handlers.


# [v8.9.1] - 2023-3-20 [PR: #797](https://github.com/aksio-insurtech/Cratis/pull/797)

### Fixed

- Null `ConceptAs<>` values are now serialized to MongoDB Null - not the `default` value as before, which is completely wrong.


# [v8.9.0] - 2023-3-20 [PR: #794](https://github.com/aksio-insurtech/Cratis/pull/794)

### Added

- Support for getting the tail sequence number based on an observer and the event types it observers. This will return the tail of the event type that is last in the sequence.


# [v8.8.4] - 2023-3-19 [PR: #793](https://github.com/aksio-insurtech/Cratis/pull/793)

### Fixed

- Fixed the frontend `IdentityContextProvider` to explicitly call the `/.aksio/me` endpoint if no cookie is there. This is to improve the devex. It does however require the `x-ms-client-*` headers to be set for the endpoint to work.


# [v8.8.3] - 2023-3-16 [PR: #791](https://github.com/aksio-insurtech/Cratis/pull/791)

### Fixed

- Fixing proxy generator to output correct type for enumerable element when it is a known type. E.g. `ConceptAs<>` types.
- Fixing the redaction commands to not be overloads, as that produces same name on the proxies.
- Fixing tenant configuration to work - its been broken since we changed client type.


# [v8.8.2] - 2023-3-16 [PR: #790](https://github.com/aksio-insurtech/Cratis/pull/790)

### Fixed

- Fixing tenant configuration to work - its been broken since we changed client type.


# [v8.8.1] - 2023-3-16 [PR: #787](https://github.com/aksio-insurtech/Cratis/pull/787)

### Fixed

- If no logger factory is specified during configuration of Cratis client, it will create a default one.


# [v8.8.0] - 2023-3-15 [PR: #781](https://github.com/aksio-insurtech/Cratis/pull/781)

### Added

- Introducing REST API in Kernel and client API on top of it for working with observers.
- Introducing REST API in Kernel and client API on top of it  for getting head and next event sequence number with event sequence



# [v8.7.0] - 2023-3-15 [PR: #780](https://github.com/aksio-insurtech/Cratis/pull/780)

### Added

- Functional support for redaction that rewinds partitions automatically. This will be iterated on the next couple of days. (#778)


# [v8.6.3] - 2023-3-8 [PR: #775](https://github.com/aksio-insurtech/Cratis/pull/775)

### Fixed

- Nullable enum values are now supported. They're represented using the `OneOf` in the JSON schema and not just a `JsonObjectType` of `Number` and the flag `Null` set, deviates from any other types.


# [v8.6.2] - 2023-3-5 [PR: #773](https://github.com/aksio-insurtech/Cratis/pull/773)

### Fixed

- Null value handling in the `ObjectsComparer` for elements in collections.


# [v8.6.1] - 2023-3-2 [PR: #772](https://github.com/aksio-insurtech/Cratis/pull/772)

### Fixed

- Keeping observers alive forever by telling Orleans to never deactivate them (only the logical unpartitioned observer). (#770)
- Fixing how failed partitions state is rehydrated for observer state to be rehydrated from the actual collection of failed partitions.
- Delaying recovery of partitions through a timer that starts immediately. This fixes so that we don't end up in dead-lock scenarios due to recovery grain calling the supervisor during start up.


# [v8.6.0] - 2023-2-24 [PR: #769](https://github.com/aksio-insurtech/Cratis/pull/769)

### Added

- Adding support for composite keys for parent key definition for child definitions in projections.

### Fixed

- Adding logging of errors for a client lifecycle participant failure. (#768)
- Removing residues of recovering partitions on the observer state.
- Fixing observer method convention to not include methods that return a generic `Task<>`. (#767)
- Fixing observer method convention to not include non-public methods. (#767)




# [v8.5.6] - 2023-2-22 [PR: #765](https://github.com/aksio-insurtech/Cratis/pull/765)

### Fixed

- Fixed frontend QueryFor to return correct query result with data set to correct default value when errors occur.


# [v8.5.5] - 2023-2-21 [PR: #764](https://github.com/aksio-insurtech/Cratis/pull/764)

### Fixed

- MongoDB Projection sink now handles setting arrays directly without array indexers.
- Projections now support pulling array of primitives directly from the event and onto the target model.


# [v8.5.4] - 2023-2-21 [PR: #763](https://github.com/aksio-insurtech/Cratis/pull/763)

### Fixed

- Fixing so we don't crash if there are no array indexers when projecting with arrays, set the properties instead rather than try to update specific part of array.
- Fixing conversion to `ExpandoObject` when JSON holds arrays of string or numbers and schema matches. This became null before.


# [v8.5.3] - 2023-2-21 [PR: #761](https://github.com/aksio-insurtech/Cratis/pull/761)

### Fixed

- Fixing an error where converting events from the MongoDB represetantion with arrays and elements being null during conversion to the schema - ignore these values, they are null.


# [v8.5.2] - 2023-2-21 [PR: #759](https://github.com/aksio-insurtech/Cratis/pull/759)

### Fixed

- Projection has changed fixed from doing `.Equals()` on JSON object against a JSON string to both being JSON strings.


# [v8.5.1] - 2023-2-21 [PR: #758](https://github.com/aksio-insurtech/Cratis/pull/758)

### Fixed

- Fixing projections check for if it has changes to return true if it has changes and not the opposite as it was.


# [v8.5.0] - 2023-2-20 [PR: #754](https://github.com/aksio-insurtech/Cratis/pull/754)

### Added

- Workbench view of connected clients.
- Added collection of event types on the `ObserverSubscription` and also `ObserverId` and `ObserverKey`. (#752)
- `OnDisconnected()` method on client - implemented for the Orleans Silo based cluster config to refresh the silo configuration at that point. (#753)
- Added new and improved failed partition worker grains.
- Added view in Workbench to see failed partitions and details on a failed partition.
- Added view of connected clients in Workbench.
- Added new and improved replay worker grain.

### Fixed

- Adding support for running with the debugger attached. It will not consider the Kernel disconnected. (#750)
- Making event sequence number duplication a transient error by retrying with an increment of 1. (#263)
- Rewind now works again in Workbench.

# [v8.4.4] - 2023-2-9 [PR: #747](https://github.com/aksio-insurtech/Cratis/pull/747)

### Fixed

- Fixing so that `LastHandled`get set during catch-up if the `LastHandled` is less than the current sequence number on the event.
- Read the state when subscribing an observer. Since we have worker grains (CatchUp) that will update parts of the state, we need to be sure we have the correct state when subscribing. This could be because a client is disconnected and then reconnected while a catchup is running.
- Improving reliability on state persistence by making sure we do the tasks on unsubscribe / deactivate in the correct order.
- Encapuslating observer subscription and adding a method for getting current subscription on the `IObserverSupervisor`.

# [v8.4.3] - 2023-2-8 [PR: #742](https://github.com/aksio-insurtech/Cratis/pull/742)

### Fixed

- Removing a tension point were the client observer subscriber was asking for all connected clients in the `OnNext` method to know what clients are connected.
- Bug in the internal event sequence cache causing it to skip events under certain circumstances. The cache is simplified and focused on providing a rolling forward view.

# [v8.4.2] - 2023-2-8 [PR: #737](https://github.com/aksio-insurtech/Cratis/pull/737)

### Fixed

- Only allow one catchup for a partition at once. Once a partition is in catchup, it shouldn't really be possible to handle events and end up in a second catchup.
- Separating handling of events for failed partitions from the active or catchup, as they are very different.
- Improved test coverage for catchup and observers.


# [v8.4.1] - 2023-2-7 [PR: #731](https://github.com/aksio-insurtech/Cratis/pull/731)

### Fixed

- Adding `lock` around schema metadata alterations as there could be concurrency issues as it could be modified from different tasks/threads.


# [v8.4.0] - 2023-2-6 [PR: #717](https://github.com/aksio-insurtech/Cratis/pull/717)

### Added

- Added an in-memory cache per silo for event sequences. This improves performance dramatically as we hit the underlying database much less. The cache is per microservice, per tenant, per event sequence.

### Fixed

- Fix count expression to use the actual target type for conversion if it is not an `Int32`
- Fixing correct execution context for projections.
- Improving performance of appending and observing events for events that does not have any actionable compliance rules associated.
- Improving performance and memory growth for internal managing of events going through the system by caching flattened properties on the event schemas.
- Do not hook up log messages for MongoDB commands unless in trace mode. This dramatically improves memory usage and performance.
- Limit amount of `IMongoClient` instances to one per server. Improved memory and performance.
- Separated catch-up operation for observers into its own grain. This improves reliability and also improves significantly the code maintainability and quality. (#722)
- Switched from converting to and from string in execution context call filters. Improves memory footprint and performance.
- Improving performance for client observers by not asking for connected clients on every event. We have a fail-safe if the client is not there.
- Performance optimization for catch-up to start on the next event sequence number greater or equal to where it left off matching the first of the event types it is observing.




# [v8.3.1] - 2023-1-30 [PR: #721](https://github.com/aksio-insurtech/Cratis/pull/721)

### Fixed

- The output folder path for proxy generation was not sanitized and properly resolved to an absolute path, so a relative path of `../Web/API` could end up with a `/your/path/to/the/code//../Web/Api` which then failed getting the correct relative path of a type when outputting types.


# [v8.2.0] - 2023-1-28 [PR: #720](https://github.com/aksio-insurtech/Cratis/pull/720)

### Added

- Adding log enrichment for execution context to Serilog. Automatically hooked up in Application Model. (#445)

Usage:

```csharp
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithExecutionContext()
            .CreateLogger();
```


# [v8.1.3] - 2023-1-27 [PR: #719](https://github.com/aksio-insurtech/Cratis/pull/719)

### Fixed

- Fixing connected client state. (#718)
- Changing the dynamics from Client -> Server connectivity, server no longer pings but instead lets the client know if its considering it disconnected through the result.


# [v8.1.2] - 2023-1-27 [PR: #716](https://github.com/aksio-insurtech/Cratis/pull/716)

### Fixed

- QueryResult and CommandResult was wrongly trying to resolve `validationErrors` while it has been renamed to `validationResults`.
- Supporting clients connecting from outside Docker to Kernel running in Docker by changing their `AdvertisedClientUri` to `host.docker.internal`. (#715)
- Supporting clients connecting with `https` and developer certificates. (#715)
- Improving startup time by running Inbox grain setup in parallel, allowing the Kernel to have first priority of becoming responsive. (#710)
- Run client observer registration in separate task, improving responsiveness for Kernel at startup. (#710)
- Removing duplicate registrations for connected clients. (#714)
- Adding log message for when the client is connected to the kernel. (#711)


# [v8.1.1] - 2023-1-25 [PR: #708](https://github.com/aksio-insurtech/Cratis/pull/708)

### Fixed

- Kernel now starts - which it didn't :(. Fixed a problem with ASP.NET Core reporting `http://+:80` as the server address, which is not a supported format by `Uri`.


# [v8.0.0] - 2023-1-23 [PR: #665](https://github.com/aksio-insurtech/Cratis/pull/665)

## Summary

This is a major release with quite a few changes. Read the changelog carefully for details.
Primarily this release is about paying back some technical debt and fixing some stability issues in the making.
Also a focus on fixing APIs that were wrong, namespaces that were wrong and general consolidation of the codebase for increased maintainability.

### Added

- Validation check during proxy generation if return type and query / command name has the same name and in the same namespace / path. Breaks the build.
- Adding [Seq](https://hub.docker.com/r/datalust/seq) as a Serilog Sink. (#393)

### Changed

- Changed from using a Orleans client to connect to Kernel to a REST API based approach.
- Moved `CommandResult` and `QueryResult` into Fundamentals. Namespace has changed.
- `ITenants` now returns an object called `Tenant` that holds both the Id and the name of the tenant.
- Namespace `Aksio.Cratis.Events.Store` has been changed to `Aksio.Cratis.Events` for all artifacts, such as `EventContext` ++.
- Namespace `Aksio.Cratis.Events.Projections` has been changed to `Aksio.Cratis.Projections` for all artifacts, such as `ProjectionId` ++.
- Namespace `Aksio.Cratis.Events.Observation` has been changed to `Aksio.Cratis.Observation` for all artifacts such as the `[Observer] attribute.
- Namespace change for all event sequences from `Aksio.Cratis.Events.Store` has been changed to `Aksio.Cratis.EventSequences` for all artifacts, such as `IEventLog` ++.
- Namespace `Aksio.Cratis.Events.Schemas` has been changed to `Aksio.Cratis.Schemas` for all artifacts, such as `ISchemaStore` ++.
-`IImmediateProjections` methods now return `ImmediateProjectionResult` instead of the model instance. To access the model you will have to do `.Model` on the result object.
- `DefineState` method on `RuleFor` is now abstract and required to be implemented. The reason for this is that we now have other mechanisms for validation such as `CommandValidator` which should be used instead of `RuleFor`.
- REST APIs are more consistent and not just based on route values. (#396)
- Cluster configuration for a single cluster mode has changed from "local" to "single" to reflect more what it is. This affects any `cratis.json` files and `cluster.json` files.

### Fixed

- Treating `JsonNode`, `JsonObject` and `JsonArray` as any types in the proxy generator.
- Fixed how we do clients inside the Kernel (#93)
- When clients reconnect, they are now sending artifacts again. The reconnect could be because the silo went down. It then needs the client artifacts. (#378)
- Connection Id for clients and connected clients on the Kernel is now consistent. (#218)
- Fixing comparing of projections to avoid replay every time one starts.
- Allowing CS8795 in proxy generation as a valid error, as this error comes early but gets resolved typically by the Microsoft Logger code generator for `[LoggerMessage]` based partial methods.
- Frontend JSON serializer now supports deserializing `any` types by just copying their content. (#533)

# [v7.4.2] - 2023-1-23 [PR: #696](https://github.com/aksio-insurtech/Cratis/pull/696)

### Fixed

- Rules can now be stateless by omitting an implementation of `DefineState` - as originally designed.


# [v7.4.1] - 2023-1-20 [PR: #693](https://github.com/aksio-insurtech/Cratis/pull/693)

### Fixed

- Nested members in validation errors are now also camel cased.


# [v7.4.0] - 2023-1-19 [PR: #689](https://github.com/aksio-insurtech/Cratis/pull/689)

### Added

- Added `RuleForConcept` in the `BaseValidator` type to be able to hook up validation that is for an actual concept and not the primitive it is encapsulating.
- Extracting out discoverability of validators into `IDiscoverableValidator`. This can now be used indepdendently, allowing you to work directly with `AbstractValidator` for instance and still be discovered at runtime.


# [v7.3.2] - 2023-1-13 [PR: #678](https://github.com/aksio-insurtech/Cratis/pull/678)

### Fixed

- Helpers for Tests/Specifications now includes the `ValidFrom` value added during `Append`.



# [v7.3.1] - 2023-1-13 [PR: #676](https://github.com/aksio-insurtech/Cratis/pull/676)

### Fixed

- `ValidFrom` in callbacks when used in `AppendEvent` with integration adapters are now nullable, as it should be.


# [v7.3.0] - 2023-1-13 [PR: #675](https://github.com/aksio-insurtech/Cratis/pull/675)

### Added

- Added a way to provide `ValidFrom` when describing events to append for integrations from a callback.


# [v7.2.0] - 2023-1-12 [PR: #674](https://github.com/aksio-insurtech/Cratis/pull/674)

### Added

- Support for `ValidFrom` for integration adapters as an optional parameter to the `AppendEvent` method.



# [v7.1.0] - 2023-1-11 [PR: #673](https://github.com/aksio-insurtech/Cratis/pull/673)

### Added

- Added support for Command, Query and Concept validators (#671).


# [v7.0.0] - 2023-1-10 [PR: #672](https://github.com/aksio-insurtech/Cratis/pull/672)

## Summary

A missing consideration in the `ObserverStorageProvider` for observer key was causing Inbox observers for different microservices to reuse its state. Causing strange behavior.

This version is therefor a major release, since it actually is not backwards compatible with the stored observers state for inboxes.

To remedy this, you can change the existing observer in the `observers collection` with the `observerId: 85dc950d-1900-4407-a484-ec1e83da16c6`. In the `_id` field you can append the microserviceId you believe is correct (if you only have one, this should be easy).
The expected format is:

`<guid> : <guid> : <guid>`

Where the Guid represent:

`<event sequence id> : <observer id> : <source microservice id>`.

Since the two first segments are known it will become:

`ae99de1e-b19f-4a33-a5c4-3908508ce59f : 85dc950d-1900-4407-a484-ec1e83da16c6 : <source microservice id>`

Concrete example:

`ae99de1e-b19f-4a33-a5c4-3908508ce59f : 85dc950d-1900-4407-a484-ec1e83da16c6 : 51f25e1d-b897-4476-a48d-ce9de38c7589`


### Changed

- Changed the key for Inbox observers to include source microservice id.


# [v6.25.5] - 2023-1-2 [PR: #662](https://github.com/aksio-insurtech/Cratis/pull/662)

### Fixed

- Fixing a bug in the command template so that enumerable properties actually now gets rendered correctly as JS arrays.


# [v6.25.4] - 2023-1-2 [PR: #660](https://github.com/aksio-insurtech/Cratis/pull/660)

### Fixed

- When using Newtonsofts `[JsonConverter(typeof(StringEnumConverter))]`, NJsonSchema would by default treat it as a string value. Changed this behavior and it will now always treat it as an integer.


# [v6.25.3] - 2022-12-30 [PR: #659](https://github.com/aksio-insurtech/Cratis/pull/659)

### Fixed

- Supporting `OneOf` representation of enums when retrieving target type.
- Allowing references for properties, making enums be represented more cohesively in Json schema generation with references.
- Support for enums with values represented as strings when converting from JSON to ExpandoObject internally.


# [v6.25.2] - 2022-12-28 [PR: #657](https://github.com/aksio-insurtech/Cratis/pull/657)

### Fixed

- Fixing `TypeConversion` to handle Guid -> string. It revertet to `Convert.ChangeType()` for this scenario, which is wrong.


# [v6.25.1] - 2022-12-28 [PR: #656](https://github.com/aksio-insurtech/Cratis/pull/656)

### Fixed

- Bug with projections having children - it was assumed the child projection had a key resolver for the parent event, which in most cases it does not.


# [v6.25.0] - 2022-12-28 [PR: #655](https://github.com/aksio-insurtech/Cratis/pull/655)

### Added

- Bringing back `TenantConfiguration` that was considered a wrong design. Turns out it was fine for a specific purpose; associating key / value pairs with a tenant for simple configuration values.
- Support for key / value configuration per tenant in the `cratis.json` file - specifically aimed towards local development and team work and the ability to check in configuration into a repository.



# [v6.24.0] - 2022-12-27 [PR: #653](https://github.com/aksio-insurtech/Cratis/pull/653)

> WARNING: Version 6.23.0 introduced a configuration system for tenants. This was a completely wrong design and put the responsibility in the wrong place. It has thus been removed.

### Added

- Support for multi tenant configuration objects using the `[Configuration]` attribute. Added a `PerTenant` property. Configuration system leverages this and provides a per tenant configuration instance by convention.

# [v6.23.0] - 2022-12-25 [PR: #651](https://github.com/aksio-insurtech/Cratis/pull/651)

### Added

- Added a new method on `ExecutionContextManager` called `.ForTenant()` that establishes a temporary disposable execution context scope. It still leverages the same underlying mechanism but automatically resets back to an execution context without tenant set once out of scope.
- Added a `ITenants` in the .NET Client that can be used to get all configured tenants.
- Added  a `ITenantConfiguration` for getting configured key/value pairs associated with a tenant.
- Added a REST API for the Kernel (`/api/configuration/tenants`) that allows for working with the configuration of tenants.

### Fixed

- Removing hard-coded Development tenant value for integration adapters.


# [v6.22.0] - 2022-12-23 [PR: #650](https://github.com/aksio-insurtech/Cratis/pull/650)

### Added

- Added frontend support for identity that works in conjunction with the result from the [Aksio ingress middleware](https://github.com/aksio-insurtech/IngressMiddleware).


# [v6.21.5] - 2022-12-21 [PR: #647](https://github.com/aksio-insurtech/Cratis/pull/647)

### Fixed

- Setting `isPerforming` on query result when using React hook for queries to `true` for the initial state.


# [v6.21.4] - 2022-12-21 [PR: #646](https://github.com/aksio-insurtech/Cratis/pull/646)

### Fixed

- Fixing queries not to execute twice when explicitly executing them via the React hook. (#644)
- Fixing so that we allow arguments in queries that hold 0 for number or false for booleans. (#645)


# [v6.21.3] - 2022-12-19 [PR: #642](https://github.com/aksio-insurtech/Cratis/pull/642)

### Fixed

- The `EnumerableConceptAsJsonConverterFactory`  recognized any type with generics and the first generic argument as concept to be valid. This fails with something like anonymous types, which have their properties represented as generic arguments. If the first property then was a concept, this was picked up. This fixes that by making sure it is an enumerable.


# [v6.21.2] - 2022-12-18 [PR: #641](https://github.com/aksio-insurtech/Cratis/pull/641)

### Fixed

- Cache cursor was not using correct event store API and was relying on in-memory filtering and a faulty next logic for the cursor.
- Client deserialization of arrays in `ObservableQueryFor` will now not try to deserialize if the data coming back is not an array.


# [v6.21.1] - 2022-12-16 [PR: #639](https://github.com/aksio-insurtech/Cratis/pull/639)

### Fixed

- Honoring the actual type of the query argument when generating proxies. (#636)
- Forcing invariant culture for Kernel. (#611)
- Returning proper query result structure with all details which then automatically fixed #637
- Returning correct command result even if exceptions happen in controller action.
- Making member names camel cased in validation result. (#593)


# [v6.21.0] - 2022-12-15 [PR: #633](https://github.com/aksio-insurtech/Cratis/pull/633)

### Added

- `.UseAksio()` for `IHostBuilder` now supports an optional `Action<MvcOptions>` parameter.

```csharp
         Host.CreateDefaultBuilder(args)
            .UseAksio(
                microserviceId: "097398ed-d43b-4499-bcf8-f5403a7fec4d",
                mvcOptionsDelegate: (options) =>
                {
                    // Manipulate options / call extension methods - which is of type MvcOptions
                })
```

### Fixed

- Removes faulty cache mechanism for the event store.
- Allowing missing event schemas when connecting one microservice Outbox to the Inbox of another. It now copies the missing event schema from source to destination. It does require the schema to be there for serialization purposes and later for schema validation and migrations.


# [v6.20.0] - 2022-12-14 [PR: #594](https://github.com/aksio-insurtech/Cratis/pull/594)

### Added

- `ProjectionSpecificationContext` now supports getting instances of models based on composite keys.
- Support for `.AddChild()` directly on an event. (#591)

You can add objects that does not have a property that identifies them:

```csharp
public class MyProjection : IProjectionFor<Something>
{
    public ProjectionId Identifier => "d661904f-15e0-4a96-a0cc-c7389635e4cd";

    public void Define(IProjectionBuilderFor<DebitAccountLatestTransactions> builder) => builder
         .From<SomeEvent>(_ => _
             .Set(m => m.SomeProperty).To(e => e.SomeProperty)
             .AddChild(m => m.Children, c => c
                 .FromObject(e => e.Child));
}
```

Or you can add objects that are uniquely identifable from a key:

```csharp
public class MyProjection : IProjectionFor<Something>
{
    public ProjectionId Identifier => "d661904f-15e0-4a96-a0cc-c7389635e4cd";

    public void Define(IProjectionBuilderFor<DebitAccountLatestTransactions> builder) => builder
         .From<SomeEvent>(_ => _
             .Set(m => m.SomeProperty).To(e => e.SomeProperty)
             .AddChild(m => m.Children, c => c
                 .IdentifiedBy(m => m.Id)
                 .UsingKey(e => e.Id)
                 .FromObject(e => e.Child));
}
```


### Fixed

- Fixing array handling to adher to the schema and not the incoming type in `ExpandoObjectConverter` for JSON.
- Fixing array handling to adher to the schema and not the incoming type in `ExpandoObjectConverter` for BSON.
- Fixing the support for root properties that indicate that the identified by property should be the object itself.
- Fixing schemas to be forced to camel case properties. NJsonSchema failed this with complex types with properties on them that implemented IEnumerable.
- Fixing deserialization of enumerable of `ConceptAs` types. Most circumstances was caught by the `ConceptAsJsonConverter` directly as the serializer recognized the item type. But for `RulesFor` with collections of concept types, this didn't not work.
- Fixed initial state for projection to be adhering to the schema, yielding correct types within the engine.
- Fixed a regression related to adding children that are not uniquely identifiable.
- Fixed MongoDB projection sink to use `$push` when adding the arrays. `$addToSet` checks for uniqueness and treats the document being added as a value type. This made it impossible to add "keyless" children that had same values to an array.
- Fixing so that `.ToValue()` supports concept values.
- Fixing `.ToString()` when creating value expression to be invariant in culture.
- FIxing problems related to knowing which type to convert to due to types having the `JsonObjectType.Null` flag set.
- Fixing fall back type conversion used in projections and other places to be culture invariant.
- Fixing `.ToValue()` expression parser to support characters typically involved in date & time values.
- Fixing `ConceptAs` JSON converter to support enum types.
- Fixing `TypeConverter` to parse `DateTime` correclty based on the format if represented as string.
- Fixing value expressions representing date and time types to convert to ISO 8601 strings with invariant culture.



# [v6.19.0] - 2022-12-12 [PR: #630](https://github.com/aksio-insurtech/Cratis/pull/631)

### Added

- Adding support for providing identity details based on HTTP headers coming in. (#621)

# [v6.18.2] - 2022-12-12 [PR: #630](https://github.com/aksio-insurtech/Cratis/pull/630)

### Fixed

- JSON Converter for dealing with enumerable of objects with Id or concepts now stops at the end of the array, not trying to consume the rest of the JSON.


# [v6.18.1] - 2022-12-9 [PR: #627](https://github.com/aksio-insurtech/Cratis/pull/627)

### Fixed

- Making the enumerable Concept JSON converter actually work as promised and be able to deserialize a JSON array of concepts.


# [v6.18.0] - 2022-12-5 [PR: #620](https://github.com/aksio-insurtech/Cratis/pull/620)

### Added

- Support for returning a response object as part of a command. POST controller actions can simply just return anything now and this will now become serialized as part of the `CommandResult` and proxy generator generates a type safe version of this with serialization information on it. (#499)

### Fixed

- Command result is now alway included, even when it is a HTTP 200. (#359)



# [v6.17.9] - 2022-12-5 [PR: #618](https://github.com/aksio-insurtech/Cratis/pull/618)

### Fixed

- Adding assembly resolvers when using types in any of the discovered types that comes from unloaded assemblies.


# [v6.17.8] - 2022-11-27 [PR: #617](https://github.com/aksio-insurtech/Cratis/pull/617)

### Fixed

- Missing XML comments.


# [v6.17.7] - 2022-11-27 [PR: #616](https://github.com/aksio-insurtech/Cratis/pull/616)

## Summary

In the ASP.NET Core 6 code there is a middleware called WebSocketMiddleware. The WebSocketManager
that we ask for .IsWebSocketRequest forwards this call to it.
This property calls internally a method called CheckSupportedWebSocketRequest which will check
the following Http Headers for valid values:
Upgrade with value Upgrade
Connection with value websocket

If they are correct, it will consider it an upgrade of the protocol and will then validate and use
the values from the Web Socket specific headers:
Sec-WebSocket-Protocol
Sec-WebSocket-Extensions
Sec-WebSocket-Version
Sec-WebSocket-Key

When running in an environment with multiple reverse proxies you can end up with the proxy adding
to the values if the values are already there, forming a collection of values as comma separated
values in the HTTP header.

The validation code in ASP.NET validates that the version is supported and that the key is valid.
Throughout the validation code in ASP.NET it recognizes the fact that it could hold multiple values
and loops through the values, except for the key - which it just does .ToString() on, which then
gives you the comma separated string.

The key is expected to be a base64 encoded byte array of 16 bytes, and obviously this would not
then be valid and we're not allowed to upgrade the connection.

The purpose of the key coming from the client is to use it and combine with a server key and send
back on the response to form a valid connection.

This version contains a workaround for this that assumes that the last key is the one from the client
and strips away any other keys and uses it as a single key instead.

### Fixed

- Workaround implemented for environments running with multiple reverse proxies that appends a new value for **Sec-WebSocket-Key** HTTP header for each hop when doing a WebSocket connection upgrade. This was not supported by ASP.NET, we take the last key and assume it is the client key.


# [v6.17.6] - 2022-11-26 [PR: #614](https://github.com/aksio-insurtech/Cratis/pull/614)

### Fixed

- Adding trace log messages to the `QueryActionFilter`.


# [v6.17.4] - 2022-11-18 [PR: #605](https://github.com/aksio-insurtech/Cratis/pull/605)

### Fixed

- Fixing Dockerfile for the development MongoDB image and adding a pipeline to build and deploy it to Docker Hub. (#603)


# [v6.17.3] - 2022-11-18 [PR: #604](https://github.com/aksio-insurtech/Cratis/pull/604)

### Fixed

- Adding a log message to see why the cluster client is not able to connect to Cratis Kernel.


# [v6.17.2] - 2022-11-14 [PR: #601](https://github.com/aksio-insurtech/Cratis/pull/601)

### Fixed

- Ignoring load failures related to project references assemblies. These assemblies will then not be part of type discovery.


# [v6.17.1] - 2022-11-14 [PR: #600](https://github.com/aksio-insurtech/Cratis/pull/600)

### Fixed

- Changing event log implementation used in specifications so that it serializes the incoming event as the regular production one. The result is correct casing and types.


# [v6.17.0] - 2022-11-10 [PR: #598](https://github.com/aksio-insurtech/Cratis/pull/598)

### Added

- `useCommand()` now returns a third component to the tuple; `ClearCommandValues` which sets them all to undefined. The proxy generator honors this and creates a proxy that does the same. (#595)

### Fixed

- Fixing the `SetCommandValues` returned from `useCommand()` to only ignore properties that are either undefined or null. Allowing for empty strings to be set. (#596)


# [v6.16.9] - 2022-11-8 [PR: #590](https://github.com/aksio-insurtech/Cratis/pull/590)

### Fixed

- Fixing so that we get the schema-store using the `ProviderFor<>`. It is singleton per microservice, but we were just injecting it without thinking of the scoping of where it was injected. The consequence of that is that we were getting the wrong schema store every now and then.


# [v6.16.8] - 2022-11-8 [PR: #589](https://github.com/aksio-insurtech/Cratis/pull/589)

### Fixed

- Changing all MongoDB calls for the schema store to be synchronous. To avoid problems with the Orleans task model.


# [v6.16.7] - 2022-11-8 [PR: #588](https://github.com/aksio-insurtech/Cratis/pull/588)

### Fixed

- Moved away from using async API for mongo db schema store.
- Supporting enums all the way through the pipeline.


# [v6.16.6] - 2022-11-8 [PR: #586](https://github.com/aksio-insurtech/Cratis/pull/586)

### Fixed

- We have identified a major bug in the cache. We will remove our cache implementation and go for the vanilla Orleans one instead, very soon. Until that point, we've set the cache to hold 20K events for now. This is absolutely not good.
- Adding more details with event names to the log.


# [v6.16.5] - 2022-11-7 [PR: #584](https://github.com/aksio-insurtech/Cratis/pull/584)

### Fixed

- Making deep cloning of JSON objects use the global `JsonSerializerOptions` to support the types we support.
- Setting default ignore condition for JSON serialization to ignore null values.
- Ignoring properties that are null when converting to `ExpandoObject` and `JsonObject` internally. The JSON serializer will then ignore these properties when deserializing in the client.


# [v6.16.4] - 2022-11-7 [PR: #583](https://github.com/aksio-insurtech/Cratis/pull/583)

### Fixed

- Composite key resolver now maintains the correct property path, enabling it to resolve to the correct type for the nested properties.


# [v6.16.3] - 2022-11-7 [PR: #582](https://github.com/aksio-insurtech/Cratis/pull/582)

### Fixed

- Supporting inherited properties in model schemas.
- Adding support for resolving target type based on reference type, for instance enums will be such a thing.
- Fixing a bug introduced in MongoDB projection sink where it tried to resolve using the wrong property and couldn't find the target type as a result.
- Projections event value resolvers now convert to the target type, assuring we have the correct types when comparing what we already have in the system and what we project ontop.
- Fixing object cloning that leverages JSON serialization to support the custom converters.
- Fixing the `InMemoryProjectionSink` to honor the actual key target types.


# [v6.16.2] - 2022-11-5 [PR: #580](https://github.com/aksio-insurtech/Cratis/pull/580)

### Fixed

- Fixing so that we get the actual enum value as an integer before sending it to the kernel for the `.ToValue()` support till we have implemented #579.


# [v6.16.1] - 2022-11-5 [PR: #578](https://github.com/aksio-insurtech/Cratis/pull/578)

### Fixed

- Fixed Immediate projections after internal type safety changes, making sure we can serialize the result properly. It crashed under certain circumstances.


# [v6.16.0] - 2022-11-5 [PR: #577](https://github.com/aksio-insurtech/Cratis/pull/577)

## Summary

This version brings improved stability and predictability to child relationships.
It is now possible to specify a relationship of events where all the events are linked to the same event source id.
The resolution through the parent key will by default resolve itself by using the parents event source id if no `.UsingParentKey()` or the new `.UsingParentKeyFromContext()` is specified. With such a structure, you might need to resolve the actual key for the child item, this can now be done by using the `.UsingKey()` on the child level of the projection.

### Added

- Supporting `.UsingKey()` for identifying child items without forcing it to be identified through the `EventSourceId`.
- Add support for mapping to constants using `.ToValue()`for the set builders for projections. (#573)
- Support for using parent key from context `.UsingParentKeyFromContext()`. (#576)


# [v6.15.11] - 2022-11-4 [PR: #574](https://github.com/aksio-insurtech/Cratis/pull/574)

### Fixed

- `DateOnly`and `TimeOnly` serialization from `JsonValue` now parses the string if its not a `DateTime` source type.
- Switched to using the `System.Text.Json` serializer when saving the event content in the event log, which then persists `DateOnly`and `TimeOnly` and other types with converters correctly.


# [v6.15.10] - 2022-11-4 [PR: #572](https://github.com/aksio-insurtech/Cratis/pull/572)

### Fixed

- Making parent hierarchy key resolution use any of the parents event types, not forcing the first in its definition. The first in the definition might not be the first that gets appended.


# [v6.15.9] - 2022-11-4 [PR: #571](https://github.com/aksio-insurtech/Cratis/pull/571)

### Fixed

- FIxing conversion of events to treat null values as null and not try to convert them to target type.
- The array indexer values - key is now honoring the target models type representing the key. Making child relations work again.
- Fixing the recursiveness of the internal `.FromParentHierarchy()` key resolver to actually be recursive. Its implementation was completely wrong where it did a recurse, but also a loop of its own hierarchy and getting random results.


# [v6.15.8] - 2022-11-3 [PR: #567](https://github.com/aksio-insurtech/Cratis/pull/567)

### Fixed

- The MongoDB Projection Sink is now much stricter on the models schema and target types and will convert both keys and properties according to the schema.


# [v6.15.7] - 2022-11-3 [PR: #566](https://github.com/aksio-insurtech/Cratis/pull/566)

### Fixed

- Making sure we set the correct type based on the model schema for properties in a changeset. This fixes an exception that occurred when Guids were used, as it didn't know how to make them with the correct guid representation for Mongo.


# [v6.15.6] - 2022-11-2 [PR: #565](https://github.com/aksio-insurtech/Cratis/pull/565)

### Fixed

- Fixed `ExpandoObjectConverter` for edge case value conversions for double <-> float and also for different date & time types.


# [v6.15.5] - 2022-11-2 [PR: #564](https://github.com/aksio-insurtech/Cratis/pull/564)

### Fixed

- The MongoDB Tools now actually working in the base development image.


# [v6.15.4] - 2022-11-2 [PR: #563](https://github.com/aksio-insurtech/Cratis/pull/563)

### Fixed

- Adding MongoDB Tools to the base development image.


# [v6.15.3] - 2022-11-2 [PR: #562](https://github.com/aksio-insurtech/Cratis/pull/562)

### Fixed

- Adding the missing MongoDB Tools.


# [v6.15.2] - 2022-11-2 [PR: #561](https://github.com/aksio-insurtech/Cratis/pull/561)

### Fixed

- Adding the missing MongoDB Database Tools to the base development image.


# [v6.15.1] - 2022-11-2 [PR: #560](https://github.com/aksio-insurtech/Cratis/pull/560)

### Fixes

- Adding the missing MongoDB Database Tools to the base development image.

# [v6.15.0] - 2022-11-2 [PR: #558](https://github.com/aksio-insurtech/Cratis/pull/558)

## Summary

This release is primarily about making projections more consistent in types, fixing problems that occurred when introducing the `.All()` functionality, which turned out to be broken due to internals not working as expected.

Below is a full sample of how a projection with `.FromEvery()` and `.Join()` works:

```csharp
public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .WithInitialModelState(() => new(Guid.Empty, string.Empty, null!, new(string.Empty, string.Empty), 0, false, DateTimeOffset.MinValue))

            // From every event in the projection, including any child projections - set last updated
            .FromEvery(_ => _
                .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred)
                .IncludeChildProjections())

            // Join in events from a different event source identifier
            .Join<AccountHolderRegistered>(_ => _
                .On(model => model.AccountHolderId)
                .Set(model => model.AccountHolder.FirstName).To(@event => @event.FirstName)
                .Set(model => model.AccountHolder.LastName).To(@event => @event.LastName))

            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.AccountHolderId).To(@event => @event.Owner)
                .Set(model => model.HasCard).To(@event => @event.IncludeCard))
            .From<DebitAccountNameChanged>(_ => _
                .Set(model => model.Name).To(@event => @event.Name))
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(model => model.Balance).With(@event => @event.Amount))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(model => model.Balance).With(@event => @event.Amount))
            .RemovedWith<DebitAccountClosed>();
}

```

### Added

- Adding extension method for `MongoDBCollectionExtensions` for getting a single document by id. You can now do `collection.FindById(<your id>)`. The type of Id will be inferred.
- Adding extension method for `MongoDBCollectionExtensions` for observing a single document by id for `ClientObservable`. You can now do `collection.ObserveById(<your id>)`. The type of Id will be inferred.
- Adding extension method for `MongoDBCollectionExtensions` for observing a single document based on a filter or LINQ expression for `ClientObservable`. You can now do `collection.ObserveSingle(<your filter>)`. The type of Id will be inferred.


### Fixed

- CHANGE: Since `.All()` was broken, we decided not to bump the major for a rename. This method is now called `.FromEvery()` reflecting that is working on every event for the projection.
- Limiting changes when using `.FromEvery()` that are applied to one for the specific event type that occurs rather than one for all event types projection projects.
- Making sure we have distinct array indexers, no duplicates.
- Adding property `.ModelCount()` on the `ProjectionSpecificationContext` to get the number of models affected within the context.
- Improved type safety internal to the event store by changing from `JsonObject` to `ExpandoObject` and honoring the JSON schema passed in for both event types and model types.
- Supporting join expressions for Projections in context of specifications.


# [v6.14.0] - 2022-10-30 [PR: #559](https://github.com/aksio-insurtech/Cratis/pull/559)

### Added

- Support for join expressions. This supports a full eventual consistent model enabling events to be out of order in the relationship. The projected end result will be resolved when events are there.

```csharp
public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .WithInitialModelState(() => new(Guid.Empty, string.Empty, null!, new(string.Empty, string.Empty), 0, false, DateTimeOffset.MinValue))

            // This is how one sets up the relationship
            .Join<AccountHolderRegistered>(_ => _
                .On(model => model.AccountHolderId)  // There has to be a property that holds the key, typically the event source Id of the other type being joined.
                .Set(model => model.AccountHolder.FirstName).To(@event => @event.FirstName)
                .Set(model => model.AccountHolder.LastName).To(@event => @event.LastName))
            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.AccountHolderId).To(@event => @event.Owner)
                .Set(model => model.HasCard).To(@event => @event.IncludeCard))
            .From<DebitAccountNameChanged>(_ => _
                .Set(model => model.Name).To(@event => @event.Name))
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(model => model.Balance).With(@event => @event.Amount))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(model => model.Balance).With(@event => @event.Amount))
            .RemovedWith<DebitAccountClosed>();
}

```


# [v6.13.0] - 2022-10-28 [PR: #557](https://github.com/aksio-insurtech/Cratis/pull/557)

### Added

- Adding the ability to project to model properties from properties from the event context from all events in a projection. Useful when one wants to set something like `LastUpdated` date time and base it on the `Occurred`.

```csharp
public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .WithInitialModelState(() => new(Guid.Empty, string.Empty, Guid.Empty, new(string.Empty, string.Empty), 0, false, DateTimeOffset.MinValue))

            // Set LastUpdated to Occurred for all events, and include any child projections
            .All(_ => _
                .Set(model => model.LastUpdated).ToEventContextProperty(ContextBoundObject => ContextBoundObject.Occurred)
                .IncludeChildProjections())

            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.OwnerId).To(@event => @event.Owner)
                .Set(model => model.HasCard).To(@event => @event.IncludeCard))
            .From<DebitAccountNameChanged>(_ => _
                .Set(model => model.Name).To(@event => @event.Name))
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(model => model.Balance).With(@event => @event.Amount))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(model => model.Balance).With(@event => @event.Amount))
            .RemovedWith<DebitAccountClosed>();
}

```


# [v6.12.1] - 2022-10-27 [PR: #556](https://github.com/aksio-insurtech/Cratis/pull/556)

### Fixed

- Due to #359 the frontend does not get a valid JSON on 200 OK responses. This is now taken into account and it will return a valid command result if this happens.


# [v6.12.0] - 2022-10-27 [PR: #537](https://github.com/aksio-insurtech/Cratis/pull/537)

### Added

- Support for setting initial model state for new instances of model used when defining a projection. `IProjectionBuilderFor` has a new `.WithInitialModelState()` method.
- Support for composite keys with projections. For model property builders (e.g. `.From()`) has new methods: `.UseCompositeKey<TKeyType>()` allowing to build up a typed composite key. (#539)
- Started work on support for joining on events with projections. For model property builders (e.g. `.From()`) there is now a `.Join<TEvent>()` method allowing to set up relationships with other events. API is in place, but doesn't do much. (#538)



# [v6.11.8] - 2022-10-19 [PR: #543](https://github.com/aksio-insurtech/Cratis/pull/543)

### Added

- MUI package with utilities when working with MUI/Material. Currently only has an encapsulation for modals. See docs for more details.


# [v6.11.7] - 2022-10-18 [PR: #541](https://github.com/aksio-insurtech/Cratis/pull/541)

### Fixed

- `JsonSerializer` was referenced wrongly within `QueryResult`.


# [v6.11.6] - 2022-10-14 [PR: #536](https://github.com/aksio-insurtech/Cratis/pull/536)

### Fixed

- Removing debugger statement in proxy generator that was left in while hunting down a problem.


# [v6.11.5] - 2022-10-14 [PR: #534](https://github.com/aksio-insurtech/Cratis/pull/534)

### Fixed

- Supporting nullable properties. When one has an enum that is marked as a nullable in a type, it does not have the type itself annotated as nullable, but the property.


# [v6.11.4] - 2022-10-14 [PR: #530](https://github.com/aksio-insurtech/Cratis/pull/530)

### Fixed

- Removing reference to the proxy generator for all assemblies related to Kernel and just add it to the API projects. This way we're not pulling the proxy generator implicitly into everything when referencing the C# SDK. (Plus added benefit of faster Cratis compile times :))


# [v6.11.3] - 2022-10-14 [PR: #528](https://github.com/aksio-insurtech/Cratis/pull/528)

### Fixed

- Fixing a problem with enum proxy generators that were too complex and looking for the wrong syntax element during compilation, causing them to crash when enum was in a 3rd party referenced assembly.


# [v6.11.2] - 2022-10-13 [PR: #527](https://github.com/aksio-insurtech/Cratis/pull/527)

### Fixed

- Frontend queries based now has a `hasData` property which reflects correctly whether or not there actually is data. This gets used in the `QueryResultWithState` and can be used inside the frontend to check if there actually is data and make decisions based on it.


# [v6.11.1] - 2022-10-13 [PR: #526](https://github.com/aksio-insurtech/Cratis/pull/526)

### Added

- Adding the `EventSequenceNumber` to `EventContext` - observers will now be able to see which sequence number the event is within its source sequence. (#498)
- Adding state for query results for the React hooks, enabling one to know if the query is performing or not. Can then typically used for loading spinners or similar. (#500)
- Introducing `[DerivedType]` for defining discriminators used by serializers. (#515, #501)
- Adding sequence number to the event context (#498)
- Proxy generator support for derived types. (#518)
- Adding support for derived types deserialization in the frontend. (#520)

### Fixed

- Fixing proxy generator to output enums. (#261)
- Fixing frontend deserializer to convert from string representation of dates to actual `Date` type. (#208)
- Avoid confusion of properties that starts with the same text. This fix will prioritize an exact match for properties and at the same time supported nested properties from a top level. (#512)


# [v6.10.36] - 2022-10-12 [PR: #516](https://github.com/aksio-insurtech/Cratis/pull/516)

### Fixed

- Making all MongoDB calls done during streams and grains to be synchronous. They're probably using `.ConfigureAwait(false)` or `Task.Run()` inside the C# driver that prevents it from returning gracefully back to the task context the Orleans task scheduler is expecting.


# [v6.10.35] - 2022-10-5 [PR: #517](https://github.com/aksio-insurtech/Cratis/pull/517)

### Fixed

- Changing the base image to the ASP.NET Core runtime for the base development image.


# [v6.10.34] - 2022-9-30 [PR: #508](https://github.com/aksio-insurtech/Cratis/pull/508)

### Fixed

- Introducing a base development Docker image to improve build times dramatically.


# [v6.10.33] - 2022-9-20 [PR: #496](https://github.com/aksio-insurtech/Cratis/pull/496)

### Fixed

- Fixing JSON serialization of concepts to not just write them as strings.


# [v6.10.32] - 2022-9-16 [PR: #495](https://github.com/aksio-insurtech/Cratis/pull/495)

### Fixed

- Fixing `DateOnlySerializer` to use mid-day point for time component when serializing to Bson Date. Without it, the date will slide a day per serialization due to timezones.


# [v6.10.31] - 2022-9-14 [PR: #492](https://github.com/aksio-insurtech/Cratis/pull/492)

### Fixed

- Making it clearer when an event type is missing from the event schema store by throwing an explicit exception.


# [v6.10.30] - 2022-9-13 [PR: #484](https://github.com/aksio-insurtech/Cratis/pull/484)

### Fixed

- MongoDB concept serializer now supporting null properly. If a value ids defined as Mongo null, it will return a `default()`of the concept type.
- Adding missing `validFrom` argument to the `IEventOutbox` interface.


# [v6.10.29] - 2022-9-8 [PR: #478](https://github.com/aksio-insurtech/Cratis/pull/478)

### Fixed

- Fixing caching between jobs for x64 and development image building, so that we actually get the binaries into the docker image.


# [v6.10.28] - 2022-9-8 [PR: #477](https://github.com/aksio-insurtech/Cratis/pull/477)

### Fixed

- Fixed correct type output for `DateOnly` and `TimeOnly` to JS `Date`.
- FIxing import statements to make sure they are sanitized and adding `./` if needed, not rooted.


# [v6.10.27] - 2022-9-8 [PR: #476](https://github.com/aksio-insurtech/Cratis/pull/476)

### Fixed

- Making projections accept event types based only on their identifier, ignoring generation and whether or not it is marked public.


# [v6.10.26] - 2022-9-8 [PR: #475](https://github.com/aksio-insurtech/Cratis/pull/475)

### Fixed

- Fixing so that automated tests/specifications for projections just looks at the event type identifier and ignores the public flag when filtering. As the rest of the system does.


# [v6.10.25] - 2022-9-7 [PR: #473](https://github.com/aksio-insurtech/Cratis/pull/473)

### Fixed

- Projections are now only looking at the event type identifier and ignoring generation and whether or not it is public when filtering event types it is interested in.


# [v6.10.24] - 2022-9-6 [PR: #472](https://github.com/aksio-insurtech/Cratis/pull/472)

### Fixed

- Reverting the inclusion of PDB files. Relying on source link and .snupkg.


# [v6.10.22] - 2022-9-2 [PR: #469](https://github.com/aksio-insurtech/Cratis/pull/469)



# [v6.10.20] - 2022-9-2 [PR: #467](https://github.com/aksio-insurtech/Cratis/pull/467)



# [v6.10.19] - 2022-9-2 [PR: #466](https://github.com/aksio-insurtech/Cratis/pull/466)

### Fixed

- Fixing cache keys for the data used for transfer between jobs, so that we actually get the artifacts built.


# [v6.10.18] - 2022-9-2 [PR: #465](https://github.com/aksio-insurtech/Cratis/pull/465)

### Fixed

- Taking out `appsettings.json` from the built Docker images. One needs now to configure this and mount it in for logging and other things.


# [v6.10.17] - 2022-9-2 [PR: #464](https://github.com/aksio-insurtech/Cratis/pull/464)

### Fixed

- Adding NPM_AUTH_TOKEN to NPM publish package step in publish-packages job of our CI/CD.


# [v6.10.16] - 2022-9-2 [PR: #463](https://github.com/aksio-insurtech/Cratis/pull/463)

### Fixed

- Fixing publish-packages job of CI/CD with regards to the cache folder for node_modules++


# [v6.10.15] - 2022-9-2 [PR: #462](https://github.com/aksio-insurtech/Cratis/pull/462)

### Fixed

- Adding yarn install to the publish packages job of our CI/CD


# [v6.10.14] - 2022-9-2 [PR: #461](https://github.com/aksio-insurtech/Cratis/pull/461)

### Fixed

- `appsettings.json` can be placed in `./config` folder, adding it to search for the application model.



# [v6.10.13] - 2022-9-2 [PR: #459](https://github.com/aksio-insurtech/Cratis/pull/459)

### Fixed

- 🤞🏻 CI/CD had job conditionals using true instead of 'true' for whether or not to publish.


# [v6.10.12] - 2022-9-2 [PR: #458](https://github.com/aksio-insurtech/Cratis/pull/458)

### Fixed

- Conditionals for jobs in CI/CD workflow was wrong. Hoping this will actually publish things now.


# [v6.10.11] - 2022-9-2 [PR: #457](https://github.com/aksio-insurtech/Cratis/pull/457)

### Fixed

- CI/CD pipeline didn't publish


# [v6.10.9] - 2022-9-1 [PR: #454](https://github.com/aksio-insurtech/Cratis/pull/454)

### Fixed

- Immediate projections now set the `Id` property to the `modelKey` being  asked for. A future expansion on this will be to allow defininig which property is the `Id` property (#455)

# [v6.10.8] - 2022-9-1 [PR: #453](https://github.com/aksio-insurtech/Cratis/pull/453)

### Fixed

- Path to x64 cache used between jobs in CI/CD.


# [v6.10.7] - 2022-9-1 [PR: #452](https://github.com/aksio-insurtech/Cratis/pull/452)

### Fixed

- CI/CD workflow adjustments


# [v6.10.6] - 2022-9-1 [PR: #451](https://github.com/aksio-insurtech/Cratis/pull/451)

### Fixed

- CI/CD pipeline tweaking.


# [v6.10.5] - 2022-9-1 [PR: #450](https://github.com/aksio-insurtech/Cratis/pull/450)

### Fixed

- Tweaking publish workflow.


# [v6.10.4] - 2022-9-1 [PR: #449](https://github.com/aksio-insurtech/Cratis/pull/449)

### Fixed

- Working on optimizing CI/CD pipelines to work in parallel.


# [v6.10.3] - 2022-9-1 [PR: #448](https://github.com/aksio-insurtech/Cratis/pull/448)

### Fixed

- Fixing observers to resume failed partitions as long as sequence number is the same or more than the failed sequence number. (#446)
- Adding explicit error message when trying to get immediate projection for model type that does not have one defined.
- Reinstated `css-modules-typescript-loader instead` of `@teamsupercell/typings-for-css-modules-loader` because it broke at production builds.


# [v6.10.2] - 2022-8-30 [PR: #442](https://github.com/aksio-insurtech/Cratis/pull/442)

### Fixed

- Converting to `any` for query arguments when passed down to base type for `useQuery()` and `useObservableQuery()` hooks.


# [v6.10.1] - 2022-8-30 [PR: #441](https://github.com/aksio-insurtech/Cratis/pull/441)

### Fixed

- Adding the `Telemetry` configration object to the Cratis Kernel configuration object.


# [v6.10.0] - 2022-8-29 [PR: #437](https://github.com/aksio-insurtech/Cratis/pull/437)

### Added

- Adding support for Application Insights for Serilog logging
- Adding support for Application Insights telemetry for Orleans from config


# [v6.9.4] - 2022-8-25 [PR: #432](https://github.com/aksio-insurtech/Cratis/pull/432)

### Fixed

- Removing CSS rule from WebPack setup, with the SASS rule we seem to be picking up CSS as well.


# [v6.9.3] - 2022-8-25 [PR: #431](https://github.com/aksio-insurtech/Cratis/pull/431)

### Fixed

- Fixing script for updating versions when publishing. The `edit-json-file` package had a breaking change or bug causing problems.


# [v6.9.2] - 2022-8-25 [PR: #430](https://github.com/aksio-insurtech/Cratis/pull/430)

### Fixed

- Upgrading React in Aksio Microservice template


# [v6.9.1] - 2022-8-25 [PR: #428](https://github.com/aksio-insurtech/Cratis/pull/428)

### Fixed

- React upgraded to version 18.0
- Upgraded all NuGet dependencies to latest
- Latest version of .NET for build actions


# [v6.9.0] - 2022-8-19 [PR: #419](https://github.com/aksio-insurtech/Cratis/pull/419)

## Summary

Adding support for `DateOnly` and `TimeOnly` throughout all pipelines.


### Added

- Added Json converters for `DateOnly` and `TimeOnly`
- Added Type converters for `DateOnly` and `TimeOnly`
- Added MongoDB serializers for `DateOnly` and `TimeOnly`


# [v6.8.0 & v6.8.1] - 2022-8-17 [PR: #417](https://github.com/aksio-insurtech/Cratis/pull/417)

## Summary

Adding support for performing operations after the config objects values has been bound up.
Automatically recognized. Below is a sample, read more in the documentation:

```csharp
[Configuration("MyConfig")]
public class ConfigObject : IPerformPostBindOperations
{
    public void Perform()
    {
        // Perform a post bind operation
    }
}
```

### Added

- Configuration objects can now implement `IPerformPostBindOperations` to be called after values have been bound to it.

### Fixed

- Fixed issues with creating and updating module.scss.ts.d-files (Issue #411)
- Add typings for css modules loader to common WebPack config (Issue #412)


# [v6.7.3] - 2022-8-8 [PR: #408](https://github.com/aksio-insurtech/Cratis/pull/408)

### Fixed

- Adding support for booleans in events for projections. (#406)


# [v6.7.2] - 2022-8-8 [PR: #405](https://github.com/aksio-insurtech/Cratis/pull/405)

### Fixed

- Added cache to development to decrease startup time. (#403)



# [v6.7.1] - 2022-8-4 [PR: #401](https://github.com/aksio-insurtech/Cratis/pull/401)

### Fixed

- Messed up the API in 6.7.0 for `EventContext.Empty()` should've been `EventContext.Empty`.



# [v6.7.0] - 2022-8-4 [PR: #400](https://github.com/aksio-insurtech/Cratis/pull/400)

### Added

- Adding convenience methods for `EventContext`. (#398)

```csharp
var eventContext = EventContext.From("<some event source id>");

// Use EventContext
```

### Fixed

- Fixes import paths to be correct across platforms. (#395)

# [v6.6.1] - 2022-8-4 [PR: #400](https://github.com/aksio-insurtech/Cratis/pull/394)

### Fixed

- Updated webpack-cli to 4.10.0 to make it work on windows

# [v6.6.0] - 2022-6-30 [PR: #392](https://github.com/aksio-insurtech/Cratis/pull/392)

## Summary

> Warning: This version is not marked as a **major release**, but a minor. However there is a breaking API
> change which normally would lead us to do a **major release**. Since the API change was for non-production code, but for the automated tests/specs, we decided for this release to keep it a **minor**. The change in mind is regarding the use of `context.Projection.GetById(...)` which now returns an `AdapterProjectionResult<T>` instead. Within this result object one will find a `Model` property that is the same as what the method previously returned. Also for test/specs that work the `ProjectionSpecificationFor<T>` is affected by this were it will return a `ProjectionResult<T>` and similiarily a `Model` property on it. Sorry for violating SemVer for this.


It is now possible to map properties on a model to properties from the event context for projections:

```csharp
public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
    builder
        .From<DebitAccountOpened>(_ => _
            .Set(model => model.LastUpdated).ToEventContextProperty(context => context.Occurred));
```

With the new test/specification assertions, one can now assert for specific event matching a predicate given. It also supports specifying number of times the event should appear with the match in the collection of events. There are specific `.Should...()` extension methods that works with the different contexts (`AdapterSpecificationContext`, `ProjectionSpecificationContext`). The specification of `Times` is reusing the construct from Moq for this. This means now that the `Aksio.Cratis.Specifications` package has Moq as a dependency. For the specification contexts mentioned, the methods are called `.ShouldAppend...()` or `.ShouldNotAppend...()`. While if you're working directly on the enumerable of appended events, its called `.ShouldContain...()` and `.ShouldNotContain...()`.

Example use:

```csharp
public class and_there_are_no_matching_event : given.no_events
{
    Exception result;

    void Establish()
    {
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, new MyEvent(43, "something")));
        events.Add(new AppendedEventForSpecifications(null!, null!, null!, new MyOtherEvent(43, "something")));
    }

    void Because() => result = Catch.Exception(() => events.ShouldContainEvent<MyEvent>((ev, _) => ev.SomeInteger == 42, Exactly(2)));

    [Fact] void should_assert_that_the_event_should_contain() => result.ShouldBeOfExactType<TrueException>();
}
```

Integration adapters can now leverage a new method called `.WithPropertiesBecomingNull()`. This enables one to create a filter for specifically appending events only under the condition of specific properties becoming null.

```csharp
public override void DefineImport(IImportBuilderFor<AccountHolder, KontoEier> builder)
{
    builder
        .WithPropertiesBecomingNull(_ => ...)
        .AppendEvent(_ => new SomeEvent());
}
```

Its also now possible to specify conditional filters for when a model already exists (has events projected to form state) or not.

```csharp
public override void DefineImport(IImportBuilderFor<AccountHolder, KontoEier> builder)
{
    builder
        .WhenModelExists()  // or .WhenModelDoesNotExist()
        .WithProperties(_ => ...)
        .AppendEvent(_ => new SomeEvent());
}
```

Similarily one can also do this for specific properties being set or not set on the projected model:


```csharp
public override void DefineImport(IImportBuilderFor<AccountHolder, KontoEier> builder)
{
    builder
        .WhenModelPropertiesAreSet(_ => _.SomeProperty, _ => _.SomeOtherProperty)  // or .WhenModelPropertiesAreNotSet(...)
        .WithProperties(_ => ...)
        .AppendEvent(_ => new SomeEvent());
}
```






### Added

- Support for mapping values from  event context in projections. (#376)
- Adding more `.Should..()`extensions for asserting against event log / event outbox and enumerable of appended events. (#324)
- Support for filtering for integration adapters for properties becoming null. (#373)
- Adding conditionals for integration adapters for whether or not there is an instance of a model (events exists and gets projected). (#381)
- Adding conditionals for integration adapters for whether or not properties are set or not during model projection. (#381)
- Adding an Autofac `IRegistrationSource` that can late bind `IMongoCollection<>` as a delegate within the correct execution context. This is in addition to the default convention built generically to automatically hook up based on a predefined convention of constructor parameter taking `IMongoCollection<>`. The default convention is fine for the simplest scenarios, while for wrapped scenarios such as `ProviderFor<>` it doesn't work. (#380)


# [v6.5.6] - 2022-6-27 [PR: #383](https://github.com/aksio-insurtech/Cratis/pull/383)

# Fixes

- Resolved deadlock situations with the MongoDB client library and its async handling. (#118)
- Formalized the concept of the warm up event we need for the internal Orleans pipeline to be hooked up and ready. (#269)
- Introduces an event sequence cache that keeps the latest 500 events (hardcoded for now) in memory. The cache works as a sliding window based on the sequence number.
- Fixes event sequence cache cursors so that they don't skip events.
- Fixes observers to avoid duplicates.

# [v6.5.5] - 2022-6-22 [PR: #386](https://github.com/aksio-insurtech/Cratis/pull/386)

### Fixed

- FIxing Aksio Microservice template to build the specs projects. Missing XUnit reference and was using project reference instead of package reference for the specifications extensions.


# [v6.5.4] - 2022-6-15 [PR: #379](https://github.com/aksio-insurtech/Cratis/pull/379)

### Fixed

- Fixes a problem where we got into deadlock when getting events when using `MoveNextAsync()` on MongoDB cursors. The consequence was that you only got the first 100 events of a selection. Fixes #272.


# [v6.5.3] - 2022-6-14 [PR: #377](https://github.com/aksio-insurtech/Cratis/pull/377)

### Fixed

- `JsonComplianceManager` now looks at properties from all inherited schemas / types.


# [v6.5.2] - 2022-6-14 [PR: #375](https://github.com/aksio-insurtech/Cratis/pull/375)

### Fixed

- Changing to Bank microservice having the `Guid.Empty()` identifier. Since this is the default identifier if none is specified, the Kernel will work for all that has this not set. In a future version we will don't care about this type of configuration and let the connecting client tell which Microservice it is.


# [v6.5.1] - 2022-6-14 [PR: #374](https://github.com/aksio-insurtech/Cratis/pull/374)

### Fixed

- Kernel didn't start in docker due to it trying to explicitly load assemblies starting with `runtimepack`.


# [v6.5.0] - 2022-6-13 [PR: #371](https://github.com/aksio-insurtech/Cratis/pull/371)

### Added

- When using event types that are marked public in integration adapters, it will now append these events to both the event log and the outbox.


# [v6.4.16] - 2022-6-13 [PR: #368](https://github.com/aksio-insurtech/Cratis/pull/368)

### Fixed

- Fixes a problem where the proxy generator didn't include relative path information for imports when the path was a parent path.


# [v6.4.13] - 2022-6-13 [PR: #367](https://github.com/aksio-insurtech/Cratis/pull/367)

### Fixed

- Fixes a problem where the proxy generator didn't include relative path information for imports when the path was a parent path.


# [v6.4.12] - 2022-6-13 [PR: #366](https://github.com/aksio-insurtech/Cratis/pull/366)

### Fixed

- Fixed a regression on our specifications/test extensions that was caused by not serializing using lower camel case, which we use internally in the projection engine consistently.


# [v6.4.11] - 2022-6-13 [PR: #365](https://github.com/aksio-insurtech/Cratis/pull/365)

### Fixed

- Fixed a regression on our specifications/test extensions that was caused by not serializing using lower camel case, which we use internally in the projection engine consistently.

# [v6.4.10] - 2022-6-12 [PR: #364](https://github.com/aksio-insurtech/Cratis/pull/364)

### Added

- Automatically discovers all referenced assemblies and maps out types. Types now has a static method on it to ignore specific assemblies by their name prefix (`AddAssemblyPrefixesToExclude()`). By default it adds `Microsoft` and `System` to known prefixes to ignore. From the application model, this is now set up in the `.UseAksio()` to ignore known 3rd parties.

### Changed

- Taking out the retention policy for inbox and outbox. This does not affect any behavior, but allows for future microservices getting history.

### Fixed

- Support for projecting to complex types by referring to the root property without having to list all child properties recursively. Fixes #344.



# [v6.4.9] - 2022-6-9 [PR: #350](https://github.com/aksio-insurtech/Cratis/pull/350)

### Fixed

- Fixing an exception when appending events saying "Execution context not set", which was caused by the dependency `JsonComplianceManager` changed in scope and setup with new registrations.


# [v6.4.7] - 2022-6-7 [PR: #346](https://github.com/aksio-insurtech/Cratis/pull/346)

### Fixed

- The Kernel REST API now honors the EventSequenceId in the `FindFor` query.
- Event sequence view in Workbench now refreshes the event types when selecting a microservice.
- Showing correct type for Inbox observer type in observer view.


# [v6.4.6] - 2022-6-7 [PR: #345](https://github.com/aksio-insurtech/Cratis/pull/345)

### Fixed

- Fixing broken backwards compatibility that was introduced with `ModelKey`. It now supports implicit conversion from `Guid` as `EventSourceId` does which it replaced in some APIs. They are now interchangable.
- FIxing Banking sample so that it builds - wrong directory references for props file.
- Upgraded to latest Autofac (6.4.0)
- Fixing SingletonLifetimeScope to have itself as root and then the container root as parent.
- Fixing lifetime for MongoDB services, these were all singleton, but implementations assuming singleton per microservice and/or tenant.
- Explicit population of schema store for each microservice, rather than lazy. This fixes a problem when using MongoDB async from within Orleans stream which seems to not quite work.
- Merging boot procedures to get control over order of initialization.
- Going through all lifecycles of services that are multi microservice and/or tenant, making them use `ProviderFor<>` when needed to be in the correct context.
- Explicit rehydration of projections to be in the correct context.


# [v6.4.5] - 2022-6-6 [PR: #340](https://github.com/aksio-insurtech/Cratis/pull/340)

### Fixed

- Add missing information about the projects that is in the sample and gets created from the template.


# [v6.4.2] - 2022-6-6 [PR: #337](https://github.com/aksio-insurtech/Cratis/pull/337)

### Fixed

- When using static cluster (cluster.json) the address for the silo was limited to IP address only. This is very inconvenient when using things like docker compose on a local dev environment. It now allows both IP or hostname and will resolve to the correct IP address.


# [v6.4.1] - 2022-6-6 [PR: #336](https://github.com/aksio-insurtech/Cratis/pull/336)

### Fixed

- Fixing `tsconfig.json` in the template so that it actually compiles with the new requirements of `ESNext`.


# [v6.4.0] - 2022-6-2 [PR: #328](https://github.com/aksio-insurtech/Cratis/pull/330)
## Summary

This version concludes the requirements for the [v6.4.0 milestone](https://github.com/aksio-insurtech/Cratis/milestone/7?closed=1).
Focused on bringing in formalized support for the concept of an **outbox** and an **inbox** and connecting these together in a good way.

For more details on how to use it, read more about it in the [outbox tutorial](./Documentation/tutorials/outbox/index.md).

### Added

- Support for appending events to outbox. Client now has `IEventOutbox`.
- Added `IsPublic` to event type, which is used to constrain what is allowed to append to the outbox.
- Support for declaratively describing how to append events to outbox based on private events.

### Fixed

- Resume of observers didn't actually resume in some circumstances. Made sure they do so.
- Known type missing for C# `bool` to JS boolean` for the proxy generator.
- Fixed issues with wrong microservice being used during set up of observers and event sequences.


# [v6.3.0] - 2022-6-2 [PR: #328](https://github.com/aksio-insurtech/Cratis/pull/328)

## Summary

This PR concludes the requirements for the [v6.3.0 milestone](https://github.com/aksio-insurtech/Cratis/milestone/9?closed=1). Primarily focused on bringing the capability of running rules that are concurrently getting state based on the event store and leveraging projections to do so. The rules is a form of assertion or validation of the input coming in.

For more details on how to use the new rules, read the tutorial in the documentation [here](./Documentation/tutorials/rules/index.md).


### Added

- Support for creating rules for commands (Models) using a fluent API built on top of FluentValidation.
- Support for creating rules for individual values built on top of ASP.NET `ValidationAttribute` infrastructure.
- Adding the Orleans dashboard, available at port **8081**, e.g. http://localhost:8081.

### Changed

- Controller actions won't be called if state is invalid. Invalid state means you don't want to carry on.
- `CommandResult` is now formalized with all the properties representing validation results, authorization, exceptions. This is also reflected in the frontend representation.



# [v6.2.1] - 2022-5-27 [PR: #322](https://github.com/aksio-insurtech/Cratis/pull/322)

### Fixed

- `ConceptFactory` now supports conversion from a string representation to `DateTimeOffset`. .NET does not support type conversion of this when using `Convert.ChangeType()`.
- Updated [documentation for concepts](./Documentation/fundamentals/concepts.md) regarding them representing a single value.


# [v6.2.0] - 2022-5-26 [PR: #320](https://github.com/aksio-insurtech/Cratis/pull/320)

## Summary

This release adds support for immediate projections. Read the [tutorial](./Documentation/tutorials/immediate-projections/index.md) for more details on how it works.

### Added

- Projections now support `.Count()`, which can be used to count the occurrences of an event.
- Formalized immediate projections. (#215)


# [v6.1.13] - 2022-5-25 [PR: #318](https://github.com/aksio-insurtech/Cratis/pull/318)

### Fixed

- Upgrading 3rd party NPM package dependencies
- Switching to `esnext` for modules in `tsconfig`
- Fixing so that we show event type names in event sequence list (#304).



# [v6.1.12] - 2022-5-25 [PR: #316](https://github.com/aksio-insurtech/Cratis/pull/316)

### Added

- Adding **netcat**, **ping** and **nano** to othe development Docker image for Cratis. This is helpful when helping on debug situations either locally, or more typically in the cloud where we can't install new packages.

### Fixed

- Fixing `tsconfig.json` for the sample and Web template to include the correct `lib` array to support `WeakRef`.


# [v6.1.11] - 2022-5-25 [PR: #315](https://github.com/aksio-insurtech/Cratis/pull/315)

### Fixed

- `CommandTracker` was not included properly in the NPM package, this is now fixed.

# [v6.1.10] - 2022-5-23 [PR: #313](https://github.com/aksio-insurtech/Cratis/pull/313)

### Fixed

- Production image in 6.1.9 is broken due to missing `.deps.json` files. These are now braught back and `cratis.json` which was the file we intended to remove is now explicitly removed from the image.


# [v6.1.9] - 2022-5-23 [PR: #312](https://github.com/aksio-insurtech/Cratis/pull/312)

### Fixed

- Removing `cratis.json` from production image. With the configuration basically combining providers / files and their configuration, we want a clean one for production.



# [v6.1.8] - 2022-5-23 [PR: #311](https://github.com/aksio-insurtech/Cratis/pull/311)

### Fixed

- Making the current hostname a fallback if not `AdvertisedIP` or `SiloHostName` is configured for the cluster config.


# [v6.1.7] - 2022-5-23 [PR: #309](https://github.com/aksio-insurtech/Cratis/pull/309)

### Fixed

- Fixing a problem that occurred when execution context was not set when getting projected model instances. Switching to `ProviderFor<>` for this. We will fix this completely with #265.


# [v6.1.6] - 2022-5-23 [PR: #308](https://github.com/aksio-insurtech/Cratis/pull/308)

### Fixed

- When none of the configuration providers can provide values, we now skip. This then makes sure that we don't bind default objects that can't been bound to any configuration data. Concretely fixes a crash we have for clients right now.


# [v6.1.5] - 2022-5-23 [PR: #307](https://github.com/aksio-insurtech/Cratis/pull/307)

### Fixed

- Making configuration value resolvers work recursively, now that one can have config objects within config objects.
- Switching back to using `Bind()` on config objects rather than `Get()` as the configuration value resolvers need to be run before we apply the config on an instance, and not after as we had it, which caused it to fail.


# [v6.1.2] - 2022-5-19 [PR: #300](https://github.com/aksio-insurtech/Cratis/pull/300)

### Added

- Introducing `AppendedEvents` on `IHaveEventLog` implemented by `AdapterSpecificationContext<,>` and `ProjectionSpecificationContext<>`, making it easier to extend on `Should*` extensions for asserting on appended events.
- `AppendedEvents` on `IHaveEventLog` exposes a new type `AppendedEventForSpecifications` that contains both a JSON representation and the original object.

### Changed

- For asserting if that events are not added during import one should now use `ShouldNotAppendEventsDuringImport()`.

### Fixed

- More flexibility around how to write custom assertions. `IHaveEventLog` is an interface implemented by `AdpaterSpecificationContext<,>` and `ProjectionSpecificationContext<>` holding the actual appended events. (Fixes #297)
- Moved assertions into general extension methods that can be used for anything implementing `IHaveEventLog`.

# [v6.1.0] - 2022-5-5 [PR: #275](https://github.com/aksio-insurtech/Cratis/pull/275)

### Added

- Support for writing specifications for adapters and projections (#230). For documentation on how, go [here](./Documentation/specifications/index.md).
- Brotli compression configuration set to smallest size


# [v6.0.1] - 2022-5-4 [PR: #281](https://github.com/aksio-insurtech/Cratis/pull/281)

### Added

- Crude kernel API docs

### Fixed

- Swagger endpoint working, navigate to `/swagger` on the Kernel (e.g. http://localhost:8080/swagger)


# [v6.0.0] - 2022-4-24 [PR: #190](https://github.com/aksio-insurtech/Cratis/pull/190)

## Summary

This release is a major release with breaking changes. Primarily, the API surfaces has not been touched, but all of configuration has changed. In addition, underlying data model has also changed. There is no migration script as of yet, since we are assuming at this point that we (Aksio) are the only ones using our own tech and we will not invest in doing migrations for non-production workloads ATM.

### Added

- Support for child configuration objects that are automatically registered with services.
- Base type `GrainSpecification` for writing specifications for grains. This solution does not require setting up a Orleans in-process TestCluster, as it mocks out the underlying grain. One caveat, as it is relying on privates and internals; if that changes, all specs relying on this gets broken. But we will know right away :) and can therefor fix it. For the time being we want to maintain this as it gives us more lightweight specs, no need for xUnit fixtures. With the recommended Orleans approach it becomes messy real fast with grains that takes dependencies having to effectively configure dependencies for the service collections for the test cluster.
- Added `ValidFrom` for events in Append APIs and storage. This is a "for the future" thing and is defaulted to `DateTimeOffset.MinValue` if not specified. The purpose of it will be to be able to append events that occur at a time but can be filtered out in projections or observers if a condition requires it. (#244)
- Observers now have a friendly name persisted along the state. From a client observer, this defaults to the fully qualified name of the type.
- Observers have a type associated with them. For now the types are `Client` or `Projection`.
- `EventContext` now has a new property called `ObservationState`. This can be used to know whether or not the observer is seeing the event for the first time or if it is a replay of the event. It also holds information on if it is the head or tail event of a replay.

### Changed

- Renaming `ProjectionResultStore` as concept to be `ProjectionSink` - consistent rename in code, log messages and exceptions. (#89)
- Renaming from `EventLog`to `EventSequence` for the abstract representation. Keeping `EventLog` as concept and the the default sequence typically used. From client, there is no change. (#129)
- Changing to file scoped namespaces for the codebase and templates. (#161)
- For consistency, changing the key representation of event types in the schema store.
- Observers failed partitions have now moved into the observer state, meaning that the MongoDB collection for `failed-observers` are gone. Logic is also now consolidated into `Observer` grain.

### Fixed

- Observers in client SDK is made tenant unaware and Kernel hooks up all Tenants automatically. (#109)
- Observers will automatically rewind if definition (event types) changes. (#236)
- Upgraded all 3rd party dependencies to the latest. (#148)
- Routes in generated proxies are not escaped. Previously it would URL encode it, which lead to "&amp;" for the *&* for multiple query string arguments.
- Observers will catch up on start up, Orleans didn't automatically play subscribers before there was a new event in the stream. The observers are now producing a dummy event on subscription to work around this problem.

### Removed

- The concept of ProjectionEventProvider is removed. As per now, we only want to provide events from our own event store.


# [v5.14.1] - 2022-4-7 [PR: #243](https://github.com/aksio-insurtech/Cratis/pull/243)

### Fixed

- Problems with local development inside Cratis and possibly anything using the WebPack setup. (#234)


# [v5.14.0] - 2022-3-30 [PR: #220](https://github.com/aksio-insurtech/Cratis/pull/220)

### Added

- Introduced a way to consume commands using React hooks - this is now then consistent with how we do queries.
- Support for tracking changes to a command.
- Adding a `CommandTracker` that can be used as an aggregation of commands to see if there are changes or not to any commands within it.


# [v5.13.2] - 2022-3-15 [PR: #216](https://github.com/aksio-insurtech/Cratis/pull/216)

### Fixed

- Nullable annotation was in the wrong place for the proxy generator.


# [v5.13.1] - 2022-3-15 [PR: #213](https://github.com/aksio-insurtech/Cratis/pull/213)

### Fixed

- Proxy generator now returns the correct underlying type for nullables and not `Nullable`.


# [v5.13.0] - 2022-3-15 [PR: #212](https://github.com/aksio-insurtech/Cratis/pull/212)

### Added

- TS ProxyGenerator support for decimal -> number (#211)
- TS ProxyGenerator support for nullables (#210)


# [v5.12.0] - 2022-3-12 [PR: #209](https://github.com/aksio-insurtech/Cratis/pull/209)

### Added

- Added Elastic search reference, since we're using this ourselves as default supported log sink.
- Lighting up the EventLog workbench to be able to see events in the event store.


# [v5.11.2] - 2022-3-8 [PR: #206](https://github.com/aksio-insurtech/Cratis/pull/206)

### Fixed

- Fixing order of precedence for config files (#201).
- Fixing `GetModelInstanceById()`of the Projection Grain to be in line with how the `ProjectionPipelineHandler`handles events. This rises technical debt for later; #205.

# [v5.11.1] - 2022-3-2 [PR: #200](https://github.com/aksio-insurtech/Cratis/pull/200)

### Fixed

- Fixed type support in proxy generation. It was using `string` for C# types `DateTime`and `DateTimeOffset`, now it uses `Date`.


# [v5.11.0] - 2022-2-27 [PR: #197](https://github.com/aksio-insurtech/Cratis/pull/197)

### Added

- Configuration of cluster options supporting local, Azure Storage and ADO.NET for now. All done through a new `cluster.json` file. If the file is missing, localhost clustering is assumed for development purposes.
- Adding a way to resolve configuration values that differ in type / implementation. See the docs for more details.


# [v5.10.0] - 2022-2-24 [PR: #194](https://github.com/aksio-insurtech/Cratis/pull/194)

### Added

- Added support for easily ignore property naming conventions through using `[IgnoreConventions]` attribute.
- Added ability to override model name for read models with the `[ModelName]` attribute. This is honored by the MongoDB collection name hookup and the Projection definitions. (rel. to. #165)



# [v5.9.0] - 2022-2-23 [PR: #193](https://github.com/aksio-insurtech/Cratis/pull/193)

### Added

- Supporting discovery and hookup of bindings for `IMongoCollection<ReadModel>` based on constructors that takes `ProviderFor<IMongoCollection<ReadModel>>`. Delivering parts of whats described in #191.


# [v5.8.11] - 2022-2-19 [PR: #186](https://github.com/aksio-insurtech/Cratis/pull/186)

### Fixed

- Fixes so that children supports being updated at any level.
- Internal code clean up and refactoring, making it more maintainable.
- Consistently working with the `PropertyPath` and `ArrayIndexer` internally.
- Fixing `ExpandoObject` cloning to clone enumerables and every element in it.


# [v5.8.10] - 2022-2-17 [PR: #185](https://github.com/aksio-insurtech/Cratis/pull/185)

### Fixed

- Child relationships are now using PropertyPath for nested objects in the Client definition, not just the name of the last property in the chain.


# [v5.8.9] - 2022-2-17 [PR: #184](https://github.com/aksio-insurtech/Cratis/pull/184)

### Fixed

- Switching to using `ActualTypeSchema` from NJsonSchema for recursive nested schemas. When properties are considered nullable, NJsonSchema will create a "one-of" with Null and and the actual type. `ActualSchema` does not resolve to the actual type schema.


# [v5.8.8] - 2022-2-17 [PR: #183](https://github.com/aksio-insurtech/Cratis/pull/183)

### Fixed

- Client was using the last name of the property when setting up properties for projection causing nested structures in both read models and events to be wrong. This is now fixed and it creates a fully qualified `PropertyPath`.


# [v5.8.7] - 2022-2-17 [PR: #182](https://github.com/aksio-insurtech/Cratis/pull/182)

### Fixed

- Fixing a bug when recursively applying compliance for events for complex structures. NJsonSchema has properties that are prefixed **Actual** which point to resolved versions for type references.


# [v5.8.6] - 2022-2-16 [PR: #179](https://github.com/aksio-insurtech/Cratis/pull/179)

### Fixed

- Removing serialization of MongoDB commands and failures for logging.


# [v5.8.5] - 2022-2-16 [PR: #177](https://github.com/aksio-insurtech/Cratis/pull/177)

### Fixed

- Aggressive logging for telling which read models are bound for the IoC has now been moved to be only during setup.


# [v5.8.2] - 2022-2-16 [PR: #173](https://github.com/aksio-insurtech/Cratis/pull/173)

### Fixed

- Further build optimizations.
- Fixing NuGet packaging to reuse what is in Aksio.Defaults.


# [v5.8.1] - 2022-2-16 [PR: #172](https://github.com/aksio-insurtech/Cratis/pull/172)

### Fixed

- Publish GitHub Actions workflow was very slow due to the multiple architecture build of the development image. This is now improved by doing the build of the binaries outside of Docker and then just copying these into the image. (#115).


# [v5.8.0] - 2022-2-16 [PR: #171](https://github.com/aksio-insurtech/Cratis/pull/171)

### Added

- All HTTP & HTTPS requests will now have their responses compressed supporting GZip and Brotli, depending on `Accept-Encoding`.


# [v5.7.3] - 2022-2-15 [PR: #169](https://github.com/aksio-insurtech/Cratis/pull/169)

### Added

- Logging for when MongoClient is created.

### Fixed

- Load the first `storage.json` and break, not continue to the next - creating a second binding.


# [v5.7.2] - 2022-2-15 [PR: #168](https://github.com/aksio-insurtech/Cratis/pull/168)

### Added

- Logging of unhandled exceptions
- Added Serilog.Exceptions and enriching all logs with this to get deeper information about all exceptions.


# [v5.7.1] - 2022-2-15 [PR: #167](https://github.com/aksio-insurtech/Cratis/pull/167)

### Added

- Logging for MongoDB related config and usage. You can see all MongoDB commands being issued when using Trace for the Aksio namespace prefix.


# [v5.7.0] - 2022-2-15 [PR: #166](https://github.com/aksio-insurtech/Cratis/pull/166)

### Added

- Look for **appsettings.json** in a **config** sub folder, this will help us configure everything when using Azure Container Instances due to its lack of not supporting indivdual file mounts.
- Configured Serilog to self-log, very useful for errors.
- Kernel now has Serilogs Elastic extension added to it.
- Added sample of how to use Serilogs Elastic for production for the Bank sample, which means also for the default template when creating a new Microservice.


# [v5.6.1] - 2022-2-14 [PR: #164](https://github.com/aksio-insurtech/Cratis/pull/164)

### Fixed

- Removing the exception that was thrown if configuration was missing. That turned out to be a behavioral change we don't want right now.


# [v5.6.0] - 2022-2-14 [PR: #162](https://github.com/aksio-insurtech/Cratis/pull/162)

### Added

- Adds search paths for config files. By default it adds `$PWD/config` and automatically also `$PWD` as fallback. This is very helpful when packaged and leverages with services like Azure Container Instances which can't mount files, only folders.


# [v5.5.0] - 2022-2-13 [PR: #159](https://github.com/aksio-insurtech/Cratis/pull/159)

### Changes

- Removing `ObjectsComparer` 3rd party dependency.

### Added

- [Integration] Support for complex structures with nested objects.
- [Integration] When you want to react on a change within a complex structure, you don't have to specify all the properties within the structure. Instead you just simply specify the top level property in the `.WithProperties()` method.

### Fixed

- Recursive objects comparison producing a correct change difference hierarchy. Making it possible to do complex object comparison for things like integration.


# [v5.4.1] - 2022-2-9 [PR: #156](https://github.com/aksio-insurtech/Cratis/pull/156)

### Fixed

- Proxy types for workbench generated with new proxy generator.


# [v5.4.0] - 2022-2-9 [PR: #155](https://github.com/aksio-insurtech/Cratis/pull/155)

### Added

- Supporting plain old ASP.NET API controllers for Commands - meaning that an object encapsulating the command is not required but optional. All parameters will now be added as properties on the generated command. (#154)

### Fixed

- Fixing file names generated when there are route parameters on controllers.
- Keeping a hash in memory of generated files - it prevents writing to disk unless the content has changed. Very useful with regards to the dotnet build server running and constantly running the generator.


# [v5.3.0] - 2022-2-7 [PR: #152](https://github.com/aksio-insurtech/Cratis/pull/152)

### Added

- Full support for recursive children in the Kernel.

### Fixed

- More consistency in the codebase internally with formalization of Key, ArrayIndexers, PropertyPath with formalized segments and specific types (PropertyName, ArrayProperty).
- PropertyPath is much more robust consistent.
- Added more specs


# [v5.2.2] - 2022-2-7 [PR: #151](https://github.com/aksio-insurtech/Cratis/pull/151)

### Fixed

- Fixing so that `PropertyDifference` supports concepts - used for now in the integration parts.


# [v5.2.1] - 2022-2-2 [PR: #145](https://github.com/aksio-insurtech/Cratis/pull/145)

### Fixed

- Fixing children model type in expression when building children within children.


# [v5.2.0] - 2022-2-2 [PR: #144](https://github.com/aksio-insurtech/Cratis/pull/144)

### Added

- Adding support for Children of Children in projection declarations. The Kernel is prepared for this. 🤞🏻 that this is working as expected.


# [v5.1.1] - 2022-2-1 [PR: #143](https://github.com/aksio-insurtech/Cratis/pull/143)

### Fixed

- Minor API documentation errors.
- ExecutionContext not being set for observers in client. It is now hard-coded to Development, as everything else.

# [v5.1.0] - 2022-1-28 [PR: #136](https://github.com/aksio-insurtech/Cratis/pull/136)

## Summary

Adds support for defining a `RemovedWith<TEvent>()` on the projection builder. This is implemented all the way down and will cause a document in e.g. MongoDB to actually be deleted.

A sample of usage:

```csharp
public class DebitAccountProjection : IProjectionFor<DebitAccount>
{
    public ProjectionId Identifier => "d1bb5522-5512-42ce-938a-d176536bb01d";

    public void Define(IProjectionBuilderFor<DebitAccount> builder) =>
        builder
            .From<DebitAccountOpened>(_ => _
                .Set(model => model.Name).To(@event => @event.Name)
                .Set(model => model.Owner).To(@event => @event.Owner))
            .From<DepositToDebitAccountPerformed>(_ => _
                .Add(model => model.Balance).With(@event => @event.Amount))
            .From<WithdrawalFromDebitAccountPerformed>(_ => _
                .Subtract(model => model.Balance).With(@event => @event.Amount))
            // ** NEW **
            .RemovedWith<DebitAccountClosed>();
}
```

# [v5.0.6] - 2022-1-26 [PR: #126](https://github.com/aksio-insurtech/Cratis/pull/126)

### Fixed

- Added `@aksio/cratis-typescript` as package for the template post script for updating references.


# [v5.0.5] - 2022-1-26 [PR: #125](https://github.com/aksio-insurtech/Cratis/pull/125)

### Fixed

- Fixing 3rd party dependency versions of Web template.
- Fixing template creation post script to update the correct NPM packages.


# [v5.0.4] - 2022-1-26 [PR: #124](https://github.com/aksio-insurtech/Cratis/pull/124)

### Fixed

- Improved startup time for clients by lazily configuring things as needed. Due to our hosted service for setting up projections and other things, Orleans connections get configured in paralell as a consequence to this.
- Fixing microservice template to have the correct NuGet references.
- Adding MongoDB port to the `docker-compose.yml` for the Sample and also the microservice template
- Adding logging to startup of clients - so we're not kept in the dark on whats going on.


# [v5.0.3] - 2022-1-25 [PR: #123](https://github.com/aksio-insurtech/Cratis/pull/123)

### Fixed

- Fixing Docker builds to not include the Aksio.Defaults - which doesn't produce a DLL when publishing with **PublishReadyToRun**.
- Fixing image name in `docker-compose.yml` for sample and template.
- Fixing path to sample for the template generator


# [v5.0.2] - 2022-1-25 [PR: #122](https://github.com/aksio-insurtech/Cratis/pull/122)

### Fixed

- Fixed the path to `entrypoint.sh` in the Dockerfile for our MongoDB image.


# [v5.0.1] - 2022-1-25 [PR: #121](https://github.com/aksio-insurtech/Cratis/pull/121)

### Fixed

- Add missing `ApplicationModel/Frontend` for the Workbench Docker image
- Locking down Newtonsoft JSON version + System.IO.FileSystem.Primitives. NJsonSchema has a dependency to an earlier version of Newtonsoft, causing problems during publish with downgraded references.
- Add **latest** tag for Workbench docker image.


# [v5.0.0] - 2022-1-25 [PR: #120](https://github.com/aksio-insurtech/Cratis/pull/120)

## Summary

This release is a consolidation of repositories and projects. It has involved simplification internally with less code and less repetition. The kernel startup code is now dogfooding the appliction model itself, making the setup simpler and more maintainable.

There are no behavioral changes.

### Changed

- All namespaces are changed. They all start now with `Aksio.Cratis`.
- All NuGet packages have new names. They all start now with `Aksio.Cratis`.
- All NPM pakcages have new names. They all start now with `@aksio/cratis`.
- Docker images changed names; aksioinsurtech/cratis:<label>
- No longer dependending on Newtonsoft.Json - consistently using System.Text.Json both in Client and Kernel
- Change how Projections get their identity. Since we have a an interface one needs to implement, this is now there - no `[Projection("<guid")]` attribute.
- `AdapterFor<>` now requires an identity, a property that needs to be implemented. This is used internally for things like the projection, seeing that it is an observer.

### Fixed

- Improved startup predictability for Kernel - taking out "WarmUp" for event logs
- Fixed error situations for observable clients - closing down more gracefully
- Graceful handling of projections when replacing these - unsubscribe previous stream subscriber
- Improved consistency in codebase for working with events by switching to `JsonObject` throughout rather than JSON strings.
- Integration now works together with the Kernel rather than all in-memory. Leveraging a new Projection Grain.

# [v4.3.1] - 2021-11-16

### Fixed

- `PropertyDifference` in the Changes engine supports Concepts. This was later also merged down to version **2.13.6**.

# [v4.3.0] - 2022-1-19 [PR: #116](https://github.com/aksio-insurtech/Cratis/pull/116)

### Added

- Adding a simple inmemory cache that can sit in front of any other `EncrpyptionKeyStore`. Configured for the MongoDB one in kernel right now.

### Fixed

- Hopefully fixing #112 - needs verification since its not consistently happening.


# [v4.2.1] - 2022-1-19 [PR: #114](https://github.com/aksio-insurtech/Cratis/pull/114)

## Summary

Improved consistent behavior for child relationships.
Events that represent something that is related to the same event source id as another event can now be modelled in a projection as a child.

Take the following:

```csharp
public void Define(IProjectionBuilderFor<Person> builder) => builder
    .From<PersonRegistered>(_ => _
        .Set(m => m.SocialSecurityNumber).To(e => e.SocialSecurityNumber)
        .Set(m => m.FirstName).To(e => e.FirstName)
        .Set(m => m.LastName).To(e => e.LastName))
    .Children(_ => _.PersonalInformation, c => c
        .IdentifiedBy(_ => _.Identifier)
        .From<PersonalInformationRegistered>(_ => _
            .UsingKey(_ => _.Identifier)
            .Set\(m => m.Type).To(e => e.Type)
            .Set\(m => m.Value).To(e => e.Value)));
```

With the child definition, it will identify the child uniquely based on the `IdentifiedBy` statement. This tells the engine to use the property `Identifier` on the target read model / document to identify it.
Within the child definition from statements can now specify a property on the event to be used as the key. The statement `UsingKey()` accomplishes this. This will then direct the projection engine to use this property to identify the child object and map this with the `IdentifiedBy()` property on the read model / document.

The point of this all is to be able to share the event source id - being the aggregate identifier for all things related. While the child identifier is the value identifier.


### Fixed

- When `ParentKey()` is not specified in a child relationship, one can use the `UsingKey()` to specify a property on the child to be the key that identifies the child.



# [v4.2.0] - 2022-1-18 [PR: #111](https://github.com/aksio-insurtech/Cratis/pull/111)

## Summary

This version is focused on Compliance and specifically GDPR; providing the necessary handling (apply) of events being appended and then for observers and projections (release).

### Added

- Compliance applied to events being appended.
- Appended events being released from compliance when rehydrated into observers and projections.

### Changed

- Encryption keys are stored asymmetrically. This is transparent in the API, but internal behavioral change.

### Fixed

- Kernel mode observers weren't hooked up. We will need to revisit this, see #109.


# [v4.1.0] - 2022-1-17 [PR: #110](https://github.com/aksio-insurtech/Cratis/pull/110)

## Summary

This version is focused on Compliance and specifically GDPR; providing the necessary handling (apply) of events being appended and then for observers and projections (release).

### Added

- Compliance applied to events being appended.
- Appended events being released from compliance when rehydrated into observers and projections.

### Changed

- Encryption keys are stored asymmetrically. This is transparent in the API, but internal behavioral change.

### Fixed

- Kernel mode observers weren't hooked up. We will need to revisit this, see #109.


# [v4.0.4] - 2022-1-14 [PR: #99](https://github.com/aksio-insurtech/Cratis/pull/99)

### Fixed

- Fixing version info passed to the development Docker image build.


# [v4.0.3] - 2022-1-14 [PR: #98](https://github.com/aksio-insurtech/Cratis/pull/98)

### Fixed

- Going back to publishing all .NET Projects for now - to get all dependencies published.


# [v4.0.2] - 2022-1-14 [PR: #97](https://github.com/aksio-insurtech/Cratis/pull/97)

### Fixed

- Adding missing Global.d.ts file for declaring modules for CSS and SCSS files.
- Making the development & production images dependent on the specific version of the workbench.


# [v4.0.1] - 2022-1-14 [PR: #96](https://github.com/aksio-insurtech/Cratis/pull/96)

### Fixed

- Fixing the publishin pipeline - missing working directory for the NuGet publish step.


# [v4.0.0] - 2022-1-14 [PR: #95](https://github.com/aksio-insurtech/Cratis/pull/95)

## Summary

This is primarily a maintenance and cleanup of code release. The reason for the major bump in version is the change in behavior as to where the Compliance workbench is now located and how it is merged with Events to provide a holistic Workbench.

### Changed

- Merged Compliance and Events workbench for a single workbench frontend.
- Consolidated APIs - cleaning up duplicates.
- Compliance frontfacing APIs are now inside the Kernel.



# [v3.4.0] - 2022-1-11 [PR: #90](https://github.com/aksio-insurtech/Cratis/pull/90)

### Added

- Adding MongoDB to the docker image being created. Later this will be multiple tags for different purposes (Development / Production..). We'll also likely split it up into multiple servers (API, Workbench, etc..).
- Started on the basics of the Compliance workbench and domain with events.
- Added a common dialog for FluentUI that makes it easy to pop up modal dialogs that share the same look & feel. This feature is also unobtrusive, meaning that you don't have to have the dialog in the markup.
- Introducing a `ScrollableDetailsList` with the starting support for paging.

### Fixed

- Removing duplicates of concept definitions - leading to simpler code.


# [v3.3.2] - 2022-1-11 [PR: #87](https://github.com/aksio-insurtech/Cratis/pull/87)

### Fixed

- Setting Yarn timeout in Dockerfile to avoid getting **ESOCKETTIMEDOUT** during Yarn install (as suggested [here](https://github.com/yarnpkg/yarn/issues/8242#issuecomment-661881292). This is till we've upgraded to Yarn 2.0 that should have this fixed.


# [v3.1.0] - 2022-1-11 [PR: #86](https://github.com/aksio-insurtech/Cratis/pull/86)

### Fixed

- Docker image creation.


# [v3.0.1] - 2022-1-11 [PR: #85](https://github.com/aksio-insurtech/Cratis/pull/85)

### Fixed

- GitHub Actions failed for publishing due to `package.json` files being altered during build for correct versions. Added a `git reset` task before to the `docker build`.


# [v3.0.0] - 2022-1-11 [PR: #84](https://github.com/aksio-insurtech/Cratis/pull/84)

## Summary

The primary focus of this PR is to bring in support for observers. While doing so, some major changes had to be done internally. There is also a shift in mindset and design during the process of doing this. Such as dropping GRPC in favor of just using the Orleans cluster client directly. This is fine for now as our primary focus is on C#/.NET support.

### Added

- Full partitioned observer support with support for failures with reminders of retry.
- Distributed event log leveraging the streaming capabilities of Orleans.
- C# Type converter support for concepts - useful when using concepts as parameters on API actions.
- Started work on compensations.


### Changed

- EventLog API changed to `Append` from `Commit`.

### Fixed

- Describe the fix and the bug

### Removed

- All GRPC contracts and implementations.



# [v2.17.0] - 2021-12-22 [PR: #76](https://github.com/aksio-insurtech/Cratis/pull/76)

## Summary

This release focuses on compliance and dealing with it for JSON objects.

### Added

- Encryption service for symmetric key generation, encryption and decryption - using AES-256.
- InMemory Encryption Key Store
- MongoDB Ecryption Key Store
- Composite Encryption Key Store that enables creating a chain of resonsibilities for stores.
- JSON compliance manager that works with schemas and applies compliance value handlers according to metadata in the schema.
- PII value handler that will encrypt and decrypt values leveraging the encryption and encryption key store.

# [v2.16.1] - 2021-12-20 [PR: #75](https://github.com/aksio-insurtech/Cratis/pull/75)

### Fixed

- Event Type workbench working again - it was not referenced in the output.
- Generating correct type info for concept types. Showing the underlying type.


# [v2.16.0] - 2021-12-20 [PR: #74](https://github.com/aksio-insurtech/Cratis/pull/74)

## Summary

This release focuses on compliance and schemas. Providing a mechanism for adding metadata on types and/or properties for compliance information, specifically GDPR and PII right now.

### Added

- Fundamentals system for capturing necessary compliance metadata related to types.
- Schema support for compliance extensions.


### Changed

- Changing from Newtonsoft JSON Schema generator - it has a limit of 10 generations per hour on its free license model.
- Moving Schema system, management and APIs to Kernel - its not a Dolittle extension.


# [v2.15.1] - 2021-11-26 [PR: #69](https://github.com/aksio-insurtech/Cratis/pull/69)

### Added

- Suspended state for a projection. Typically if a projection fails for some reason.

### Fixed

- Filter on event types per projection on the subjects created in the pipeline.
- Error handling when providing events.


# [v2.15.0] - 2021-11-26 [PR: #68](https://github.com/aksio-insurtech/Cratis/pull/68)

### Added

- Workbench will now reflect changes to projections in "real-time" - basically observing what is happening with projections running.

# [v2.13.6] & [v2.13.7] - 2021-11-16

### Fixed

- `PropertyDifference` in the Changes engine supports Concepts.

# [v2.13.5] - 2021-11-16 [PR: #66](https://github.com/aksio-insurtech/Cratis/pull/66)

### Fixed

- Extracting out the interface for the `JsonProjectionSerializer` - enabling mocking in specs internally and externally for anything leveraging custom pipelines.


# [v2.13.4] - 2021-11-15 [PR: #65](https://github.com/aksio-insurtech/Cratis/pull/65)

### Fixed

- Fixing so that we save the correct offset for the projection - it needs to be the next event offset it will start processing from.

# [v2.13.3] - 2021-11-12 [PR: #64](https://github.com/aksio-insurtech/Cratis/pull/64)

### Fixed

- For the Dolittle EventStream implementation we had a **greater than** on the query for getting from a specific offset rather than **greater than or equal**. Meaning we wouldn't get the first event - ever.

# [v2.13.2] - 2021-11-12 [PR: #63](https://github.com/aksio-insurtech/Cratis/pull/63)

### Added

- Added trace log messages from the Dolittle EventStore implementation.

# [v2.13.1] - 2021-11-11 [PR: #62](https://github.com/aksio-insurtech/Cratis/pull/62)

### Fixed

- Throw `TypeIsMissingEventType` if trying to get `EventTypeId` from a type not adorned with `[EventType]`.

# [v2.13.0] - 2021-11-11 [PR: #61](https://github.com/aksio-insurtech/Cratis/pull/61)

### Added

- Formalized `IEventStore` and `IEventStream` for the Dolittle extension - making it possible to work with these in other scenarios other than the internal projection event provider.

# [v2.12.2] - 2021-11-10 [PR: #60](https://github.com/aksio-insurtech/Cratis/pull/60)

### Fixed

- Opening up Changeset and changes to be for other types than just ExpandoObject.  The abstract concept of changes can now be more widely applied.

# [v2.12.1] - 2021-11-10 [PR: #59](https://github.com/aksio-insurtech/Cratis/pull/59)

### Fixed

- Upgrading to version 11 of the Dolittle SDK - it had some breaking changes that won't affect the exterior from Cratis.

# [v2.12.0] - 2021-11-8 [PR: #57](https://github.com/aksio-insurtech/Cratis/pull/57)

### Added

- Introducing a Changes API in Fundamentals for representing changes on any object instance. This is a formalization of what was very Event specific within the Projection engine.

# [v2.11.0] - 2021-11-8 [PR: #56](https://github.com/aksio-insurtech/Cratis/pull/56)

## Summary

This PR brings in the ability to have children on models based on events.
When using the C# client API you'll find it as this:

```csharp
[Projection("8fdaaf0d-1291-47b7-b661-2eeba340a520")]
public class AccountsOverviewProjection : IProjectionFor<AccountsOverview>
{
    public void Define(IProjectionBuilderFor<AccountsOverview> builder)
    {
        builder
            .Children(_ => _.DebitAccounts, _ => _
                .IdentifiedBy(_ => _.Id)
                .From<DebitAccountOpened>(_ => _
                    .UsingParentKey(_ => _.Owner)
                    .Set(_ => _.Name).To(_ => _.Name)));
    }
}
```

You model the relationship by telling what is the property on the model that identifies every child, this is to be able to do the correct operation (Add, Remove, Update) on a child. The Event needs to have a property on it that identifies the parent object; the key. As a child relationship is considered a child projection, you have the same capabilities on a child as on a parent.

### Added

- Support for children on objects in projections.

### Fixed

- Internal restructuring for extensibility and improved maintainability across the board.

### Fixed

- Projections are now waiting per OnNext() to finish - guaranteeing order of operations.


# [v2.9.1] - 2021-10-14 [PR: #41](https://github.com/aksio-insurtech/Cratis/pull/41)

### Fixed

- Failsafe for the MongoDB serializer of ConceptAs<Guid> - if the Guid is stored as string, we'll try and parse it as a string representation of the Guid.


# [v2.9.0] - 2021-10-14 [PR: #40](https://github.com/aksio-insurtech/Cratis/pull/40)

### Added

- Making the projection API a bit more flexible with regards to building set operations.
- One can now map properties to the event source id.

# [v2.8.3] - 2021-10-14 [PR: #39](https://github.com/aksio-insurtech/Cratis/pull/39)

### Fixed

- NPM packages now have the correct **main**, **module** and **typings** reference set.
- Fixing **tsconfig.json** files to have the correct **outDir**, making the typescript compiler output to the correct path structure within **dist** folder of the packages.

# [v2.8.2] - 2021-10-14 [PR: #38](https://github.com/aksio-insurtech/Cratis/pull/38)

### Fixed

- Making naming convention consistent for MongoDB collections referenced by type. We need to improve this even more, as described in #37.



# [v2.8.1] - 2021-10-14 [PR: #36](https://github.com/aksio-insurtech/Cratis/pull/36)

### Fixed

- [TS] Removing internal complexity that is not needed.


# [v2.8.0] - 2021-10-13 [PR: #34](https://github.com/aksio-insurtech/Cratis/pull/34)

### Added

- Kernel projection engine supporting From statements and simple property mapping.
- .NET Client support for describing projections.
- JSON support for describing projections.
- MongoDB support for materializing projections into documents.
- InMemory support for materializing projections (completely black box for now - content not accessible).
- Dolittle support for providing events from Dolittles MongoDB event-log .


# [v2.7.0] - 2021-10-4 [PR: #32](https://github.com/aksio-insurtech/Cratis/pull/32)

### Added

- [C#] `ITypes` now have a property called `ProjectReferencedAssemblies` - enabling you to see all project referenced assemblies discovered.

# [v2.6.4] - 2021-10-4 [PR: #30](https://github.com/aksio-insurtech/Cratis/pull/30)

Nothing

# [v2.6.3] - 2021-10-4 [PR: #28](https://github.com/aksio-insurtech/Cratis/pull/28)

### Fixed

- Making sure all package.json files for packages that should be public has a publish profile of such.

# [v2.6.2] - 2021-10-4 [PR: #27](https://github.com/aksio-insurtech/Cratis/pull/27)

### Fixed

- Remove `"private":false`  from all package.json files. This broke NPM publish claiming the packages were private.

# [v2.6.1] - 2021-10-4 [PR: #26](https://github.com/aksio-insurtech/Cratis/pull/26)

### Fixed

- Fixing JS/TS publish pipeline to actually publish.

# [v2.6.0] - 2021-10-4 [PR: #25](https://github.com/aksio-insurtech/Cratis/pull/25)

### Added

- CI + Publishing pipelines for NPM based packages.
- Introducing NPM package @aksio/webpack
- Introducing NPM package @aksio/fundamentals


# [v2.5.0] - 2021-10-1 [PR: #23](https://github.com/aksio-insurtech/Cratis/pull/23)

### Added

- Added builder for workbench for setting assembly that holds the API.
- Added extension method for the Dolittle Workbench API configuration.



# [v2.4.4] - 2021-10-1 [PR: #22](https://github.com/aksio-insurtech/Cratis/pull/22)

### Fixed

- Fixing a bug where Bson serializer for Guid already exists when we get to setting up how we want it setup. Turns out that the MongoDB driver will configure a bunch of serializers default if you create a MongoClient, if you then do a registration of a serializer after that - its too late. This fix makes sure we do it before.


# [v2.4.3] - 2021-10-1 [PR: #21](https://github.com/aksio-insurtech/Cratis/pull/21)

### Fixed

- Completely mixed up Use* and Add* for configuration APIs. Fixing this for Dolittle + Workbench setup.


# [v2.4.2] - 2021-10-1 [PR: #20](https://github.com/aksio-insurtech/Cratis/pull/20)

### Fixed

- Fixing build that creates the embedded version of the Events Workbench after restructuring. This got broken in 2.4.0.

# [v2.4.1] - 2021-10-1 [PR: #18](https://github.com/aksio-insurtech/Cratis/pull/18)

### Fixed

- Fixing ServiceCollection extension APIs from version 2.4.0. Instead of `Use*`for the methods, its now `Add*`. Should've been major version, but since this mistake was done 20 minutes ago - no one has consumed it yet :).



# [v2.4.0] - 2021-10-1 [PR: #17](https://github.com/aksio-insurtech/Cratis/pull/17)

### Added

- Configuration methods for Dolittle Schema Store

# [v2.3.2] - 2021-10-1 [PR: #16](https://github.com/aksio-insurtech/Cratis/pull/16)

### Fixed

- Separating out Workbench as its own package - decoupling from the Cratis Kernel, enabling using it with only 3rd parties like Dolittle without all the dependencies from Cratis.


# [v2.3.1] - 2021-10-1 [PR: #15](https://github.com/aksio-insurtech/Cratis/pull/15)

### Fixed

- Fixing so that we actually initialize the assemblies prefix filter property before using it.


# [v2.3.0] - 2021-10-1 [PR: #14](https://github.com/aksio-insurtech/Cratis/pull/14)

### Added

- One can now include more assemblies for type discovery in addition to the project referenced assemblies.


# [v2.2.0] - 2021-10-1 [PR: #13](https://github.com/aksio-insurtech/Cratis/pull/13)

### Added

- Dolittle extensions for APIs used by Workbench
- Event Workbench supporting event types with schemas

### Changed

- Consolidated projects into one Fundamentals project - no need to separate these out, improves build times and general happiness :)

# [v2.1.6] - 2021-9-27 [PR: #11](https://github.com/aksio-insurtech/Cratis/pull/11)

### Fixed

- Index.html for Workbench now has the correct `/events` prefix for all file references.

# [v2.1.5] - 2021-9-27 [PR: #9](https://github.com/aksio-insurtech/Cratis/pull/9)

### Fixed

- Disabling workbench build + copying - hoping embedding will finally work.


# [v2.1.4] - 2021-9-27 [PR: #7](https://github.com/aksio-insurtech/Cratis/pull/7)

### Fixed

- Getting the Workbench actually embedded (Famous last words).


# [v2.1.3] - 2021-9-27 [PR: #6](https://github.com/aksio-insurtech/Cratis/pull/6)

### Fixed

- Fixing embedding of the workbench SPA files to be embedded after it has been copied. This broke on Linux builds.

# [v2.1.2] - 2021-9-27 [PR: #5](https://github.com/aksio-insurtech/Cratis/pull/5)

### Fixed

- Type discovery for Cratis internals.
- Deep linking fixed - SPA middleware.
- Embedded resources relative path inclusion.

# [v2.1.1] - 2021-9-27 [PR: #4](https://github.com/aksio-insurtech/Cratis/pull/4)

### Fixed

- Added missing service collection extensions for Cratis Workbench



# [v2.1.0] - 2021-9-27 [PR: #3](https://github.com/aksio-insurtech/Cratis/pull/3)

Added:

- Start of the Cratis workbench for events.

# [v2.0.0] - 2021-9-15 [PR: #2](https://github.com/aksio-insurtech/Cratis/pull/2)

Initial release of v2.
