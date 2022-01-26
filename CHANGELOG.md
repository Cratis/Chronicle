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
