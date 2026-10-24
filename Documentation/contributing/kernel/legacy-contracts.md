---
title: "Legacy contract matrix"
description: "Read the compatibility report, connect an owner issue to executable proofs, and distinguish missing coverage from a compatible release."
---

A wire comparison can pass while an old client still behaves incorrectly. The legacy contract matrix keeps those claims separate: it connects each compatibility promise to owner issues, required jobs, selected specs, and named planted defects in `.github/legacy-contracts.json`.

The manifest currently uses **report** enforcement. Green runnable rows are covered; pending rows are gaps, not exemptions or evidence of compatibility. Report mode does not make the matrix a release certification.

## Read the report

The .NET build calls `legacy-contracts.yml` after wire, grain, non-SQL, and SQL specs finish. The job builds only `Source/Tools/LegacyContracts`, downloads the existing TRX artifacts, and reads issue states using a read-only token. It does not rebuild the solution or rerun specs.

| Row | Claim | Current coverage |
| --- | --- | --- |
| W | Wire contract and Connect compatibility | Runnable, including five mutations of the real descriptor |
| G | Orleans grain compatibility during rolling upgrades | Runnable; separate from persisted generation-floor admission |
| O | Startup admission against persisted generation floors | Runnable, with in-memory persistence and incompatible-sibling refusal specs |
| C1 | Released .NET client behavior against the new kernel | Pending owner proof |
| C2 | Kotlin, TypeScript, Elixir, and Python client behavior | Pending owner proof |
| D | Disabled or unadopted stores retain legacy behavior | Pending owner proof |
| R | Reducer capability refusal and generation isolation | Pending owner proof |
| P | Provider parity, fencing, or explicit refusal | Pending owner proof |
| S | Delayed stale DDL, cleanup, and commands are fenced | Pending owner proof |

Each runnable selector must match at least its declared minimum number of distinct fully qualified tests. Every selected result must pass; skipped tests do not count as proof. A selector matches its exact name or a namespace/class boundary followed by a dot, not an arbitrary substring. Repeated results for the same test do not inflate the minimum; any non-passing execution makes that test non-passing.

Each planted-defect declaration also names an exact passing spec. The wire tool plants a removed field, removed method, changed field type, unary-to-streaming change, and removed enum value in an in-memory copy of the committed descriptor. The matrix self-test separately plants missing owners, invalid ownership, failed/skipped tests, empty matches, insufficient counts, missing proofs, stale pending rows, skipped/failed jobs, and unavailable inputs. Neither self-test edits generated contracts.

## Interpret a verdict

| Verdict | Meaning | Report mode | Release mode |
| --- | --- | --- | --- |
| Green | All declared executable proofs passed | Pass | Pass |
| Unowned | No owner issue, a pull request used as an owner, or an owner closed without completion | Fail | Fail |
| Red | Required job failed/cancelled, a selected test did not pass, a minimum was missed, or a planted spec is missing | Fail | Fail |
| StalePending | A pending row has no open owner left; all owners completed without wiring its proofs | Fail | Fail |
| Pending | At least one valid owner remains open and proof is not yet wired | Warning | Fail |
| Skipped | A required job skipped, including wire's explicit skipped output | Warning | Fail |

Completed owner issues remain valid for runnable rows. Closed-as-not-planned or duplicate issues do not. A pending row remains pending while at least one valid owner is open; it becomes stale when all complete.

The tool exits `0` for the active mode's passing policy, `1` for a policy failure, and `2` when it cannot evaluate. Malformed inputs, missing TRX files, missing job results, and failed issue lookups must never become green results. Job skipping cannot excuse absent or failing test proofs.

## Wire a pending row

1. Keep its issue numbers in `owners`. Use real Chronicle issues, never pull request numbers. The evaluator consumes their current state, completion reason, and pull-request marker.
2. Implement the row's `requiredProofs` in the owning behavior. Add specs that plant the defect and demonstrate the expected refusal or unchanged behavior.
3. Ensure the existing upstream spec jobs execute them and upload their TRX results.
4. Set `status` to `runnable`. Declare the upstream job IDs, fully qualified selectors with positive minimum counts, and exact planted-spec names. Clear `requiredProofs` only after wiring those proofs.
5. Read the matrix report. A closed owner alone does not satisfy a proof, and a namespace with no tests fails rather than silently passing.

Do not lower selector counts or remove planted proofs merely to get a green check. Update them only when the executable contract genuinely changes.

## Run the evaluator locally

Use .NET 10 and saved inputs from the same build. `issues.json` is an array of objects with `number`, uppercase `state`, nullable uppercase `stateReason`, and `isPullRequest`. `jobs.json` maps upstream job IDs to `result` and nullable `outputs`; it has the shape of GitHub's `toJSON(needs)`. Results are lowercase, such as `success` or `skipped`. The TRX directory can contain nested project and artifact directories.

From the repository root:

```bash
dotnet run --project Source/Tools/LegacyContracts -c Release -- --self-test
dotnet run --project Source/Tools/LegacyContracts -c Release -- \
  --manifest .github/legacy-contracts.json \
  --issues issues.json \
  --jobs jobs.json \
  --trx TestResults
```

The evaluator reports each row's verdict, test count, planted-proof count, and reason. Add `--github` for annotations and the step summary. `--mode release` evaluates strict policy locally without changing the manifest; it fails while pending or skipped rows remain. This increment leaves the committed policy in report mode.

For a manual GitHub run, supply a build run ID containing the specs artifacts and that run's upstream jobs JSON. Use matching source and results; a manual run without those artifacts cannot evaluate compatibility. Publish wiring, review ownership, and required-check policy are outside this increment.
