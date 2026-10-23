<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/test-setup-cost.md -->
# Test setup cost and where to test a guarantee

Use this when a test lane is slow but the runner's per-test times look small, when
a suite re-tests a dependency, or when compile errors surface late.

## Contents

- Time hidden outside per-test timing
- Profile the test host
- Fix setup upstream
- Test each guarantee where it can break
- Surface compile errors early
- Count compiles per push

## Time hidden outside per-test timing

Compare the test runner's reported total test time with the wall time of the test
step. A large gap means time is spent outside what the runner attributes to tests:
per-class or per-fixture setup (scenario, host or container construction), discovery,
or process start. Specification frameworks that build a context in a base-class
setup often charge it to no test at all.

1. Read both numbers from the same run: the runner's summary and the step duration
   from `ci-timings.sh`'s slowest-steps table.
2. If setup dominates, sharding only divides the waste across more jobs. Fix the
   setup first.

## Profile the test host

Measure before guessing, with the language's profiler on the test process:

```bash
dotnet tool install --global dotnet-trace
dotnet-trace collect --format speedscope -- dotnet test Project.Specs --no-build --configuration Release
```

Read the inclusive time of the setup methods. Pass means the profile names the
dominant constructor or discovery call; a flat profile means the cost is elsewhere
(I/O, container start, process start).

## Fix setup upstream

Per-test construction of identical, immutable data is the usual cause. Fix it where
the data originates rather than skipping specifications:

- Build shared metadata (type discovery, schemas, registrations) once per process
  and reuse it across scenarios.
- Share expensive generators and caches instead of creating one per instance.
- Cache reflection results that are recomputed on every call.
- Use a prebuilt service collection as a template and copy it per test.
- Do not share mutable state between tests to save time; share only what is immutable.

A fix belongs in the library that owns the setup, not in each consuming repository.

## Test each guarantee where it can break

- A suite that tests a dependency's semantics belongs in that dependency's own
  repository. In a consumer, run it only when the dependency pin changes, or nightly.
- Check a wire or API shape with a committed snapshot and a diff, not a live
  integration run.
- Mixed-version or upgrade matrices run nightly or before a release, not on every
  pull request.
- Where a behavior is only meaningful in a deployed environment, prefer a
  post-deploy synthetic check plus unit specifications and contract snapshots over
  a pull-request integration environment.
- Never remove the only check of a required guarantee; relocate it and say where it
  now runs.

## Surface compile errors early

A pull request that fails to compile should fail in the first minutes, not after a
long test leg. Make a fast build or lint job a `needs` of the long legs, and keep
it short enough that it is not itself the critical path.

## Count compiles per push

Count how many times one push compiles the same code across all jobs and
workflows (`dotnet build` or `dotnet test` without `--no-build` in the step list).
Each extra compile is avoidable: build once, reuse the output (see
`build-once-and-shard.md`). Analyzers can roughly double compile time; run them once
in a parallel lint job and disable them in the shared build, while keeping the
strictest configuration that consumers enforce somewhere on the required path.
