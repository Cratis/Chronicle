# API

The REST API client is exposed in the Kernel when packaged as a Docker container, hidden behind a feature flag
that is default set to true. Its main use case is for the Workbench to work.

In addition to this, the Workbench can also be embedded into a client project, it will also then embed the API.

## Internal constructors

When we repack assemblies we do an [internalization](./internalization.md) of types from assemblies
being merged in. Some of these types, such as gRPC service contracts are typically things we would like to use
as dependencies on constructors of the controllers.

The problem with this is that we can't have public constructors with dependencies to these types that
will become internal during build of a repacked assembly.

To make this work, the constructors are internal as a consequence.

## Custom Controller Activation

The default activation of controllers is not capable of resolving controllers with internal constructors.
In the `API` client project you'll therefor find a type called `CustomControllerActivator` that is capable of
resolving these.
