---
title: "Interpreting Integration Timings"
description: "Separate build, fixture, scenario, SQL command and cleanup costs without adding overlapping measurements."
---

Opt-in integration timings explain where an existing test invocation spends time. They do not change test selection, timeouts, retries, database isolation or host lifetime. They are diagnostics, not a benchmark or a speedup claim.

## Collect an existing CI invocation

Use Node.js 22 or later. Set these variables before starting the test process:

- `CHRONICLE_INTEGRATION_TIMINGS=1` enables records. Without it, the instrumentation produces no timing output and reads no timing clock.
- `CHRONICLE_INTEGRATION_RUN_ID` identifies one CI invocation. Use a different value for every retry and matrix entry.
- `CHRONICLE_INTEGRATION_TIMINGS_FILE` is a unique, writable sidecar prefix. Each process appends to `<prefix>.<pid>.jsonl`; the wrapper also writes `<prefix>.wrapper.jsonl`. Never reuse the prefix between invocations.
- `CHRONICLE_RUNTIME_MODE`, `CHRONICLE_STORAGE_PROVIDER` and `CHRONICLE_INTEGRATION_FILTER` label the selected invocation. Keep them consistent with its actual arguments. Missing metadata is reported as unavailable, not guessed.

The recorder also includes `GITHUB_SHA` and `GITHUB_RUN_ATTEMPT` when present. Labels must contain identifiers only, never connection strings, credentials, SQL, parameter values or application data.

Prefix the **existing exact command** with `node .github/scripts/integration-timings.mjs run --phase build --` or `node .github/scripts/integration-timings.mjs run --phase test --`. The wrapper forwards arguments, standard output, standard error, exit status and termination signals. It does not add a build, remove a filter, alter a retry policy or start another test invocation.

For build attribution, collect MSBuild's `-clp:PerformanceSummary` output separately from test output. The parser recognizes the existing protobuf/client/kernel generation targets as inclusive worker subsets. A test command using `--no-build` performs no generation; missing build target records are unavailable, not zero.

The wrapper replays sidecars after the child exits, so passing tests remain observable when the runner captures console output. Upload the raw console log, TRX files and all matching sidecars, even on failure or cancellation. Out-of-process fixtures forward only timing metadata into the kernel; they stream timing-prefixed kernel lines into the host's log and sidecar. Capture is bounded to 16 KiB per line. Other kernel output is neither retained nor copied into timing artifacts. Truncated timing lines and sink failures produce an incomplete-report warning.

## Summarize the artifacts

Run the following from the repository checkout, supplying the paths of artifacts from the **same invocation**:

```sh
node .github/scripts/integration-timings.mjs summarize \
  --log artifacts/test.log \
  --build-log artifacts/build.log \
  --trx artifacts/results.trx \
  --jsonl artifacts/timings.jsonl \
  --require-phase initialize \
  --require-phase scenario \
  --require-phase cleanup \
  --markdown artifacts/phase-table.md \
  --json artifacts/phase-totals.json
```

Input options and `--require-phase` may be repeated. The phase inventory is producer-specific: the example is for client-fixture lifecycle measurements, not a SQL-only invocation. Every selected run/mode/provider/filter partition must contain every required phase. Without an explicit inventory, the CLI marks the report partial. Keep the inventory with the CI invocation contract; do not remove a required phase to turn a missing measurement green.

The summarizer deduplicates console, sidecar and TRX replays, including started/final records. Missing summaries, open scopes, conflicting records, malformed data, warnings, invalid TRX durations and missing required phases make completeness partial and return a nonzero status. A partial report is useful for diagnosis but is not evidence that all work was measured. It must not replace the original test verdict.

## Read the phase table

- **Inclusive time** includes nested work. Initialization includes host startup/bootstrap; scenario time includes waits. Do not add parent and child durations.
- **Exclusive time** subtracts the union of direct child intervals, clipped to the parent. Concurrent children are not subtracted twice.
- **Process-union time** merges overlapping intervals only within one clock domain. A process-unique clock-domain id keeps two containers with the same PID separate. Unions across processes are not wall time.
- **Wall time** comes only from the wrapper. Unaccounted wall time remains explicitly unavailable because kernel, runner and wrapper clocks are not aligned.
- **TRX test durations** are reported separately as inclusive test time, with mean, p50, p95 and maximum. Their sum is not elapsed suite time.
- **Generation targets** are inclusive worker subsets, not additive build-wall phases. A missing target is not evidence that generation was free.

The first lazy `Services` or `CreateClient` access on a factory records `host-silo-start-shared` or `host-silo-start-per-context`. Subsequent successful accesses record reuse separately. Factory allocation has its own phase and is not treated as host startup. A failed first access does not mark the host as reused. Per-context MongoDB host disposal is separate from scenario and storage cleanup.

SQL measurements separate container startup/disposal, database create/drop/template cloning, context construction, migration/table setup and command time. The spec-only EF observer tracks bounded sets of context/command ids and numeric durations; it retains no contexts, commands, connections, SQL or parameters. Its `ef-command-duration` aggregate counts completed commands and failures. It has no interval union or command percentiles: those cannot be reconstructed from a total. Missing EF observations or unfinished pairs are partial, not a successful zero-command measurement.

DEVELOPMENT-only kernel reset records a whole SQL wipe and individual target wipes without changing the reset statements. Target counts are explicit. SQLite reports tables actually dropped; PostgreSQL and SQL Server table counts remain unavailable because their existing server-side batches do not return those counts. Command sums and target-wipe durations overlap and must not be added.

Comparisons require the same candidate, configuration, filter, provider, kernel image and artifact lineage. A code-ready instrumentation change is not a CI measurement; report unavailable evidence rather than estimating a speedup.
