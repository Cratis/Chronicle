<!-- cratis-ai-managed: skills/cratis-specifications-csharp/references/running-specs.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Running specifications and migrating to 5.0

## Contents

- Project shape
- Local loop
- Measure specification cost
- Filters
- Migrate from 4.x to 5.0

The xUnit examples and migration checklist require `Cratis.Specifications.XUnit.v3`
5.0 on `net10.0`. This is not a requirement to migrate frameworks: identify the
project's evaluated runner first. TUnit and `Cratis.Specifications.Runner` suites
also run their built executable, with their own help-confirmed flags, not xUnit
`-trx`/filter flags. Only legacy VSTest projects use `dotnet test <project> --no-build`.
Check package versions and the actual configuration/TFM; the 4.x migration checklist
below applies only when migrating, not as a prerequisite to running an existing suite.

Sources: [Running specifications fast](https://github.com/Cratis/Specifications/blob/main/Documentation/running-fast.md),
[The native runner](https://github.com/Cratis/Specifications/blob/main/Documentation/runner.md),
[Phase timings](https://github.com/Cratis/Specifications/blob/main/Documentation/phase-timings.md) and
[Upgrading to 5.0](https://github.com/Cratis/Specifications/blob/main/Documentation/upgrading-5.0.md).

## Project shape

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
</PropertyGroup>
<ItemGroup>
    <PackageReference Include="Cratis.Specifications.XUnit.v3" />
    <PackageReference Include="xunit.v3.mtp-off" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.runner.visualstudio" />
</ItemGroup>
```

Versions live in `Directory.Packages.props`. Keep `Cratis.Specifications` and the
adapter package on the same version. The Cratis AI repository's `Source/Cratis.AI.Specs`
is a worked example, including the CI step that runs the built executable.

## Local loop

In the upstream reported benchmark of 1,500 trivial tests on a loaded Mac, an
xUnit v3 suite used about 4.5 times more CPU through `dotnet test` than as the
built executable. This is a measurement, not a universal ratio. Restore and build
once, then run the executable:

```bash
dotnet restore Source/MyProject.Specs
dotnet build Source/MyProject.Specs --configuration Release --no-restore
./Source/MyProject.Specs/bin/Release/net10.0/MyProject.Specs
```

Without an apphost, run `dotnet <path>/MyProject.Specs.dll`. `dotnet run --no-build`
also works but still starts MSBuild to resolve the project, so prefer the executable
for repeated runs. A pass is exit code 0 with the reported total equal to the
passed count.

Where a solution mixes Microsoft.Testing.Platform and VSTest projects, run each
project on its own; `dotnet test` on the whole solution does not work there.

## Measure specification cost

Apply the [scenario cost budget](../../../rules/specs.scenarios.csharp.md): plain
decision specs should take roughly milliseconds, not seconds. Measure scenario
facts and justify their cost by pipeline/authorization wiring, constraints,
or declarative mapping that a plain call cannot
prove. Prefer fewer startups, not fewer assertions or weaker production guards.

Capture TRX for the affected project before and after the change. With VSTest,
use `dotnet test <project> --no-build --configuration Release --logger "trx;LogFileName=specs.trx" --results-directory <results>`.
For the built xUnit v3 `mtp-off` executable, use `-trx <results>/specs.trx`;
check `<executable> -h` for other runner flavors rather than mixing switches.
Use separate result paths for each run, under `.ai-work/` for local work.

Inspect `UnitTestResult` entries: rank `duration` by `testName`, group by spec
class, and compare pure calls, command scenarios and read-model scenarios. Shared
setup may be charged to one fact, so also compare class totals and suite wall
time; an individual fact's duration is not its isolated startup cost. Keep package
versions, configuration, filters, concurrency/shards and machine fixed; preserve
discovered/passed/failed/skipped counts and investigate any change. Do not count
restore/build time as test execution or repeat unchanged runs merely for reassurance.

### Diagnose before changing the harness

Measure the existing suite before a rewrite. Separate restore/build, runner
startup/discovery and reporting from specification execution. Phase timings do
not measure total process startup or discovery; compare launch wall time separately.
For repeated local runs, use the built executable rather than repeatedly invoking
`dotnet test`/MSBuild. Keep the evaluated runner and its supported flags.

Enable phase diagnostics without changing pass/fail behavior. This example assumes
the Release/net10.0 executable built above and runs from the repository root:

```bash
mkdir -p .ai-work/spec-timings
CRATIS_SPECS_TIMINGS=1 CRATIS_SPECS_TIMINGS_FILE="$PWD/.ai-work/spec-timings/phases.json" \
    ./Source/MyProject.Specs/bin/Release/net10.0/MyProject.Specs
```

Alternatively, set `<CratisSpecificationsPhaseTimings>true</CratisSpecificationsPhaseTimings>`
in the project. Slow phases default to a 1,000 ms threshold; set
`<CratisSpecificationsSlowPhaseThreshold>500</CratisSpecificationsSlowPhaseThreshold>`
for a 500 ms diagnostic threshold, not a performance budget. The JSON file records
`Establish`, `Because` and `Destroy` durations; native-runner output also records
facts. Other frameworks time facts through their own runner output. Timings are
off by default, additive when enabled, and never change whether a spec passes.

| Cost observed | Next check |
| --- | --- |
| Restore/build | Keep it outside the execution comparison; restore/build once for the local loop. |
| Runner startup/discovery | Compare the built executable's launch path, not just fact durations. |
| Repeated `Establish`/`Because` | Check shared lifecycle eligibility and opt-outs before changing packages. |
| Scenario setup/action | Identify pipeline, mapping or append-time proof a plain call cannot provide. |
| Slow fact | Keep assertions read-only; inspect the measured operation rather than adding sleeps. |
| `Destroy` | Measure disposal/cleanup separately from setup and action. |

Specifications 5 shares eligible xUnit v2/v3 lifecycle by default. `Establish` and
`Because` run once per class; facts only read that shared context. Repeating a
200 ms `Because` across five facts costs about 1 s instead of about 200 ms in
this illustrative calculation, before other costs. Remove accidental opt-outs
rather than weakening assertions; retain `[PerFactSpecificationLifecycle]` only
where mutation genuinely cannot move into `Because`.

Record a before/after comparison, not a claimed universal timing:

| Field | Keep or report |
| --- | --- |
| Environment | Machine, package versions, configuration/TFM, launch command, filters and unchanged concurrency/shards. |
| Coverage | Discovered, passed, failed and skipped counts plus the class/method list; investigate any difference. |
| Execution | Suite wall time, class totals and phase/fact durations in separate result files. |
| Change | Name the removed repeated cost or the extra framework proof that justifies scenario cost. |

Do not merge unrelated behaviors, remove facts or turn off production guards to
meet a timing target. One class per declarative read model remains the default:
share a meaningful event sequence, capture intermediate states and assert them
with many read-only facts; add a class only for a sequence that cannot share it.
A slow mandatory scenario is an upstream harness gap, not permission to drop proof.

## Filters

The executable accepts the filters of the xUnit v3 runner flavor the project
references. Check the flavor with `<executable> -h`; the `run-tests` skill owns
the command choice and `filter-syntax` owns the expression syntax.

| Project references | Filter flags |
| --- | --- |
| `xunit.v3.mtp-off` (house default) | `-class`, `-method`, `-namespace`, `-trait`, or one `-filter "<query>"`; do not mix the three kinds |
| MTP-enabled `xunit.v3` | `--filter-class`, `--filter-method`, `--filter-trait`, or one `--filter-query` |
| `Cratis.Specifications.Runner` (experimental native runner) | `--treenode-filter '/*/*/when_registering/*'` or `--filter-uid <uid>` |

Do not use `dotnet test --filter` against an MTP executable. See the
[`run-tests`](../../run-tests/SKILL.md) and [`filter-syntax`](../../filter-syntax/SKILL.md)
skills for the exact command per platform.

## Migrate from 4.x to 5.0

Follow the upstream [upgrade guide](https://github.com/Cratis/Specifications/blob/main/Documentation/upgrading-5.0.md)
and record the spec count and outcomes first.

1. Record the baseline: total, passed, failed and skipped counts, and the list of
   discovered classes and methods, from the unmodified branch.
2. Target `net10.0` in every specification project; projects that must stay on
   net8.0/net9.0 stay on 4.x.
3. Move `Cratis.Specifications` and the adapter package to the same 5.0 version. For
   xUnit v3 switch to `Cratis.Specifications.XUnit.v3`, `xunit.v3.mtp-off` and
   `<OutputType>Exe</OutputType>`.
4. Delete any `[assembly: Xunit.TestFramework(...)]` that registers Cratis
   Specifications.
5. Fix each `CRSPEC0008` error by moving the change into `Establish` or `Because`; use
   `[PerFactSpecificationLifecycle]` only on a class whose facts genuinely mutate shared
   state and cannot move that mutation into `Because`.
6. Change assertion delegates typed as `Predicate<T>` to `Func<T, bool>`; for
   NUnit 4 comparisons that relied on unordered `ShouldEqual(params)`, use
   `ShouldEqualInAnyOrder`. Null actual values now fail, including nullable
   `ShouldNotContain` strings. Assertions live in core with forwarded types;
   rebuild with warnings as errors and fix new nullable warnings. Delete
   `CratisSpecificationsWarnOnSharedContextMutation` (it has no effect in 5).
   Native-runner `async void` facts/phases must return `Task` or `ValueTask`
   instead (CRSPEC0010 is an error).
7. Run the executable and require the same total, the same passed count and the same
   classes and methods as the baseline (display names may become more readable),
   with no new skips. A different count means a spec was
   lost or duplicated; investigate before continuing.
8. Point CI at the built executable, and run mixed solutions project by project.
