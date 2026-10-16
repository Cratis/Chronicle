from pathlib import Path
import shlex

out = Path('/Volumes/sourcecode/repos/cratis/Chronicle/.ai-work/strong-consistency/pull-2026-10-16')
worktree = '/Volumes/sourcecode/repos/cratis/.worktrees/chronicle-4748-measure'
dll = worktree + '/Benchmarks/Chronicle.Benchmarks/bin/Debug/net10.0/Chronicle.Benchmarks.dll'
phase = '/Users/sindrewilting/.pi/agent/bin/pi-phase'
rows = []
commands = []

def add(kind, shape, observer, profile='on', stats='equivalent', lanes=64, wake='hint', purpose='', gates=''):
    scenario = f'{kind}:{observer}:{shape}'
    n = int(shape.split(':')[0])
    long = n >= 10000 or 'sparse9' in shape
    if long:
        deadline, reserve, drain, timeout = 720, 120, 300, 780
    elif kind == 'catchup':
        deadline, reserve, drain, timeout = 420, 100, 180, 480
    else:
        deadline, reserve, drain, timeout = 200, 100, 100, 240
    i = len(rows) + 1
    slug = f'matrix-{i:03d}-{kind}-{observer}-{shape.replace(":", "-").replace("=", "-")}-{profile}-{stats}-L{lanes}-{wake}'
    env = ['env', '-u', 'PULL_DROP_SEQ', '-u', 'PROBE_KERNEL_RESPONSE_TIMEOUT', '-u', 'PROBE_KERNEL_DEBUG',
           'CRATIS_CHRONICLE_LOCAL_IMAGE=cratis/chronicle:bench-4748', 'BENCH_MONGO_PORT=27448', 'BENCH_CHRONICLE_PORT=35448',
           f'PROBE_OUT={out}', 'PROBE_DRAIN_ALLOW_INFLIGHT=1', f'PROBE_DEADLINE_SECONDS={deadline}',
           f'PROBE_FINALIZE_SECONDS={reserve}', f'PROBE_DRAIN_SECONDS={drain}', f'PROBE_PROFILE={profile}',
           f'PULL_STATS={stats}', f'PULL_LANES={lanes}', 'PULL_PAGE=1000', 'PULL_FLUSH_MS=50', f'PULL_WAKE={wake}']
    cmd = f'cd {shlex.quote(worktree)} && PATH=/opt/homebrew/bin:$PATH ' + shlex.join(env + [phase, 'run', '--kind', 'integration', '--label', f'4748 matrix {i:03d} {kind} {observer}',
           '--timeout', str(timeout), '--queue-timeout', '2400', '--approved-long-timeout', '--log', str(out / (slug + '.log')), '--tail', '0', '--', 'dotnet', dll, 'probe', scenario])
    commands.append(cmd)
    mode = stats if observer == 'pull' else 'today' if kind != 'appendonly' else 'append-only twin'
    title = f'{scenario}; profile={profile}; {mode}; lanes={lanes}; wake={wake}'
    rows.append(f'{i}. **{title}** — {purpose} Feeds §7 {gates}. Internal total deadline {deadline}s = measurement/setup {deadline-reserve}s + finalization reserve {reserve}s; drain cap {drain}s; phase {timeout}s.\n\n   ```sh\n   {cmd}\n   ```\n')

for shape in ['2000:2000:64', '10000:10000:64', '10000:100:64', '10000:1:64']:
    repeats = 3 if shape in ['2000:2000:64', '10000:100:64'] else 1
    for profile in ['on', 'off']:
        for repeat in range(1, repeats + 1):
            for observer, stats in [('reactor', 'equivalent'), ('pull', 'equivalent'), ('pull', 'checkpoint')]:
                add('catchup', shape, observer, profile, stats, purpose=f'Fresh adjacent catch-up pair/control, repeat {repeat}/{repeats}; one-key row supplies transport ceiling.',
                    gates='correctness, projected full/post-registration throughput, 10k completion/key insensitivity, equivalent writes, durable gap, CPU')
            if repeat == 1 and shape in ['2000:2000:64', '10000:100:64']:
                for stats in ['equivalent', 'checkpoint']:
                    add('catchup', shape, 'pull', profile, stats, lanes=1, purpose='Single-lane dispatch control adjacent to same-engine/key-count pair.',
                        gates='client-bound/lanes diagnosis; correctness; throughput attribution')

