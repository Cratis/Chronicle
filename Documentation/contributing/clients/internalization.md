# Internalization

The clients should only expose a single idiomatic API surface, whatever dependencies it has to provide that should not be exposed.
This is to avoid confusion for anyone consuming our client packages. We have concepts represented with the same name sitting in
different namespaces and packages for different purposes, with tools automatically resolving using statements, these can easily then
be pulled in and create confusion and less optimal developer experience.

In addition to that, there are APIs we don't want to publicly support and version in the same manner as we do with the
official client APIs.

Good examples of things we want to hide are the [contracts](./contracts.md) and any of the Kernel APIs being included in the
full .NET InProcess client.

## Repacking assemblies

To be able to hide APIs we don't want to share, we repack our assemblies. This is done using a tool called [ILRepack](https://github.com/gluck/il-repack).
It makes it possible for us to take a set of assemblies and merge them into one single assembly.
As part of that process we also have to change the members of most of the assemblies to be internal. By doing that,
they won't become visible and consumable for anyone outside.

However, this has some consequences that we need to deal with. We've built tools to help us deal with issues that arise and also
build tools for helping us if we do something that causes [internals to become visible](#internals-verifier).

```mermaid
flowchart LR
    Build --> ILRepack --> AssemblyFixer --> InternalsVerifier
```

The process is captured in a `.targets` file called `ILRepack.targets` located in the `Source/Clients` folder.
We leverage a [MSBuild wrapper for ILRepack](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task) which we include
as a package reference in the projects that performs repacking.

```xml
<PackageReference Include="ILRepack.Lib.MSBuild.Task">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

The next thing the project needs to have is a reference to the `ILRepack.targets` file in a `<PropertyGroup>`.

```xml
<ILRepackTargetsFile>$(MSBuildThisFileDirectory)../ILRepack.targets</ILRepackTargetsFile>
```

### Repack property

Repacking will only occur if the `Repack` property is set to true. This is something you specify during build.

```shell
dotnet build -p:Repack=true
```

### Projects depending on repacked assemblies

When we have packages / projects that depend on a package that gets repacked and also need to know about the things
that has been internalized, like we have for the `AspNetCore` project for instance. We need to make
the internals visible to this assembly. We do this by adding an `<InternalsVisibleTo>` property with the name of
the assembly that the internals should be visible to.

For instance in the `DotNET.csproj` file, you'll find an `<ItemGroup>` with a set of these, like the following:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="Cratis.Chronicle.AspNetCore" />
</ItemGroup>
```

Another issue for these projects is that it can't have a regular project reference when we do repacking, as it
needs the compiler to bind it to the correct assembly - otherwise it won't work at runtime.
But, we don't need the repacking while developing, as that will hurt the feedback loop.

For us to be able to support both of these scenarios optimally, the `.csproj` files for these projects are
somewhat different.

To begin with, we need to control the build ourselves to build things in order.
That means we can't leverage the default `.csproj` approach of having the following project definition at the top:

```xml
<Project Sdk="Microsoft.NET.Sdk"> <!-- ... or Microsoft.NET.Sdk.Web for ASP.NET Core -->
```

The `Sdk="..."` has to be removed. Instead we import the correct Sdk props & targets files in the correct location.

With the following snippet we take control over building the dependency, in this case the `DotNET.csproj` project before
our own gets built, but only if the `Repack` property is set to true.

```xml
<Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk.Web" />

<Target Name="BuildDependencies">
    <MSBuild Projects="../DotNET/DotNET.csproj" Targets="Build" Properties="Configuration=$(Configuration);TargetFramework=$(TargetFramework);TargetFrameworks=$(TargetFrameworks);Repack=$(Repack)" />
    <Copy SourceFiles="../DotNET/bin/$(Configuration)/$(TargetFramework)/Cratis.Chronicle.dll" DestinationFolder="$(OutDir)"/>
</Target>

<Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk.Web" />

<PropertyGroup Condition="'$(Repack)' == 'true'">
    <BuildDependsOn>BuildDependencies;$(BuildDependsOn)</BuildDependsOn>
</PropertyGroup>

<ItemGroup Condition="'$(Repack)' == 'true'">
    <Reference Include="$(OutDir)/Cratis.Chronicle.dll"/>
</ItemGroup>

<ItemGroup Condition="'$(Repack)' != 'true'">
    <ProjectReference Include="../DotNET/DotNET.csproj" />
    <ProjectReference Include="../Connections/Connections.csproj" />
</ItemGroup>
```

With this block of configuration in the `.csproj` file, we bind to the correct output file during repack and leverage
a regular project reference when not doing repack.

### Building deterministically

The `dotnet` tool in conjunction with `msbuild` tries to be as fast as possible and will parallelize tasks.
This will make the build flaky and sometimes break due to the complexity. To overcome that problem we
basically have to disable parallelism when building with repacking.

```shell
dotnet build -p:Repack=true -maxcpucount:1
```

With this we get it to build everything properly and artifacts be ready before the next step.
Typically during our GitHub actions, we build for release and for pull requests we only build for one
.NET target framework like the following:

```shell
dotnet build -f net9.0 --configuration Release -p:Repack=true -maxcpucount:1
```

## Assembly fixer

The assembly fixer tool located in the `Source/Tools/AssemblyFixer` folder makes the merged assemblies work at runtime.
This is a specific problem when using the `DotNET.InProcess` package, as that sets up everything using Microsoft Orleans.
Orleans have a feature where it discovers artifacts such as `Grains` from different assemblies. Assemblies that
include a reference to the Orleans SDK will get an attribute called `AssemblyPart` added to it.
When we merge assemblies, most of these assemblies seize to exist at runtime and Orleans would crash.

With the Assembly fixer, we load the finished merged assembly and remove all of these attributes.

## Internals Verifier

If we have anything that publicly exposes something from one of the assemblies that should be internalized.
The ILRepack tool will not make those types internal. This is a violation and something that is easy to forget.
With the `InternalsVerifier` we get a post build tool that will look through types in the merged assembly
and check if these are originating from an assembly that should be internalized and then break the build
as a consequence.

This is as easy as having a property exposed with a type or a public constructor taking a dependency.
