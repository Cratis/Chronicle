# Observer delivery experiment (Cratis/Chronicle#4748)

Experimental branch only, not for merging into `main`. It holds the measurement harness and the benchmark-only "ordered reader" (pull coordinator) used to decide whether the observer delivery redesign in decision 0004 is worth building.

## What is here

- `Benchmarks/Chronicle.Benchmarks/Probe*.cs`, `ProbePull*.cs`, `ProbeSystemDrain.cs`: the probe.
  - It does exact per-event delivery accounting (loss, duplicates, order and payload) from acknowledged append results.
  - It attributes MongoDB profiler operations to the probe, system or shared owner, scoped to the run.
  - It records effect, persisted-progress and job clocks.
- The pull coordinator: one ordered MongoDB reader, per-key lanes, and a contiguous-prefix watermark with batched progress writes. It has two progress modes: `PULL_STATS=checkpoint` and `PULL_STATS=equivalent`, which also writes per-key handled counts.
- Results and plans from the run, in this folder:
  - `step1-results.md`, `step1-diagnosis-*.md`: today's engine baseline.
  - `step2-plan.md`: the experiment design and its §7 pass/fail gates.
  - `step2-results-interim.md`: the interim step-2 analysis of rows 1–72.
- Scripts:
  - `make-run-list.py` generates the 140-row run list. Its paths are absolute and must be edited for your checkout.
  - `run-matrix.sh` is a sequential driver. It stops on a delivery defect (exit 1) or after 3 consecutive failures.
  - `analyze-pull.py` evaluates the §7 gates from `results.jsonl`.

## How to run

The probe runs against a locally built server image (`cratis/chronicle:bench-4748`, a Debug `linux-arm64` publish of `Source/Kernel/Server`, see #4842) on task-unique ports:

```sh
CRATIS_CHRONICLE_LOCAL_IMAGE=cratis/chronicle:bench-4748 BENCH_MONGO_PORT=27448 BENCH_CHRONICLE_PORT=35448 PROBE_OUT=<output folder> \
  dotnet Benchmarks/Chronicle.Benchmarks/bin/Debug/net10.0/Chronicle.Benchmarks.dll probe <kind>:<observer>:<events>:<keys>:<concurrency>[:batch]
```

- `kind` is one of `live`, `catchup` or `appendonly`.
- `observer` is one of `reactor`, `projection` or `pull`.
- Other settings come from environment variables (`PROBE_*`, `PULL_*`), documented in `step2-plan.md` and `make-run-list.py`.

Exit codes: 0 complete, 1 delivery defect, 2 invalid or did not finish.

## Status (2026-10-16)

- **Step 1 (today's engine) is done.** It found #4954, #4972, #4973 and #4974.
- **Step 2 is incomplete.**
  - 83 of 140 rows ran before the shared machine became too loaded for valid runs.
  - The interim verdict is **not GO**:
    - catch-up gates pass (about 14 times faster at 2,000 keys; at 10,000 keys it completes at about 5,800 events/s, where today's engine does not finish);
    - live gates fail (reads 1.001–1.003 per event, observer-only live cost 119% of today's, durable gap 243–506 ms against a 200 ms limit).
  - The interim rows were measured on a contended machine with profiling on, so they are indicative only.

The follow-up to finish step 2 is tracked in its own issue, linked from #4748.
