# Internalization

Clients must present a single, clear, and idiomatic API surface. Any dependencies required to implement this API should remain hidden from consumers.
This prevents confusion caused by exposing internal concepts or types that may exist in multiple namespaces or packages for different purposes.
When tools like IDEs auto-import namespaces, exposing these internals can lead to ambiguous references and a degraded developer experience.

Furthermore, some APIs are not intended for public consumption or long-term support.
Exposing these would require us to maintain and version them alongside the official client APIs, which is undesirable.

Typical examples of APIs we internalize include [contracts](../kernel/contracts.md) and any Kernel APIs bundled with the full .NET InProcess client.

## Runtime-only packaging

To prevent exposing APIs that are not intended for public use, we package our assemblies using a runtime-only strategy.
This approach separates compile-time dependencies (what developers see in IntelliSense) from runtime dependencies (what's available at execution time).

The core idea is to:
- **Package SDK assemblies under `lib/`** as compile-time references
- **Ship Server, Contracts, Services, and other kernel assemblies as runtime-only assets** in the `runtimes/` folder

This means:
- IntelliSense/autocomplete shows only SDK types
- `using Cratis.Chronicle.Projections;` binds to SDK's IProjection interface
- Even if consumers manually type internal server type names, they won't compile (no compile-time reference to those DLLs)
- Server/contracts can contain identical namespaces and types—they simply aren't part of compilation

The following diagram illustrates the packaging structure:

```mermaid
flowchart TB
    subgraph NuGet["NuGet Package"]
        subgraph lib["lib/ (Compile-time)"]
            SDK["Cratis.Chronicle.dll<br/>(SDK API)"]
            InProcess["Cratis.Chronicle.InProcess.dll<br/>(InProcess API)"]
        end
        subgraph runtimes["runtimes/ (Runtime-only)"]
            Connections["Cratis.Chronicle.Connections.dll"]
            Contracts["Cratis.Chronicle.Contracts.dll"]
            ContractsImpl["Cratis.Chronicle.Contracts.Implementations.dll"]
            Core["Cratis.Chronicle.Core.dll"]
            Services["Cratis.Chronicle.Services.dll"]
            Storage["Cratis.Chronicle.Storage*.dll"]
            Others["Other kernel assemblies..."]
        end
    end
    
    Consumer["Consumer Application"]
    Consumer -->|"Compiles against"| lib
    Consumer -->|"Runs with"| runtimes
```

## Configuring runtime-only packaging

To configure a project for runtime-only packaging, you need to:

1. Mark dependencies as `PrivateAssets="all"` to prevent them from becoming transitive compile-time dependencies
2. Use `<None>` items with `Pack="true"` and appropriate `PackagePath` to include assemblies in the `runtimes/` folder of the NuGet package

### Example: DotNET.csproj

The `DotNET.csproj` packages the SDK assembly (Cratis.Chronicle.dll) for compile-time use while hiding Connections and Contracts as runtime-only:

```xml
<ItemGroup>
    <!-- Mark these as private so they don't become transitive compile-time dependencies -->
    <ProjectReference Include="../Connections/Connections.csproj">
        <PrivateAssets>all</PrivateAssets>
    </ProjectReference>
    <ProjectReference Include="../../Kernel/Contracts/Contracts.csproj">
        <PrivateAssets>all</PrivateAssets>
    </ProjectReference>
</ItemGroup>

<!-- Package runtime-only assemblies in runtimes folder -->
<ItemGroup>
    <None Include="$(OutputPath)Cratis.Chronicle.Connections.dll" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" />
    <None Include="$(OutputPath)Cratis.Chronicle.Connections.pdb" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" Condition="Exists('$(OutputPath)Cratis.Chronicle.Connections.pdb')" />
    <None Include="$(OutputPath)Cratis.Chronicle.Contracts.dll" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" />
    <None Include="$(OutputPath)Cratis.Chronicle.Contracts.pdb" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" Condition="Exists('$(OutputPath)Cratis.Chronicle.Contracts.pdb')" />
    <None Include="$(OutputPath)Cratis.Chronicle.Contracts.Implementations.dll" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" />
    <None Include="$(OutputPath)Cratis.Chronicle.Contracts.Implementations.pdb" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" Condition="Exists('$(OutputPath)Cratis.Chronicle.Contracts.Implementations.pdb')" />
</ItemGroup>
```

### Example: DotNET.InProcess.csproj

The `DotNET.InProcess.csproj` packages the InProcess assembly for compile-time use while hiding all kernel assemblies as runtime-only:

```xml
<ItemGroup>
    <!-- Mark kernel assemblies as private so they're runtime-only -->
    <ProjectReference Include="../../Kernel/Storage/Storage.csproj">
        <PrivateAssets>all</PrivateAssets>
    </ProjectReference>
    <ProjectReference Include="../../Kernel/Core/Core.csproj">
        <PrivateAssets>all</PrivateAssets>
    </ProjectReference>
    <ProjectReference Include="../../Kernel/Services/Services.csproj">
        <PrivateAssets>all</PrivateAssets>
    </ProjectReference>
    <!-- Additional kernel projects... -->
</ItemGroup>

<!-- Package kernel assemblies in runtimes folder -->
<ItemGroup>
    <None Include="$(OutputPath)Cratis.Chronicle.Storage.dll" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" />
    <None Include="$(OutputPath)Cratis.Chronicle.Core.dll" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" />
    <None Include="$(OutputPath)Cratis.Chronicle.Services.dll" Pack="true" PackagePath="runtimes/$(TargetFramework)/" Visible="false" />
    <!-- Additional kernel assemblies... -->
</ItemGroup>
```

## Grpc Client Factory

To efficiently reuse the core connection logic while internalizing implementation details, we take a specialized approach
to generating and consuming gRPC clients.

When kernel assemblies are packaged as runtime-only, types such as `ChronicleConnection` from the `Connections` project are not available at compile-time, making it
impossible to reference them in generated gRPC client code. To address this, we generate the required client
types at build time and include them in the .NET client assembly.

This process involves two key steps:

- **Build-time client generation:** The `GrpcClients` project uses `System.Reflection.Emit` and gRPC tooling to generate a dedicated assembly containing all required client implementations. These types are pre-generated and ready for use, eliminating the need for runtime code generation.
- **Custom ClientFactory:** We override the default `ClientFactory` to discover and instantiate these pre-generated client types. Instead of attempting to create new clients dynamically (which would fail due to missing compile-time references), the factory locates and uses the generated implementations.

The resulting workflow for the .NET client is as follows:

```mermaid
flowchart LR
    GrpcClients["Generate gRPC Clients"] --> Build["Build SDK"] --> Package["Package with Runtime Assemblies"]
```

By generating clients ahead of time and customizing the factory, we ensure that all necessary gRPC clients are available and
discoverable, even without compile-time access to internal types. This approach maintains a clean public API, leverages shared connection logic,
and avoids runtime errors related to inaccessible types.

## Projects with access to internals

When a project depends on a packaged assembly and requires access to its internalized types—such as the `AspNetCore` project,
we must explicitly grant visibility to those internals. This is achieved by adding an `<InternalsVisibleTo>` entry for the consuming assembly.

For example, in the `DotNET.csproj` file, you will find an `<ItemGroup>` containing one or more such entries:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="Cratis.Chronicle.AspNetCore" />
</ItemGroup>
```

This configuration ensures that `AspNetCore` can access internal members of the SDK assembly, while those internals remain
hidden from all other consumers.

## Internals Verifier

When a public API inadvertently exposes a type from an assembly that should be runtime-only, consumers may attempt to use these types in their code, leading to compilation errors.
This is a common source of accidental leaks—such as a public property, method, or constructor referencing an internal type—which can compromise the intended encapsulation.

To prevent this, we use the `InternalsVerifier` as a post-build tool. It scans assemblies for any public members that reference types originating from assemblies marked for
runtime-only packaging. If such a violation is detected, the build fails immediately, ensuring that no internal implementation details are exposed to consumers.

Typical violations include public properties or constructors that use runtime-only types, which are easy to overlook during development. The `InternalsVerifier` enforces this boundary
automatically, maintaining a clean and intentional API surface.
