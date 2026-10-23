<!-- cratis-ai-managed: skills/check-bin-obj-clash/references/common-causes-and-fixes.md -->
# Output-path clash causes and fixes

## Contents

- Common Causes and Fixes
  - Multi-targeting without TargetFramework in path
  - Shared output directory across projects (CANNOT be fixed with AppendTargetFramework)
  - RuntimeIdentifier builds clashing
  - Multiple solutions building the same project
  - Extra global properties creating redundant project instances
  - Explicit `<MSBuild>` Build/Publish with extra global properties (self or cross-project)
  - `SetTargetFramework` re-injecting a single-targeting project's own TFM on a `ProjectReference`

## Common Causes and Fixes

### Multi-targeting without TargetFramework in path

**Problem:** Project uses `TargetFrameworks` but OutputPath doesn't vary by framework.

```xml
<!-- BAD: Same path for all frameworks -->
<OutputPath>bin\$(Configuration)\</OutputPath>
```

**Fix:** Include TargetFramework in the path:

```xml
<!-- GOOD: Path varies by framework -->
<OutputPath>bin\$(Configuration)\$(TargetFramework)\</OutputPath>
```

Or rely on SDK defaults which handle this automatically:

```xml
<AppendTargetFrameworkToOutputPath>true</AppendTargetFrameworkToOutputPath>
<AppendTargetFrameworkToIntermediateOutputPath>true</AppendTargetFrameworkToIntermediateOutputPath>
```

### Shared output directory across projects (CANNOT be fixed with AppendTargetFramework)

**Problem:** Multiple projects explicitly set the same `BaseOutputPath` or `BaseIntermediateOutputPath`.

```xml
<!-- Project A - Directory.Build.props -->
<BaseOutputPath>..\SharedOutput\</BaseOutputPath>
<BaseIntermediateOutputPath>..\SharedObj\</BaseIntermediateOutputPath>

<!-- Project B - Directory.Build.props -->
<BaseOutputPath>..\SharedOutput\</BaseOutputPath>
<BaseIntermediateOutputPath>..\SharedObj\</BaseIntermediateOutputPath>
```

**IMPORTANT:** Even with `AppendTargetFrameworkToOutputPath=true`, this will still clash! .NET writes certain files directly to the `IntermediateOutputPath` without the TargetFramework suffix, including:

- `project.assets.json` (NuGet restore output)
- Other NuGet-related files

This causes errors like `Cannot create a file when that file already exists` during parallel restore.

**Fix:** Each project MUST have a unique `BaseIntermediateOutputPath`. Do not share intermediate output directories across projects:

```xml
<!-- Project A -->
<BaseIntermediateOutputPath>..\obj\ProjectA\</BaseIntermediateOutputPath>

<!-- Project B -->
<BaseIntermediateOutputPath>..\obj\ProjectB\</BaseIntermediateOutputPath>
```

Or simply use the SDK defaults which place `obj` inside each project's directory.

### RuntimeIdentifier builds clashing

**Problem:** Building for multiple RIDs without RID in path.

**Fix:** Ensure RuntimeIdentifier is in the path:

```xml
<AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>
```

### Multiple solutions building the same project

**Problem:** A single build invokes multiple solutions (e.g., via MSBuild task or command line) that include the same project. Each solution build evaluates and builds the project independently, with different `Solution*` global properties that don't affect the output path.

**How to detect:** Compare `SolutionFileName` and `CurrentSolutionConfigurationContents` across evaluations for the same project. Different values indicate multi-solution builds. For example:

