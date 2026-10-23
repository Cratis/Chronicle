<!-- cratis-ai-managed: skills/cratis-ci-pipeline-performance/references/build-once-and-shard.md -->
# Build once, shard the tests

## Contents

- When it fits
- Shape
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
      - name: Test the shard's projects
        env:
          PROJECTS: ${{ matrix.projects }}
        run: |
          failed=0
          for project in $PROJECTS; do
            dotnet test "$project" --no-build --configuration Release \
              --logger "trx;LogFileName=$(basename "$project").trx" \
              --results-directory "$RUNNER_TEMP/test-results" || failed=1
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
re-measure; stop when the spread is within the job start cost.

## Decisions

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

- A filter or project list that matches nothing passes `dotnet test`. End every
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
