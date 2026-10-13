<!-- cratis-ai-managed: skills/cratis-arc-hosting/references/integrations-and-publish.md -->
# Integrations and publish boundaries

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

Verified at **Arc v22.75.0**. These are host configuration fragments, not a claim
that any arbitrary application is NativeAOT-compatible.

## Storage and Chronicle

`AddCratisArc(configureOptions: ..., configureBuilder: ...)` separates
`ArcOptions` from the `IArcBuilder`. With the corresponding packages referenced,
the builder supports `WithChronicle`, `WithMongoDB` and `WithEntityFrameworkCore`.
`AddCratis` instead names these arguments `configureArcOptions` and
`configureArcBuilder` and composes Chronicle for you. Do not activate/register the
same stack twice.

- `WithChronicle` installs the Chronicle client integration, including tenant
  namespace resolution, command returned-event handling and read-model access.
  The server and its credentials/connection belong to deployment configuration.
- `WithMongoDB` configures Arc's MongoDB storage/query integration.
- `WithEntityFrameworkCore` registers the application's EF integration; use the
  EF migration skill for the actual context/provider/migration setup.

Source signatures under `Source/DotNET`:
[Cratis composition](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Cratis/WebApplicationBuilderExtensions.cs),
[ASP.NET builder](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Arc/WebApplicationBuilderExtensions.cs),
[Core builder](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Arc.Core/ArcApplicationBuilderExtensions.cs),
[Chronicle integration](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Chronicle/ArcBuilderExtensions.cs),
[MongoDB integration](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/MongoDB/ArcBuilderExtensions.cs).

## MVC and NativeAOT

For runtime-only removal of MVC, use this ASP.NET configuration excerpt:

```csharp
builder.AddCratisArc(configureBuilder: arc => arc.WithoutControllers());
```

Model-bound command/query endpoints still work. Controller commands/queries and
`MapControllers` do not. Runtime removal alone cannot tell the trimmer to remove
MVC. For trimming/NativeAOT set the application MSBuild feature switch:

```xml
<PropertyGroup>
  <CratisArcControllersSupport>false</CratisArcControllersSupport>
</PropertyGroup>
```

The switch also turns controllers off without `WithoutControllers()`. Arc
registers the ASP.NET services it previously got through MVC (including CORS,
endpoint API exploration and authorization). Do not infer that every provider,
serializer or dependency is trim-safe just because MVC is off.
[Controller boundary](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Arc/ControllersArcBuilderExtensions.cs).

## Application JSON metadata

`ArcOptions.AddJsonTypeInfoResolver(IJsonTypeInfoResolver)` puts application
contracts ahead of Arc's defaults. Supply your source-generated
`JsonSerializerContext.Default` during configuration, before serialization makes
options read-only. Include the read models, query arguments and responses the
application serializes, and **each** `CommandResult<TResponse>` used for typed
command responses. Arc's own context cannot enumerate your generic responses.
Reflection fallback only exists when reflection-based serialization is enabled.
[Resolver contract](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Arc.Core/ArcOptions.cs).

## Optional event-model viewer

`Cratis.Arc.Screenplay.Embedded` supplies
`services.AddCratisEventModelViewer(...)` and
`endpoints.MapCratisEventModel(params Assembly[] assemblies)` (a
`RouteGroupBuilder`, allowing explicit host authorization). It exposes
`/.cratis/event-model`. Automatic exposure defaults to an unoptimized Debug entry
assembly **and** Development; explicit `Enabled=true` overrides that default.
Do not enable the explorer in a deployed environment accidentally. Mapping it
manually needs an intentional host policy, not an assumption of automatic gating.
The Screenplay family owns generated-model content and compiler capability.
Sources: [viewer registration](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Screenplay.Embedded/Hosting/EventModelViewerServiceCollectionExtensions.cs),
[mapping](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Screenplay.Embedded/Hosting/EventModelEndpoints.cs),
[automatic exposure](https://github.com/Cratis/Arc/blob/v22.75.0/Source/DotNET/Screenplay.Embedded/Hosting/EventModelViewerExposure.cs).
