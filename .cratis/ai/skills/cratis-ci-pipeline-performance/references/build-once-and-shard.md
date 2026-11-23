<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/build-once-and-shard.md -->
# Build once, shard the tests

## Contents

- When it fits
- Shape
- One analyzer owner per build tuple
- Order of steps
- Specs from built executables
- Decisions
- Pitfalls
- Fewer, bigger jobs

Use this when one workflow builds the same code more than once, or runs build and
tests serially in one long job. The pattern: a single build job compiles in
`Release` and uploads the output; a shard matrix downloads it and runs the tests
with `--no-build`. The wall clock becomes build + the slowest shard instead of
build + all tests.

## When it fits

- The report shows `execution-bound` and one job holds most of the span.
- Several jobs each restore and compile the same solution.
- Tests are independent by project or namespace.

It does not fit when the build is short compared with job start cost, or when
the report is `queue-bound`: more shards are more independently scheduled jobs.
Merge shards first, then re-measure.

## Shape

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    timeout-minutes: 45
    permissions:
      contents: read
    steps:
      - uses: actions/checkout@v7 # pin by full commit SHA
      - uses: actions/setup-dotnet@v6 # pin by full commit SHA
      - uses: Cratis/Workflows/.github/actions/dotnet-cache@main
      - run: dotnet build Repo.slnx --configuration Release
      - name: Archive build output
        run: |
          set -euo pipefail
          find . -type d -name bin -not -path '*/node_modules/*' -not -path './.git/*' -print0 \
            | tar --null -T - --zstd -cf build-output.tar.zst
      - uses: actions/upload-artifact@v7 # pin by full commit SHA
        with:
          name: build-output
          path: build-output.tar.zst
          compression-level: 0
          retention-days: 1
          if-no-files-found: error

  specs:
    name: ${{ matrix.shard }}
    needs: [build]
    runs-on: ubuntu-latest
    timeout-minutes: 30
    permissions:
      contents: read
    strategy:
      fail-fast: false
      matrix:
        include:
          - shard: specs-1
            projects: Source/Core.Specs Source/Storage.Specs
          - shard: specs-2
            projects: Source/Api.Specs
          - shard: specs-3
            projects: Source/Client.Specs Source/Tools.Specs
    steps:
      - uses: actions/checkout@v7 # pin by full commit SHA
      - uses: actions/setup-dotnet@v6 # pin by full commit SHA
      - uses: actions/download-artifact@v8 # pin by full commit SHA
        with:
          name: build-output
      - run: tar --zstd -xf build-output.tar.zst
      # Example assumes each xUnit v3 mtp-off AssemblyName matches its directory.
      # Use evaluated TargetPath for custom outputs; TUnit/native runners need their own report flags.
      # Only VSTest projects use dotnet test <project> --no-build --configuration Release with --logger/--results-directory.
      # The executable must come from the same tuple (configuration, TFM, RID) the build job produced.
      - name: Test the shard's projects
        env:
          PROJECTS: ${{ matrix.projects }}
        run: |
          failed=0
          for project in $PROJECTS; do
            mkdir -p "$RUNNER_TEMP/test-results"
            # xUnit v3 mtp-off example: resolve each project's actual AssemblyName/TFM.
            "$project/bin/Release/net10.0/$(basename "$project")" \
              -trx "$RUNNER_TEMP/test-results/$(basename "$project").trx" || failed=1
          done
          exit "$failed"
      - name: Verify every project ran tests
        if: ${{ !cancelled() }}
        env:
          PROJECTS: ${{ matrix.projects }}
        run: |
          failed=0
          for project in $PROJECTS; do
            file="$RUNNER_TEMP/test-results/$(basename "$project").trx"
            executed=$(grep -o -m1 'executed="[0-9]*"' "$file" 2>/dev/null | grep -o '[0-9]*' || echo 0)
            echo "$project executed $executed tests"
            [ "$executed" -gt 0 ] || failed=1
          done
          exit "$failed"
      - uses: actions/upload-artifact@v7 # pin by full commit SHA
        if: ${{ !cancelled() }}
        with:
          name: test-results-${{ matrix.shard }}
          path: ${{ runner.temp }}/test-results/*.trx
          if-no-files-found: warn
```

Each matrix entry carries its own `projects` list, which the test step reads, so
shards run different tests. For a test framework filter instead of a project list,
carry the filter in the entry (`filter: FullyQualifiedName~Core`) and pass it as
`--filter`. Keep the assignment in the workflow or one checked-in script so that
adding a project cannot leave it unassigned; review the list when a project is added.

To balance, read each shard job's median duration from the slowest-jobs table of
`ci-timings.sh`, move a project from the longest shard to the shortest, and
re-measure; stop when the spread is within the job start cost. Balance by
measured wall time, and confirm the new split on one real CI run before merging it.

## One analyzer owner per build tuple

A build tuple is the configuration, target framework, runtime identifier and every
build-affecting property. Every tuple that is built, tested or shipped has
**exactly one** owning build that runs the analyzers with warnings as errors:
never zero, and a second owner only repeats the cost.

- **Default owner: a parallel required job.** It builds the same tuple as the spec
  build with the analyzers on and warnings as errors, and fails early on compile and
  analyzer errors. The spec build is a non-owner and passes `-p:RunAnalyzers=false`.
  The owning job is a required check: never leave it out of branch protection, and
  the pre-push gate runs the analyzers too (see
  [analyzer cost](test-setup-cost.md#analyzer-cost)).
- **Alternative owner: the shared spec build.** A repository may choose, by its own
  decision record, that the build feeding the specs owns the analyzers, so the specs
  run on analyzed output. Then no separate analyzer job exists for that tuple.
- **Non-owners** pass `-p:RunAnalyzers=false` (and `-p:EnforceCodeStyleInBuild=false`
  where code style runs in the build). Use `RunAnalyzers`/`RunAnalyzersDuringBuild`
  as the owner switch: `EnableNETAnalyzers=false` only turns off the .NET SDK's own
  analyzers and leaves other analysis running, so it is not an owner switch. Never
  remove `Analyzer` items either: generators are delivered as analyzer references,
  so the output would lose its generated code.
- **Check generators survived.** Assert that a known generated file or type is in a
  non-owner build's output before trusting it.
- **A tuple that is packed or published is always owned**, so shipped binaries were
  analyzed by some build.
- **Match tuples.** A shard that builds Release/net10.0 after an owner built Debug
  compiles again and leaves its own tuple unowned. Reuse the owner's output, or make
  the second tuple own its analyzers.

## Order of steps

1. Restore once.
2. Build the owning tuple.
3. Later builds use `--no-restore`.
4. Run the specs from the built executables.
5. Pack with `--no-build`.

Publish and native AOT are separate builds with their own tuple; do not run
`dotnet publish` nested inside a spec step. Do not split projects to speed up the
build: measure with `-bl` and `ReportAnalyzer=true` first (see the `msbuild` skill's
build performance references).

## Specs from built executables

xUnit v3 projects are executables: run the built file from the build tuple's output
directory instead of `dotnet test`, which rebuilds implicitly. Native filters are
owned by [running-specs](../../cratis-specifications-csharp/references/running-specs.md);
do not restate the syntax here. Fail closed on counts:

- every shard reports executed > 0;
- the total across shards equals the spec count before the change;
- repositories with unmigrated v3 consumers track them in their own migration issue.

## Decisions

- **Cache dependencies only.** Cache restored packages and installed dependencies,
  never build output; build output travels as an artifact.
- **Artifact, not cache, between jobs of one run.** A cache keyed on the commit
  SHA is immutable: one failed save poisons the key for every later attempt and a
  rerun cannot recover. An artifact belongs to the run and is the intended
  primitive for passing build output between its jobs.
- **Archive with `tar`.** Artifact upload does not preserve file permissions or
  empty directories; a tar keeps both. Output that was already compressed should
  not be compressed again: `compression-level: 0`.
- **Include every consumed output.** Frontend-dependent specs need the built
  `dist` folders as well as `bin`; list them in the archive.
- **Short retention.** `retention-days: 1` for build output nobody downloads by
  hand. Test results can live longer.
- **Same configuration end to end.** Test with the configuration that built
  (`--configuration Release --no-build`); a different one silently rebuilds.
- **`fail-fast: false`** on shard matrices, so one failing shard does not hide the
  results of the others, and a rerun of failed jobs reruns only the failed shard.
- **Upload results with `if: ${{ !cancelled() }}`** so failures keep their TRX.

## Pitfalls

- A filter or project list that matches nothing passes: an executable with a
  filter matching nothing exits 0, and so does VSTest `dotnet test`. End every
  shard with a check that each assigned project or namespace ran at least one test,
  and print the counts. Zero tests is a failed shard.
- Shards balance by test time, not by project count (see the balancing steps
  after the example).
- A shard that downloads a large artifact pays that transfer on every job; measure
  the download step in the slowest-steps table before adding shards.
- Static assets outside `bin`/`dist` (generated sources, local tool caches) must
  be in the archive or regenerated by the shard.

## Fewer, bigger jobs

When the report is `queue-bound`, every extra job costs a scheduler start, a
checkout and a toolchain setup. Merge sibling jobs that share setup (several
lint or format checks, several small build legs) into one job with several steps.
Keep a job separate only when it is on the critical path and long enough that
parallelism repays its start. State the expected effect on both wall clock and
runner-minutes, since merging parallel work lengthens the chain.
