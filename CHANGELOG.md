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
