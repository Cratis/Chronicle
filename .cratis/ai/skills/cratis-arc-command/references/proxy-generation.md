<!-- cratis-ai-managed: skills/cratis-arc-command/references/proxy-generation.md -->
# Proxy generation

## Contents

- Wiring
- MSBuild properties
- Route settings must match the host and the frontend
- Skipping generation
- What the generator extracts for client-side validation
- Failure modes
- Blocking severity

Source: `Cratis.Arc.ProxyGenerator.Build`.

## Wiring

Reference the build package from the project that contains the commands,
queries and read models, and set an output path:

```xml
<PackageReference Include="Cratis.Arc.ProxyGenerator.Build" Version="22.139.0" />

<PropertyGroup>
  <CratisProxiesOutputPath>$(MSBuildThisFileDirectory)../<Web>/src/api</CratisProxiesOutputPath>
</PropertyGroup>
```

Install `@cratis/arc` and `@cratis/arc.react` on the frontend side.

The package adds a `CratisProxyGenerator` target that runs `AfterTargets="AfterBuild"`,
and its condition is `'$(CratisProxiesOutputPath)' != '' and '$(CratisProxiesSkip)' != 'true'
and '$(DisableProxyGenerator)' != 'true'` (earlier Arc checks only the path). **A missing output
path is not an error — the target simply never runs and nothing is generated.**
That is the first thing to check when a build succeeds but no proxies appear.

Output folders mirror the C# **namespace**, not the file path. A command in
`<Root>.<Feature>` lands in `<output>/<Feature>/`.

Requires Arc 22.82.0 for collection command responses to generate array types
in `Command<...>` and `useCommand<...>` (for example `IEnumerable<ProjectSummary>`
becomes `ProjectSummary[]`). Check the generator package version and regenerate;
older proxies can type the already-array runtime response incorrectly.

## MSBuild properties

Properties below are forwarded by the package's `.props`/`.targets`; boolean
switches without a declared default are off when absent.

| Property | Default | Effect |
| --- | --- | --- |
| `CratisProxiesOutputPath` | empty | Where to write. Empty disables generation entirely |
| `CratisProxiesSkip` | off | `true` skips the generator even when the output path is set (see Skipping generation) |
| `CratisProxiesInputAssembly` | empty | Overrides the assembly to read |
| `CratisProxiesIncremental` | `true` | Requires Arc 22.138.0; reuse unchanged owned output; set `false` to regenerate every build |
| `CratisProxiesFileHeader` | project setting | Requires Arc 22.123.0; otherwise uses `.editorconfig` `file_header_template`, or no header; `none` disables it |
| `CratisProxiesSegmentsToSkip` | empty | Leading namespace segments to drop from the output path |
| `CratisProxiesLibraryMode` | `false` | Generates for a library rather than an application |
| `CratisProxiesSkipOutputDeletion` | **`true`** | Already the default — the generator does not wipe the output folder |
| `CratisProxiesSkipCommandNameInRoute` | `false` | Leaves the command name out of the generated route |
| `CratisProxiesSkipQueryNameInRoute` | `false` | Leaves the query name out of the generated route |
| `CratisProxiesApiPrefix` | `api` | Route prefix |
| `CratisProxiesSkipFileIndexTracking` | `false` | Disables tracking which files the generator owns |
| `CratisProxiesSkipIndexGeneration` | `false` | Does not write the per-folder `index.ts` barrels |
| `CratisProxiesUseSourceFileAsOutputFile` | `false` | Names the generated file after the C# source file rather than the type |
| `CratisProxiesUseProxyFileSuffix` | off | Emits `Name.proxy.ts` and matching barrels; avoids `.ts`/`.tsx` basename collisions |
| `CratisProxiesEmitInterfaces` | off | Emits plain TypeScript `interface`s instead of `@field`-decorated classes — no `@cratis/fundamentals` import in the output. Only for consumers that never reconstruct instances from JSON; command and query proxies need the classes. Needs Arc 22.15.0 or later. |

⚠️ `CratisProxiesSkipOutputDeletion` defaults to `true`. Guidance that tells you
to set it to `true` "so the generator does not delete your hand-written files"
is describing the default, not a fix.

