# [v2.8.3] - 2021-10-14 [PR: #39](https://github.com/Cratis/cratis/pull/39)

### Fixed

- NPM packages now have the correct **main**, **module** and **typings** reference set.
- Fixing **tsconfig.json** files to have the correct **outDir**, making the typescript compiler output to the correct path structure within **dist** folder of the packages.

# [v2.8.2] - 2021-10-14 [PR: #38](https://github.com/Cratis/cratis/pull/38)

### Fixed

- Making naming convention consistent for MongoDB collections referenced by type. We need to improve this even more, as described in #37.



# [v2.8.1] - 2021-10-14 [PR: #36](https://github.com/Cratis/cratis/pull/36)

### Fixed

- [TS] Removing internal complexity that is not needed.


# [v2.8.0] - 2021-10-13 [PR: #34](https://github.com/Cratis/cratis/pull/34)

### Added

- Kernel projection engine supporting From statements and simple property mapping.
- .NET Client support for describing projections.
- JSON support for describing projections.
- MongoDB support for materializing projections into documents.
- InMemory support for materializing projections (completely black box for now - content not accessible).
- Dolittle support for providing events from Dolittles MongoDB event-log .


# [v2.7.0] - 2021-10-4 [PR: #32](https://github.com/Cratis/cratis/pull/32)

### Added

- [C#] `ITypes` now have a property called `ProjectReferencedAssemblies` - enabling you to see all project referenced assemblies discovered.

# [v2.6.4] - 2021-10-4 [PR: #30](https://github.com/Cratis/cratis/pull/30)

Nothing

# [v2.6.3] - 2021-10-4 [PR: #28](https://github.com/Cratis/cratis/pull/28)

### Fixed

- Making sure all package.json files for packages that should be public has a publish profile of such.

# [v2.6.2] - 2021-10-4 [PR: #27](https://github.com/Cratis/cratis/pull/27)

### Fixed

- Remove `"private":false`  from all package.json files. This broke NPM publish claiming the packages were private.

# [v2.6.1] - 2021-10-4 [PR: #26](https://github.com/Cratis/cratis/pull/26)

### Fixed

- Fixing JS/TS publish pipeline to actually publish.

# [v2.6.0] - 2021-10-4 [PR: #25](https://github.com/Cratis/cratis/pull/25)

### Added

- CI + Publishing pipelines for NPM based packages.
- Introducing NPM package @cratis/webpack
- Introducing NPM package @cratis/fundamentals


# [v2.5.0] - 2021-10-1 [PR: #23](https://github.com/Cratis/cratis/pull/23)

### Added

- Added builder for workbench for setting assembly that holds the API.
- Added extension method for the Dolittle Workbench API configuration.



# [v2.4.4] - 2021-10-1 [PR: #22](https://github.com/Cratis/cratis/pull/22)

### Fixed

- Fixing a bug where Bson serializer for Guid already exists when we get to setting up how we want it setup. Turns out that the MongoDB driver will configure a bunch of serializers default if you create a MongoClient, if you then do a registration of a serializer after that - its too late. This fix makes sure we do it before.


# [v2.4.3] - 2021-10-1 [PR: #21](https://github.com/Cratis/cratis/pull/21)

### Fixed

- Completely mixed up Use* and Add* for configuration APIs. Fixing this for Dolittle + Workbench setup.


# [v2.4.2] - 2021-10-1 [PR: #20](https://github.com/Cratis/cratis/pull/20)

### Fixed

- Fixing build that creates the embedded version of the Events Workbench after restructuring. This got broken in 2.4.0.

# [v2.4.1] - 2021-10-1 [PR: #18](https://github.com/Cratis/cratis/pull/18)

### Fixed

- Fixing ServiceCollection extension APIs from version 2.4.0. Instead of `Use*`for the methods, its now `Add*`. Should've been major version, but since this mistake was done 20 minutes ago - no one has consumed it yet :).



# [v2.4.0] - 2021-10-1 [PR: #17](https://github.com/Cratis/cratis/pull/17)

### Added

- Configuration methods for Dolittle Schema Store

# [v2.3.2] - 2021-10-1 [PR: #16](https://github.com/Cratis/cratis/pull/16)

### Fixed

- Separating out Workbench as its own package - decoupling from the Cratis Kernel, enabling using it with only 3rd parties like Dolittle without all the dependencies from Cratis.


# [v2.3.1] - 2021-10-1 [PR: #15](https://github.com/Cratis/cratis/pull/15)

### Fixed

- Fixing so that we actually initialize the assemblies prefix filter property before using it.


# [v2.3.0] - 2021-10-1 [PR: #14](https://github.com/Cratis/cratis/pull/14)

### Added

- One can now include more assemblies for type discovery in addition to the project referenced assemblies.


# [v2.2.0] - 2021-10-1 [PR: #13](https://github.com/Cratis/cratis/pull/13)

### Added

- Dolittle extensions for APIs used by Workbench
- Event Workbench supporting event types with schemas

### Changed

- Consolidated projects into one Fundamentals project - no need to separate these out, improves build times and general happiness :)

# [v2.1.6] - 2021-9-27 [PR: #11](https://github.com/Cratis/cratis/pull/11)

### Fixed

- Index.html for Workbench now has the correct `/events` prefix for all file references.

# [v2.1.5] - 2021-9-27 [PR: #9](https://github.com/Cratis/cratis/pull/9)

### Fixed

- Disabling workbench build + copying - hoping embedding will finally work.


# [v2.1.4] - 2021-9-27 [PR: #7](https://github.com/Cratis/cratis/pull/7)

### Fixed

- Getting the Workbench actually embedded (Famous last words).


# [v2.1.3] - 2021-9-27 [PR: #6](https://github.com/Cratis/cratis/pull/6)

### Fixed

- Fixing embedding of the workbench SPA files to be embedded after it has been copied. This broke on Linux builds.

# [v2.1.2] - 2021-9-27 [PR: #5](https://github.com/Cratis/cratis/pull/5)

### Fixed

- Type discovery for Cratis internals.
- Deep linking fixed - SPA middleware.
- Embedded resources relative path inclusion.

# [v2.1.1] - 2021-9-27 [PR: #4](https://github.com/Cratis/cratis/pull/4)

### Fixed

- Added missing service collection extensions for Cratis Workbench



# [v2.1.0] - 2021-9-27 [PR: #3](https://github.com/Cratis/cratis/pull/3)

Added:

- Start of the Cratis workbench for events.

# [v2.0.0] - 2021-9-15 [PR: #2](https://github.com/Cratis/cratis/pull/2)

Initial release of v2.
