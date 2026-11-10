<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Chronicle chaos durability rig

An opt-in, single-node durability probe against the **real Chronicle kernel**. This is no-release developer tooling, deliberately outside `Chronicle.slnx` and PR CI. It is not `Cratis.Chronicle.Testing`, an in-memory substitute, or a benchmark. Phase 1 covers single `AppendOnce` events only; network proxies, transactions, constraints, and multi-silo faults are future work.

**Current verification status:** authored under a disk hold. Not built, restored, tested, or run yet. The commands below are for use **after the hold is lifted**, not evidence of a passing rig.

## Prerequisites

- .NET 10, Bash, Docker with Linux containers, and the repository's normal package feeds.
- macOS or Linux (`kill -STOP` / `kill -CONT` must be available).
- Free host ports **35000**, **27017**, **27018**. Never point this rig at an existing stack or production database. It creates an isolated network and dedicated kernel, replica-set MongoDB, and independent side-ledger MongoDB containers.
- Adequate disk space before publishing or starting Docker. On the shared development machine, stop below 25 GiB free; flag below 40 GiB.
- A local slim kernel image. Through the required phase runner, run:

```bash
~/.pi/agent/bin/pi-phase run --kind build -- bash Chaos/build-kernel-image.sh
```

The script restores the server for the Linux RID, invokes `Integration/LocalKernelPublish.proj`'s `PublishKernel` target, then builds `Docker/LocalSlim/Dockerfile`. ARM64 hosts use `linux-arm64`. An existing `cratis/chronicle:local-development-slim` image is reused; pass `--force` to rebuild. Reuse does not prove it matches the checkout.

## Run and reproduce

The program command (route execution through the required local phase runner on shared machines):

```bash
dotnet run --project Chaos/ChaosRig -- run --seed 42 --duration 2m
```

It prints the seed, run ID, output directory, and planned fault times before starting infrastructure. Omit `--seed` to generate one. Replay the printed seed, duration and fault selection against the same checkout/image to reproduce the **schedule**. Real process scheduling and transport timing remain nondeterministic.

| Option | Default | Meaning |
| --- | --- | --- |
| `--seed <integer>` | generated, printed | Deterministic fault selection and timing |
| `--duration <s/m/h>` | `2m` | Chaos window, excluding startup and settle |
| `--settle <s/m/h>` | `60s` | Bounded drain budget |
| `--faults <list>` | `all` | Comma-separated names below, or `none` |
| `--output <path>` | `Chaos/runs/<UTC>-<seed>` | Logs and snapshots; ignored by Git |
| `--keep-containers` | off | Retain containers/network and data for inspection |
| `--self-test-violation` | off | Remove one acknowledged event from the collected in-memory snapshot; must produce a dirty verdict and exit 2 |

Durations must be positive. A window too short for a fault plus recovery has an empty fault schedule. `--faults none` is the baseline smoke run.

## Faults

| Name | Action and purpose |
| --- | --- |
| `KillKernel` | SIGKILL, then restart the same container: crash across append/observer persistence |
| `KillMongo` | SIGKILL the kernel database, then restart: persistence and reconnect behavior |
| `StopKernel` | SIGSTOP, then SIGCONT: stale ownership and a frozen silo |
| `StopMongo` | SIGSTOP, then SIGCONT: blocked database writes without process exit |
| `KillProducer` | Kill/restart the producer process: resume a durable pending intention with the identical idempotency key and payload |
| `StopProducer` | SIGSTOP/SIGCONT: outstanding append/reconnect timing |
| `KillConsumer` | Kill/restart the consumer: re-registration and delivery progress |
| `StopConsumer` | SIGSTOP/SIGCONT: an unavailable observer client and later recovery |

Faults are globally serialized (thus never overlap on one target). Every fault has an explicit recovery inside the window. Holds last 16–21 seconds, crossing the kernel's explicitly configured **5-second single-writer lease TTL**. The independent ledger is never faulted. No artificial reactor exception is injected; redelivery after a crash is visible rather than hidden by deduplication.

## Workload and oracle

