<!-- cratis-ai-managed: skills/cratis-arc-command/references/proxy-generation.md -->
# Proxy generation

Verified against `Cratis.Arc.ProxyGenerator.Build` `22.10.4`.

## Wiring

Reference the build package from the project that contains the commands,
queries and read models, and set an output path:

```xml
<PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="22.10.4" />

<PropertyGroup>
  <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../<Web>/src/api</CratisProxiesOutputPath>
</PropertyGroup>
```

Install `@cratis/arc` and `@cratis/arc.react` on the frontend side.

The package adds a `CratisProxyGenerator` target that runs `AfterTargets="AfterBuild"`,
and its condition is `'$(CratisProxiesOutputPath)' != ''`. **A missing output
path is not an error — the target simply never runs and nothing is generated.**
That is the first thing to check when a build succeeds but no proxies appear.

Output folders mirror the C# **namespace**, not the file path. A command in
`<Root>.<Feature>` lands in `<output>/<Feature>/`.

## MSBuild properties

Every property below is declared by the package with the default shown.

| Property | Default | Effect |
| --- | --- | --- |
| `CratisProxiesOutputPath` | empty | Where to write. Empty disables generation entirely |
| `CratisProxiesInputAssembly` | empty | Overrides the assembly to read |
| `CratisProxiesSegmentsToSkip` | empty | Leading namespace segments to drop from the output path |
| `CratisProxiesLibraryMode` | `false` | Generates for a library rather than an application |
| `CratisProxiesSkipOutputDeletion` | **`true`** | Already the default — the generator does not wipe the output folder |
| `CratisProxiesSkipCommandNameInRoute` | `false` | Leaves the command name out of the generated route |
| `CratisProxiesSkipQueryNameInRoute` | `false` | Leaves the query name out of the generated route |
| `CratisProxiesApiPrefix` | `api` | Route prefix |
| `CratisProxiesSkipFileIndexTracking` | `false` | Disables tracking which files the generator owns |

⚠️ `CratisProxiesSkipOutputDeletion` defaults to `true`. Guidance that tells you
to set it to `true` "so the generator does not delete your hand-written files"
is describing the default, not a fix.

Item groups the target also forwards: `AssemblyToPackageMapping`,
`ExcludeType`, `ExcludeNamespace`, `NamespaceRoot`, and `TypeToTsType`.

## What the generator extracts for client-side validation

The generated command proxy carries the rules it could extract, so
`validateClientSide()` can run them without a round trip. Extraction reads
validators derived from `AbstractValidator<T>`, `BaseValidator<T>`,
`DiscoverableValidator<T>`, `CommandValidator<T>` and `QueryValidator<T>`.

Recognised FluentValidation rules:

- not null and not empty;
- email;
- length, minimum length, maximum length, exact length;
- comparison rules (greater/less than and their or-equal forms);
- regular expression.

Recognised data annotations: `[Required]`, `[StringLength]`, `[MinLength]`,
`[MaxLength]`, `[Range]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`,
`[Url]`, `[CreditCard]`.

Rules declared on a `ConceptValidator<T>` are projected onto every property
typed as that concept, so declaring a value's format once validates it in the
browser too.

⚠️ **`Must(...)` is not extracted.** Neither is any rule that needs an injected
dependency. Those run on the server only, which means
`validateClientSide()` can pass while `execute()` still fails validation.
FluentValidation rules win over data annotations where both describe the same
property.

## Failure modes

| Symptom | Cause |
| --- | --- |
| Nothing is generated | `CratisProxiesOutputPath` is unset, or the project did not build |
| Files appear under an unexpected folder | The folder mirrors the namespace; change the namespace or use `NamespaceRoot`/`CratisProxiesSegmentsToSkip` |
| Stale proxies after a rename | `dotnet clean` then `dotnet build`; the generator does not delete by default |
| A rule validates on the server but not in the browser | The rule is not in the extractable set above |