| Property | Eval from Solution A | Eval from Solution B |
|---|---|---|
| `SolutionFileName` | `BuildAnalyzers.sln` | `Main.slnx` |
| `CurrentSolutionConfigurationContents` | 1 project entry | ~49 project entries |
| `OutputPath` | `bin\Release\netstandard2.0\` | `bin\Release\netstandard2.0\` ← **clash** |

**Example:** A repo build script builds `BuildAnalyzers.sln` then `Main.slnx`, and both solutions include `SharedAnalyzers.csproj`. Both builds write to `bin\Release\netstandard2.0\`. The first build compiles; the second skips compilation but still runs `CopyFilesToOutputDirectory`.

**Fix:** Options include:
1. **Consolidate solutions** - Ensure each project is only built from one solution in a single build
2. **Use different configurations** - Build solutions with different `Configuration` values that result in different output paths
3. **Exclude duplicate projects** - Use solution filters or conditional project inclusion to avoid building the same project twice

### Extra global properties creating redundant project instances

**Problem:** A project is built multiple times within the same solution due to extra global properties (e.g., `PublishReadyToRun=false`) that create distinct MSBuild project instances. These properties don't affect output paths but prevent MSBuild from caching results across instances, causing redundant target execution.

**How to detect:** Compare global properties across evaluations for the same project within the same solution (same `SolutionFileName`). Look for properties that differ but don't contribute to path differentiation:

| Property | Eval A (from Razor.slnx) | Eval B (from Razor.slnx) |
|---|---|---|
| `PublishReadyToRun` | *(not set)* | `false` |
| `OutputPath` | `bin\Release\netstandard2.0\` | `bin\Release\netstandard2.0\` ← **clash** |

This is particularly wasteful for projects where the extra property has no effect (e.g., `PublishReadyToRun` on a `netstandard2.0` class library that doesn't use ReadyToRun compilation).

**Fix:** Options include:
1. **Remove the extra global property** - Investigate which parent target/task is injecting the property and prevent it from being passed to projects that don't need it
2. **Use `RemoveGlobalProperties` metadata** - On `ProjectReference` items, use `RemoveGlobalProperties="PublishReadyToRun"` to strip the property before building the referenced project
3. **Condition the property** - Only set the property on projects that actually use it (e.g., only for executable projects, not class libraries)

### Explicit `<MSBuild>` Build/Publish with extra global properties (self or cross-project)

**Problem:** A target uses the `<MSBuild>` task to build or publish a project with an extra global property, most commonly a "publish-on-build" target. The offending call can be in the target project itself **or in another project** that consumes it (e.g. a test or layout project publishing a tool):

```xml
<!-- (a) same project (publish-on-build) -->
<Target Name="PublishOnBuild" AfterTargets="Build">
  <MSBuild Projects="$(MSBuildProjectFullPath)" Targets="Publish" Properties="_IsPublishing=true" />
</Target>

<!-- (b) project A publishes project B that it consumes -->
<MSBuild Projects="..\tool\tool.csproj" Targets="Publish" Properties="_IsPublishing=true" />
```

Either way this forks a distinct instance of the target project (`path` + `{_IsPublishing=true}`) that shares the same `OutputPath`/`IntermediateOutputPath` as the instance the solution/graph already builds. Both write the same files — for NativeAOT this includes the `*.sourcelink` intermediate, which produces `SourceLinkWriter` / "file in use" failures under parallel builds.

**How to detect:** Follow the Primary workflow above — the `evaluations` and `evaluation_global_properties` tools surface two evaluations of the target project that share the same `OutputPath`/`IntermediateOutputPath` but differ only by a path-neutral publish flag such as `_IsPublishing`, and the `double_writes` tool flags the resulting shared-file writes directly. To tell case (a) from (b), see which project the extra `{_IsPublishing=true}` evaluation runs *under* in the build tree (from the overview/projects tools): the target project itself for (a), or a consumer project that invoked the `<MSBuild>` task for (b).

**Fix:** Depends on where the call lives:

- **Same project (a):** you can't strip the property with `RemoveGlobalProperties` (the project injects it on itself). Set the flag as a **static** (non-global) property and run the target in the **same** instance via `DependsOnTargets`/`CallTarget`, with a guard against a target cycle when publish is the entry point:

```xml
<PropertyGroup>
  <_PublishWasInvokedDirectly Condition="'$(_IsPublishing)' == 'true'">true</_PublishWasInvokedDirectly>
  <_IsPublishing>true</_IsPublishing>
</PropertyGroup>
<Target Name="PublishOnBuild"
        AfterTargets="Build"
        DependsOnTargets="Publish"
        Condition="'$(_PublishWasInvokedDirectly)' != 'true'" />
```

- **Cross-project (b):** the consumer must not fork the producer with path-neutral global properties. Make the producer publish as part of its own build (the (a) fix in *its* project), then have the consumer **sequence** it and read its output instead of re-publishing it:

```xml
<ItemGroup>
  <ProjectReference Include="..\tool\tool.csproj" ReferenceOutputAssembly="false" />
