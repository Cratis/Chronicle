# Proxy Generator Setup — Reference

The proxy generator runs as an MSBuild task during `dotnet build` and produces TypeScript interfaces and classes for every command and query it finds.

---

## Install the NuGet package

Add to every `.csproj` that contains controllers, commands, or queries:

```xml
<PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="*" />
```

## Configure the output path

```xml
<PropertyGroup>
  <!-- Path is relative to the .csproj file -->
  <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../Web/src/api</CratisProxiesOutputPath>
</PropertyGroup>
```

## Install the frontend package

```bash
npm install @cratis/arc
```

---

## Run the generator

```bash
dotnet build
```

The generator will write TypeScript files under the configured output path, mirroring your C# namespace hierarchy as folders:

```
Web/src/api/
  Accounts/
    OpenDebitAccount.ts     ← POST action → command proxy
    AllAccounts.ts          ← GET action  → query proxy
    index.ts
  index.ts
```

---

## Common gotchas

| Problem | Fix |
| ------- | --- |
| No files generated | Ensure the package is referenced and the project builds cleanly first |
| Wrong folder structure | The folder mirrors the **namespace**, not the file path — adjust your namespace |
| Stale proxies | Run `dotnet clean && dotnet build` to force full regeneration |
| Proxies mixed with hand-written files | Set `<CratisProxiesSkipOutputDeletion>true</CratisProxiesSkipOutputDeletion>` to prevent the generator deleting everything; or move proxies to a dedicated folder |

---

## Multi-project solutions

```
MyApp.API.csproj:
  <ProjectReference Include="../MyApp.Domain/MyApp.Domain.csproj" />
  <PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="*" />
  <CratisProxiesOutputPath>../MyApp.Web/src/api</CratisProxiesOutputPath>
```

Only the project with controllers needs the proxy generator package. Domain and read-model projects do not.