for shape in ['1000:1:1', '1000:1:16', '1000:1000:16', '1000:1:1:batch']:
    repeats = 3 if shape == '1000:1:16' else 1
    for profile in ['on', 'off']:
        add('appendonly', shape, 'reactor', profile, purpose='Fresh append-only twin; subtract only matching mode/shape from live totals; batch terminal append cost cancels under inherited ruling.',
            gates='total live observer cost; append-side floor; transaction cross-check')
        for repeat in range(1, repeats + 1):
            for observer, stats in [('reactor', 'equivalent'), ('pull', 'equivalent'), ('pull', 'checkpoint')]:
                add('live', shape, observer, profile, stats, purpose=f'Fresh live comparison, repeat {repeat}/{repeats}; profile-off supplies decisive timing/lag, profile-on cost.',
                    gates='correctness, equivalent live writes, live reads, live-minus-twin cost <=30%, durable/effect lag, CPU')
            if repeat == 1 and shape in ['1000:1:1', '1000:1:16']:
                for stats in ['equivalent', 'checkpoint']:
                    add('live', shape, 'pull', profile, stats, wake='poll', purpose='Polling-only wake control beside hint-driven pair, without optimistic append acknowledgement wake.',
                        gates='live reads, durable/effect lag; optimistic-wake sensitivity; correctness')

for shape in ['300:1:1:rate=20', '300:1:1:rate=200', '1000:1:16:rate=1000']:
    for profile in ['on', 'off']:
        add('appendonly', shape, 'reactor', profile, purpose='Rate-matched append-only twin for paced cost subtraction.', gates='live-minus-twin cost; arrival-rate control')
        for observer, stats in [('reactor', 'equivalent'), ('pull', 'equivalent'), ('pull', 'checkpoint')]:
            add('live', shape, observer, profile, stats=stats, purpose='Matched paced arrivals expose timer-driven writes; profile-off pairs decide timing/lag, profile-on supplies cost; report attained append rate.',
                gates='equivalent writes/event <=2 at20/s or <=0.2 at>=200/s; reads; cost; lag; correctness')

for profile in ['on', 'off']:
    for observer, stats in [('reactor', 'equivalent'), ('pull', 'equivalent'), ('pull', 'checkpoint')]:
        add('catchup', '2000:2000:64:sparse9', observer, profile, stats, purpose='Nine unrelated events before each relevant event,20000 total; no server-side type filter in pull.',
            gates='selectivity relevant-event throughput >=3x; no unexpected unrelated delivery; correctness/cost')

for stats in ['equivalent', 'checkpoint']:
    add('catchup', '10000:100:64:overlap1000', 'pull', stats=stats, purpose='Extra unmapped #4583-shaped overlap:10000 seeded events plus1000 live appends while catch-up reads.',
        gates='correctness/zero loss during catch-up+live overlap only; not a step1 performance comparison')