</ItemGroup>
<!-- consumer reads tool's publish dir; it does NOT invoke Publish on tool -->
```

See `msbuild` skill `references/msbuild-antipatterns.md` (AP-22) for the authoring-time smell and rationale.

### `SetTargetFramework` re-injecting a single-targeting project's own TFM on a `ProjectReference`

**Problem:** A `ProjectReference` sets `SetTargetFramework="TargetFramework=<tfm>"` metadata pointing at a **single-targeting** project (one that uses singular `<TargetFramework>`, not `<TargetFrameworks>`), where the injected `<tfm>` **equals the TFM the project already targets**. `SetTargetFramework` injects `TargetFramework` as a **global property** on the referenced project's build.

```xml
<!-- BAD: Tool.csproj single-targets net8.0 and we inject that SAME net8.0 -->
<ProjectReference Include="..\Tool\Tool.csproj" SetTargetFramework="TargetFramework=net8.0" />
```

Injecting the TFM the project already targets is **path-neutral** — the project already resolves to `bin\<config>\net8.0\` and `obj\<config>\net8.0\` on its own. So it doesn't change the output path; it only forks a distinct instance `(project, {TargetFramework=net8.0})`. The solution/graph builds the very same project as `(project, {})`. Both share the same `OutputPath`/`IntermediateOutputPath`, so the project is **built twice** to the same location — a bin/obj clash under parallel builds.

**How to detect:** Follow the Primary workflow above. The `evaluations` and `evaluation_global_properties` tools surface two evaluations of the referenced project that share the same `OutputPath`/`IntermediateOutputPath` and differ only by a `TargetFramework` global property, while the project itself is single-targeting (its own `TargetFramework` already equals the injected value). The `double_writes` tool flags the resulting shared-file writes directly.

**Note:** The P2P protocol itself does **not** inject `TargetFramework` for a non-multi-targeting reference — the clash comes specifically from the explicit `SetTargetFramework` metadata overriding that safe default.

**Fix:** Remove the redundant `SetTargetFramework` when it just restates the project's own single TFM:

```xml
<!-- GOOD -->
<ProjectReference Include="..\Tool\Tool.csproj" />
```

**When `SetTargetFramework` is legitimate (not a clash):**

- **Multi-targeting reference** — the referenced project uses `<TargetFrameworks>` and you need one specific TFM. Each TFM has a distinct output path, so no clash.
- **Overriding to a *different* TFM** — you may use `SetTargetFramework` on a single-targeting project to build it under a TFM *other than* the one it declares. Because the injected TFM then changes the output path (`obj\<config>\<different-tfm>\`), the instance no longer collides with `(project, {})`. Only the *same-TFM* case is path-neutral and clashing.
- **Framework-incompatible reference** — whenever the referencing and referenced projects target **incompatible frameworks** (e.g. a `.NETFramework` project referencing a `.NETCoreApp` project, or vice-versa) — **regardless of single- or multi-targeting on either side** — set `SkipGetTargetFrameworkProperties="true"` (the P2P `GetTargetFrameworkProperties` negotiation would otherwise fail) and `ReferenceOutputAssembly="false"` (an assembly built for an incompatible framework can't be consumed as a reference — you only want to trigger/sequence the build):

  ```xml
  <ProjectReference Include="..\Tool\Tool.csproj"
                    SkipGetTargetFrameworkProperties="true"
                    ReferenceOutputAssembly="false" />
  ```

  With `SkipGetTargetFrameworkProperties="true"`, the negotiation no longer stops the **referencing** project's own `TargetFramework` global property (present when it builds for a specific TFM, e.g. it is multi-targeting) from flowing into the referenced project. For a **single-targeting** referenced project that would force it to build under the wrong TFM / output path. Prevent it by either setting `SetTargetFramework="TargetFramework=<tfm>"` (pin the TFM) **or** `UndefineProperties="TargetFramework"` (strip the inherited global property so the project builds as it declares) — use one, not both:

  ```xml
  <ProjectReference Include="..\Tool\Tool.csproj"
                    SkipGetTargetFrameworkProperties="true"
                    UndefineProperties="TargetFramework"
                    ReferenceOutputAssembly="false" />
  ```

See `msbuild` skill `references/msbuild-antipatterns.md` (AP-23) for the authoring-time smell and rationale.