Before each append the producer durably journals a unique ledger ID, source, timestamp and numeric payload in the independent MongoDB container. It uses `AppendOnce` with that ID as the key. Success becomes `acked`; a failed response or transport exception becomes `unknown`; abrupt termination can leave `intended`. A restarted producer retries pending intentions with the original event and key. There is no lost-event assertion for an absent unknown/intended append: acknowledgement may never have happened.

The consumer registers a reactor, active reducer, and projection. The reactor inserts **every delivery** into an external side-ledger using a fresh document ID; duplicates are deliberately not suppressed. A duplicate here is a finding about externally visible redelivery, not an assumption that arbitrary reactor side effects are exactly-once. The reducer and projection count and sum contributions per source.

After the chaos window, recover targets, fence the producer by terminating it, and leave consumers running. The reader pages the actual event log, reads native observers/failures/jobs and materialized models through the client, and reads both independent ledgers. A narrow **read-only** BSON inspection of the kernel `observers` collections supplies `inFlightPartitions`, which the client API does not expose. It discovers databases instead of duplicating Chronicle's physical naming rules, matches the three exact observer IDs, and rejects missing/repeated documents or an absent/non-array field. It never writes kernel storage.

A clean verdict requires three consecutive clean snapshots: every acknowledged ID present, no duplicate event IDs, no sequence holes/repeated positions, matching source/payload intentions, all three observers active and at the observed tail, no failed/in-flight partitions or incomplete jobs, exact model counts/sums, and one side-ledger delivery per logged ID. Empty workloads cannot pass. Unknown IDs present once are allowed; twice are duplicates. Settle timeout cannot turn a transient clean sample into a pass.

Example output:

```text
VERDICT: clean (seed 42)
VERDICT: 1 lost, 0 duplicated, 0 gaps, 0 observers behind, 0 projections behind, 0 reducers behind, 0 failed partitions, 0 reactor duplicates (seed 42)
```

Additional drain/content failures are printed as `VIOLATION:` and included in `verdict.json`, even if the fixed summary counters are zero.

| Exit | Meaning |
| --- | --- |
| `0` | Complete, clean verdict |
| `2` | Complete snapshot with a durability/drain violation |
| `1` | Bad arguments, cancellation, infrastructure failure, or no complete snapshot |

These codes intentionally follow this rig's plan (not a general-purpose scanner's convention).

## Output and cleanup

The output directory contains `options.json`, `schedule.json`, stdout/stderr logs per child generation and container, `snapshot.json`, and `verdict.json`. A self-test additionally writes its modified snapshot; the original remains unchanged. Settle exceptions are logged separately. Do not commit run outputs; inspect/redact logs before sharing.

Normal completion, exceptions and Ctrl-C terminate owned child processes and remove labeled containers, anonymous data volumes, and the network. `--keep-containers` retains Docker resources but still terminates child processes. If the driver itself is killed, clean up explicitly:

```bash
dotnet run --project Chaos/ChaosRig -- cleanup --run-id <printed-run-id>
# Remove every rig-labeled run (destructive; includes retained data):
dotnet run --project Chaos/ChaosRig -- cleanup
```

Cleanup selects only the `chronicle-chaos` label. It does not touch unlabeled local stacks. A forced driver kill may also leave child processes; only terminate PIDs you can verify belong to your run.

## Verification and findings

After the hold lifts, build with the repository's approved iterate profile and run the pure `ChaosRig.Specs` executable (no containers). Then run a no-fault smoke, `--self-test-violation`, and seeds 1, 2, 3 for two minutes each, through the prescribed phase runner. Run repository Tier 1/2 gates before declaring PR readiness. None has run during the hold.

On a dirty verdict, preserve the seed, checkout/image version, flags, logs and snapshots. Reproduce with a smaller fault set/window, then report a minimized product issue with concrete expected/observed behavior and acceptance criteria. Do not fix Chronicle product code in this rig change. Distinguish at-least-once side-effect redelivery from acknowledged event loss. Never post raw connection details or sensitive logs. This phase does not add CI, publish images, or file findings automatically.
