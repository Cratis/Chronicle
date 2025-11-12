# Internalization

Clients must present a single, clear, and idiomatic API surface. Any dependencies required to implement this API should remain hidden from consumers.
This prevents confusion caused by exposing internal concepts or types that may exist in multiple namespaces or packages for different purposes.
When tools like IDEs auto-import namespaces, exposing these internals can lead to ambiguous references and a degraded developer experience.

Furthermore, some APIs are not intended for public consumption or long-term support.
Exposing these would require us to maintain and version them alongside the official client APIs, which is undesirable.

Typical examples of APIs we internalize include [contracts](../kernel/contracts.md) and any Kernel APIs bundled with the full .NET InProcess client.

## Repacking assemblies

To prevent exposing APIs that are not intended for public use, we repack our assemblies using [ILRepack](https://github.com/gluck/il-repack).
This tool merges multiple assemblies into a single output, allowing us to internalize types and members that should remain hidden from consumers.
By making these internals inaccessible, we ensure a clean and focused API surface for client developers.

Repacking introduces some challenges, such as managing dependencies and ensuring that no internalized types are accidentally exposed.
To address these, we've developed supporting tools that automate the process, verify the integrity of the merged assemblies, and catch any violations—such
as accidentally leaking internals—early in the build pipeline. For more details on this verification, see [Internals Verifier](#internals-verifier).

The following flow happens when doing a repack build:

```mermaid
flowchart LR
    Build --> ILRepack --> AssemblyFixer --> InternalsVerifier
```

The repacking workflow is defined in a `.targets` file named `ILRepack.targets` within the `Source/Clients` directory.
To automate and integrate ILRepack into our build process, we use the [ILRepack.Lib.MSBuild.Task](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task) MSBuild wrapper.
This package is referenced directly in any project that requires assembly repacking, ensuring a seamless and repeatable build experience.

```xml
<PackageReference Include="ILRepack.Lib.MSBuild.Task">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

Next, ensure your project includes a reference to the `ILRepack.targets` file within a `<PropertyGroup>`.
This tells MSBuild where to find the repacking workflow configuration:

```xml
<PropertyGroup>
    <ILRepackTargetsFile>$(MSBuildThisFileDirectory)../ILRepack.targets</ILRepackTargetsFile>
</PropertyGroup>
```

The projects that a repacked project depends on that it wants to have automatically merged in are added as `<ProjectReference>`
with a slight twist. By marking it as a **private asset**, the `ILRepack.targets` file will pick it up as something
to merge in.

Example:

```xml
<ProjectReference Include="../Connections/Connections.csproj">
    <PrivateAssets>all</PrivateAssets>
</ProjectReference>
```

## Grpc Client Factory

To efficiently reuse the core connection logic while internalizing implementation details, we take a specialized approach
to generating and consuming gRPC clients.

When assemblies are repacked, types such as `ChronicleConnection` from the `Connections` project become internal, making it
impossible to instantiate gRPC clients at runtime using standard mechanisms. To address this, we generate the required client
types at build time and merge them into the .NET client assembly.

This process involves two key steps:

- **Build-time client generation:** The `GrpcClients` project uses `System.Reflection.Emit` and gRPC tooling to generate a dedicated assembly containing all required client implementations. These types are pre-generated and ready for use, eliminating the need for runtime code generation.
- **Custom ClientFactory:** We override the default `ClientFactory` to discover and instantiate these pre-generated client types. Instead of attempting to create new clients dynamically (which would fail due to internalized types), the factory locates and uses the merged implementations.

The resulting workflow for the .NET client is as follows:

```mermaid
flowchart LR
    GrpcClients --> Build --> ILRepack --> AssemblyFixer --> InternalsVerifier
```

By generating clients ahead of time and customizing the factory, we ensure that all necessary gRPC clients are available and
discoverable, even after internalization. This approach maintains a clean public API, leverages shared connection logic,
and avoids runtime errors related to inaccessible types.

## Internal assemblies

You can explicitly specify which assemblies should be merged and internalized by using the `<InternalAssembly>` item,
which is recognized by the `ILRepack.targets` file.

For example, to merge the generated contract clients assembly and ensure its types are internalized, add the following to your project file:

```xml
<InternalAssembly Include="$(OutputPath)$(ContractsImplementationAssembly).dll" />
```

## Excluding assemblies from internalization

In some scenarios, you may want to merge an assembly into the output without internalizing its types—preserving their original accessibility.
For example, the `Connections` project is merged but remains public so its types are accessible to consumers.

To achieve this, use the `<DoNotInternalize>` item in your project file to specify assemblies that should be merged as-is:

```xml
<DoNotInternalize Include="Cratis.Chronicle.Connections.dll" />
```

The `ILRepack.targets` file recognizes this item and will configure the `ILRepack` tool correctly.

### Repack property

Repacking is triggered only when the `Repack` property is explicitly set to `true` during the build process.
This allows you to control when internalization occurs, ensuring that regular development builds remain fast and developer-friendly.
To enable repacking, pass the property as a parameter when invoking the build:

```shell
dotnet build -p:Repack=true
```

### Projects depending on repacked assemblies

When a project depends on a repacked assembly and requires access to its internalized types—such as the `AspNetCore` project does,
we must explicitly grant visibility to those internals. This is achieved by adding an `<InternalsVisibleTo>` entry for the consuming assembly.

For example, in the `DotNET.csproj` file, you will find an `<ItemGroup>` containing one or more such entries:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="Cratis.Chronicle.AspNetCore" />
</ItemGroup>
```

Another challenge for these projects is that a standard project reference cannot be used during repacking, since the compiler must bind to the correct,
repacked assembly for runtime correctness. However, during regular development, repacking is unnecessary and would slow down the feedback loop.

To support both scenarios efficiently, the `.csproj` files for these projects require a custom approach.

First, we need to explicitly control the build order to ensure dependencies are built in the correct sequence.
This means we cannot rely on the default `.csproj` pattern, which typically starts with a project definition like:

```xml
<Project Sdk="Microsoft.NET.Sdk"> <!-- ... or Microsoft.NET.Sdk.Web for ASP.NET Core -->
```

Instead of specifying `Sdk="..."` at the top of the `.csproj` file, we explicitly import the appropriate SDK props and targets files at the correct locations.
This approach gives us full control over the build process and allows us to customize the build order and dependencies as needed.

The following configuration ensures that the `DotNET.csproj` dependency is built before the current project, but only when the `Repack` property is set to `true`.
This guarantees that the repacked assembly is available for reference during the build, while preserving the standard development workflow when repacking is not required.

```xml
<Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk.Web" />

<Target Name="BuildDependencies">
    <Copy SourceFiles="../DotNET/bin/$(Configuration)/$(TargetFramework)/Cratis.Chronicle.dll" DestinationFolder="$(OutDir)"/>
    <Copy SourceFiles="../DotNET/bin/$(Configuration)/$(TargetFramework)/Cratis.Chronicle.pdb" DestinationFolder="$(OutDir)" Condition="Exists('../DotNET/bin/$(Configuration)/$(TargetFramework)/Cratis.Chronicle.pdb')"/>
</Target>

<Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk.Web" />

<PropertyGroup Condition="'$(Repack)' == 'true'">
    <BuildDependsOn>BuildDependencies;$(BuildDependsOn)</BuildDependsOn>
</PropertyGroup>

<ItemGroup Condition="'$(Repack)' == 'true'">
    <ProjectReference Include="../DotNET/DotNET.csproj" ReferenceOutputAssembly="false"/>
    <Reference Include="$(OutDir)/Cratis.Chronicle.dll"/>
</ItemGroup>

<ItemGroup Condition="'$(Repack)' != 'true'">
    <ProjectReference Include="../DotNET/DotNET.csproj" />
    <ProjectReference Include="../Connections/Connections.csproj" />
</ItemGroup>
```

> Note: When repacking, the `<ProjectReference/>` to a repacked project, like the `DotNET.csproj` will have to not reference the output assembly directly, which will
> be the assembly before it has been repackaged. This is due to build optimization and parallelism. In conjunction with the `<Copy/>` statement we get the correct
> assembly that it will create correct bindings to. Also notice that we include the `.pdb` file when copying, to get a correct reference.

This configuration ensures that, when repacking is enabled, your project references the merged output assembly directly—guaranteeing runtime correctness and hiding internal APIs.
During normal development (when repacking is not enabled), it falls back to standard project references, preserving fast incremental builds and IDE tooling support.
This dual approach provides both a clean public API for consumers and a smooth developer experience for contributors.

### Building deterministically

By default, the `dotnet` CLI and `msbuild` optimize for speed by running build tasks in parallel. However, when repacking assemblies, this parallelism can introduce race conditions
and intermittent build failures due to the complex dependencies and sequencing required. To ensure a reliable and deterministic build when repacking, we explicitly disable
parallelism by setting `-maxcpucount:1` on the build command. This forces the build to run tasks sequentially, guaranteeing that all dependencies are processed in the correct order
and preventing issues related to concurrent execution.

```shell
dotnet build -p:Repack=true -maxcpucount:1
```

This approach ensures that all necessary artifacts are built in the correct order and are available before proceeding to subsequent steps in the pipeline. In our GitHub Actions workflows,
we typically perform release builds, while pull request builds target a single .NET framework for efficiency. For example:

```shell
dotnet build -f net10.0 --configuration Release -p:Repack=true -maxcpucount:1
```

## Assembly fixer

The Assembly Fixer tool, found in the `Source/Tools/AssemblyFixer` directory, ensures that merged assemblies function correctly at runtime. This is particularly important for scenarios
involving the `DotNET.InProcess` package, which relies on Microsoft Orleans for artifact discovery (such as `Grains`). Orleans uses the `AssemblyPart` attribute to identify assemblies
containing relevant artifacts. However, after merging, many of these original assemblies no longer exist, which can cause Orleans to fail during runtime.

To address this, the Assembly Fixer processes the merged output and removes all `AssemblyPart` attributes that reference assemblies no longer present. This step prevents Orleans from attempting to load missing assemblies, ensuring stable runtime behavior for the repacked client.

## Internals Verifier

When a public API inadvertently exposes a type from an assembly that should have been internalized, ILRepack cannot automatically make those types internal. This is a common source
of accidental leaks—such as a public property, method, or constructor referencing an internalized type—which can compromise the intended encapsulation.

To prevent this, we use the `InternalsVerifier` as a post-build tool. It scans the merged assembly for any public members that reference types originating from assemblies marked for
internalization. If such a violation is detected, the build fails immediately, ensuring that no internal implementation details are exposed to consumers.

Typical violations include public properties or constructors that use internalized types, which are easy to overlook during development. The `InternalsVerifier` enforces this boundary
automatically, maintaining a clean and intentional API surface.