The generator preserves hand-written `index.ts` exports, and generated files
follow the Cratis ESLint configuration. Named concepts live only in the backend
model: generated TypeScript erases them to primitives; nullable concept properties
are optional. Sources:
`Tools/ProxyGenerator.Build/build/Cratis.Arc.ProxyGenerator.Build.targets` under
`Source/DotNET`.

Incremental generation fingerprints assemblies, generator/configuration, validation metadata
and owned output. A changed or missing generated file forces regeneration, as
does a previous warning so diagnostics remain visible. It does not make a
hand-edited generated file an input. Configure `file_header_template` explicitly
if you previously relied on the built-in Cratis MIT header; it is no longer
inserted without configuration.

Item groups the target also forwards: `AssemblyToPackageMapping`,
`ExcludeType`, `ExcludeNamespace`, `NamespaceRoot`, and `TypeToTsType`.

## Route settings must match the host and the frontend

The route properties (`CratisProxiesApiPrefix`, `CratisProxiesSegmentsToSkip`,
`CratisProxiesSkipCommandNameInRoute`, `CratisProxiesSkipQueryNameInRoute`) shape
the URLs written into the generated client. The host maps endpoints from
`Cratis:Arc:GeneratedApis` (`RoutePrefix` default `api`, `SegmentsToSkipForRoute`
default `0`, `IncludeCommandNameInRoute` and `IncludeQueryNameInRoute` default
`true`). The skip properties and the include keys are inverses. Arc does not
compare them: a mismatch is a 404 with no build error. Change both in one
commit.

On the frontend, the generated route already starts with the API prefix.
`<Arc apiBasePath>` is only an extra deployment prefix (`/workbench` plus
`/api/orders` is `/workbench/api/orders`); never repeat `/api` there, and
`basePath` does nothing. See `cratis-arc-configuration` for the full list.

## Skipping generation

**Proxies are generated by the committing client, never in CI.** The client that
changes a command or query builds Debug, generates the proxies and commits them
with the change. CI builds compile against the committed proxies; a CI build that
regenerates them produces diffs nobody commits or drift that exists only on the
runner. Every CI build of an Arc project passes `-p:CratisProxiesSkip=true
-p:CratisProxiesOutputPath=` in every configuration.

Requires Arc 22.185.1 or later for `-p:CratisProxiesSkip=true`, Arc's off switch; fall
back to an empty `-p:CratisProxiesOutputPath=` on older Arc.

The `CratisProxyGenerator` target runs only when `CratisProxiesOutputPath` is non-empty
and `CratisProxiesSkip` is not `true`, so Skip works even when the project sets the
output path. Older Arc ignores `CratisProxiesSkip` silently (MSBuild accepts unknown
`-p:` names), so a build that must work on both passes both. Prefer `CratisProxiesSkip`;
`DisableProxyGenerator` is only an alias of it in current Arc, and elsewhere works only
where a repository defines it (the Chronicle kernel `Core.csproj` does).

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

Conditional `When`/`Unless` rules are not projected to the client. Extracted
rules retain severity (`withSeverity`); command blocking thresholds still apply.

⚠️ **`Must(...)` is not extracted.** Neither is any rule that needs an injected
dependency. Those run on the server only, which means
`validateClientSide()` can pass while `execute()` still fails validation.
FluentValidation rules win over data annotations where both describe the same
property.

## Failure modes

| Symptom | Cause |
| --- | --- |
| Nothing is generated | `CratisProxiesOutputPath` is unset, `CratisProxiesSkip` is `true`, or the project did not build |
| Files appear under an unexpected folder | The folder mirrors the namespace; change the namespace or use `NamespaceRoot`/`CratisProxiesSegmentsToSkip` |
| Stale proxies after a rename | `dotnet clean` then `dotnet build`; the generator does not delete by default |
| A rule validates on the server but not in the browser | The rule is not in the extractable set above |

## Blocking severity

`[BlockOnValidationSeverity(ValidationResultSeverity.Warning)]` declares the
lowest blocking severity for validation, `Provide` and execution. Caller
`allowedSeverity`/`X-Allowed-Severity` can only make it stricter; `Unknown` blocks.
The proxy carries `blockOnValidationSeverity` and honors it client-side.