header = '''# Step 2 matrix — orchestrator-only ordered commands

**Pending blocker:** corrected live10:1:1 smoke is exact10/10 but has11 reads through DurableMs (1.1/event), above the requested <=1 gate. Short-page immediate rereads are fixed and self-tested; the remaining overhead has not been traced to individual reads. Do not call this gate passed or silently subtract idle reads. Resolve with the orchestrator before matrix execution.

Generated by make-run-list.py for the corrected harness; no regenerated matrix rows have been run. The old harness row001 is retained in status-pre-fix.tsv and results.jsonl as historical evidence, not a corrected-harness result. Per-row scripts runs/NNN.sh contain exactly a bash shebang and the corresponding standalone command.
Run exactly one at a time in completion-notifying background execution, preserving each producer exit and unique log. Every command is standalone (explicit cd/env, one pi-phase invocation). Check branch/HEAD against handoff.md, rebuild if source inputs changed, check ports27448/35448 and disk before starting. Kernel d5d0eea66d, image cratis/chronicle:bench-4748 unchanged; no kernel response-timeout override. All commands explicitly remove planted loss/debug/forced-timeout variables. No push/GitHub/production edits.

Do not run later dependent rows after a pull correctness failure (missing/dup/order/unexpected/payload/gap/tail), two unchanged INVALID causes or unknown mutation/cleanup outcome. A real correctness failure is DEFECT/exit1, including typed tail/per-key/handled/gap failures even when finalization later fails. The planted drop smoke is not a matrix row: EXPECTED_DROP/exit0 ONLY for exactly the requested missing position, no other delivery defects, exact tail/counts and successful setup/attribution/cleanup. It is excluded from performance samples. A wrong/additional failure is DEFECT/exit1; setup/attribution failures remain INVALID/exit2. #4954 JOB_FAILED/DID_NOT_FINISH on today's catch-up side is a retained engine finding, not permission to alter the timeout or hide the disposition. Admission75 is not a benchmark result; the user authorized up to10 attempts/phase, retries may reuse the same log name (only exit75 is retried). Measurement clocks exclude seeding/startup but the internal deadline includes them; long budgets retain step1's720s/300s-drain envelope.

Coverage: both pull stats modes; adjacent same-engine controls;3 repeats of2k/2k,10k/100 and live1:16 in each profile mode; fresh twins; one-key ceiling; lanes1; paced20/200/1000; sparse9; profile-off decisive timings; poll wake; catch-up+live overlap. Noop variants were removed by final step1 ruling; catchup:projection is N/A for this experiment and deliberately absent. Repeats are prescribed by §6, not reassurance runs. Do not count timing-only Mongo=null as zero cost.

Accounting: inherited Mongo profiler rows/classifier stay unchanged. Mongo emits a profiler update record per statement in a bulk, not per wire command. PullWireCommands counts actual dedicated-client wire attempts (including split bulks/retries) over the exact measurement clock window; PullCounts/handled and profiler document counts prove work. Use actual wire writes for §7's commands/event gate, and report documents separately. Each profiled pull row also exposes Mongo.pullCommandAccounting: inert per-wire query $comment identifiers group update statements INSIDE THE SAME profiler boundaries. Its writes/writeStatements must exactly match the dedicated-client wire writes/documents or the row is INVALID (split/retry/boundary loss is not silently underestimated). Its adjustedCommands replaces only coordinator update statements with proven wire writes; all reads and non-coordinator/system/shared commands retain the inherited totals. Use this boundary-aligned adjusted total for live-minus-matching-twin cost, show the inherited raw totals too. The final multi-key smoke proved11statements=2wirewrites, raw39commands=adjusted30. Never mix clock windows or treat missing reconciliation as GO. Profile-off reactor catch-up uses a jobs replace/delete change stream for the same full-success/deletion proof, with the profiler never enabled. Smokes validate that path; no crash/failover/fencing claim.

For timing comparisons use COMPLETE_TIMING_ONLY rows. **Post-registration ev/s is the deciding catch-up figure**, capped by today's fresh1-key post-registration ceiling; full ev/s remains reported as context, not a second registration-asymmetric deciding figure. Registration observation is itself an approximate boundary and today's work may start before its definition is observed. Medians only over valid samples; list all failed/invalid dispositions. For the live read gate use pullReadsAtDurable / expected (also emitted as pullReadsPerEventAtDurable), measured from clock start to DurableMs. Report pullReads alongside it: the full clock window including quiescence, excluding warmup; idle finalization polls are not reads needed for durable delivery. GapHolds counts distinct missing positions; GapRetries counts5ms retries. The flusher still wakes every1ms, an optimistic client-CPU caveat; client CPU includes that polling. Profile-on rows supply costs and transaction cross-checks; terminal transaction cost remains NOT PROFILED. CPU start/end snapshots retain combined cgroup and per-process kernel/mongod ticks (Hz included), client CPU separately. A single10k/10k/control/sparse sample is not a three-run median; label sample counts and uncertainty honestly.

'''
(out / 'run-list.md').write_text(header + f'{len(rows)} commands below. Each feeds §7 correctness where applicable; timing-only rows waive only Mongo-cost evidence, never membership/payload/tail/job proof.\n\n' + '\n'.join(rows))
runs = out / 'runs'
runs.mkdir(exist_ok=True)
for i, cmd in enumerate(commands, 1):
    (runs / f'{i:03d}.sh').write_text('#!/bin/bash\n' + cmd + '\n')
print(f'Wrote {len(rows)} exact commands to {out / "run-list.md"} and runs/NNN.sh')
