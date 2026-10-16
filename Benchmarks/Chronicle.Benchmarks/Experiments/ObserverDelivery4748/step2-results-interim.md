# Step 2 pull coordinator — interim/offline matrix analysis

Snapshot: run-list rows 001–072; 72/140 status-completed rows; 90 JSONL records read; 0 partial trailing records ignored. Engine d5d0eea66d; harness e27b3ff5ab per handoff. No build, benchmark, process control, GitHub operation, or source edit performed.

## Immediate flags

- Coverage cannot recover within remaining matrix: catchup:reactor:10000:100:64 / off / today has 2/3 valid repeats (040,045,048). Metrics use the valid-sample median, not an invented third sample.
- No captured pull structural correctness failure. Performance FAIL is not delivery DEFECT.
- Observed gate failures: Live reads/event <=1 (1:1); <=0.3 (:16); Live-minus-matching-twin cost <=30% of today; Durable gap <=FLUSH_MS+150ms. Remaining different scenarios cannot replace completed single-sample/complete-repeat results; unfinished repeat medians remain provisional.

## Section 7 gates

| Gate | Verdict | Evidence (run-list rows) |
| --- | --- | --- |
| Correctness / validity, every run | **INSUFFICIENT** | 69/72 valid; invalid/failed 023,026,045; 68 pending; pull structural defects none |
| 2k projected >=10x (equivalent) | **PASS** | catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: 14.31 >= 10 (013,018,021,054,012,017,020) |
| Avoid STOP: 2k projected >=5x (equivalent) | **PASS** | catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: 14.31 >= 5 (013,018,021,054,012,017,020) |
| 10k finishes; projected >=min(2000,ceiling) (equivalent) | **PASS** | catchup:pull:10000:10000:64 / off / equivalent / L64 / hint: 5,761 >= 2,000 (027,054) |
| 10k/100-key rate >=0.5 (equivalent) | **PASS** | catchup:pull:10000:10000:64 / off / equivalent / L64 / hint: 0.939 >= 0.5 (027,041,046,049) |
| 100 keys >=today (equivalent) | **PASS** | catchup:pull:10000:100:64 / off / equivalent / L64 / hint: 5.027 >= 1 (041,046,049,040,045,048) |
| Sparse relevant rate >=3x (equivalent) | **INSUFFICIENT** | catchup:pull:2000:2000:64:sparse9 / off / equivalent / L64 / hint: n/a >= 3 (none); repeats/pairs pending 137,136 |
| 2k projected >=10x (checkpoint) | **PASS** | catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint: 14.31 >= 10 (014,019,022,054,012,017,020) |
| Avoid STOP: 2k projected >=5x (checkpoint) | **PASS** | catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint: 14.31 >= 5 (014,019,022,054,012,017,020) |
| 10k finishes; projected >=min(2000,ceiling) (checkpoint) | **PASS** | catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint: 5,761 >= 2,000 (028,054) |
| 10k/100-key rate >=0.5 (checkpoint) | **PASS** | catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint: 1.015 >= 0.5 (028,042,047,050) |
| 100 keys >=today (checkpoint) | **PASS** | catchup:pull:10000:100:64 / off / checkpoint / L64 / hint: 5.264 >= 1 (042,047,050,040,045,048) |
| Sparse relevant rate >=3x (checkpoint) | **INSUFFICIENT** | catchup:pull:2000:2000:64:sparse9 / off / checkpoint / L64 / hint: n/a >= 3 (none); repeats/pairs pending 138,136 |
| Equivalent catch-up writes/event <=0.02 | **INSUFFICIENT** | catchup:pull:2000:2000:64:sparse9 / on / equivalent / L64 / hint: n/a <= 0.02 (none); repeats/pairs pending 134; catchup:pull:10000:100:64:overlap1000 / on / equivalent / L64 / hint: n/a <= 0.02 (none); repeats/pairs pending 139 |
| Equivalent live writes/event <=0.2 (>=200 paced; 1:16) | **INSUFFICIENT** | live:pull:1000:1:16 / on / equivalent / L64 / hint: 0.11 <= 0.2 (071); repeats/pairs pending 076,079; live:pull:1000:1:16 / on / equivalent / L64 / poll: n/a <= 0.2 (none); repeats/pairs pending 073; +2 insufficient comparisons (details below) |
| Equivalent paced20 writes/event <=2 | **INSUFFICIENT** | live:pull:300:1:1:rate=20 / on / equivalent / L64 / hint: n/a <= 2 (none); repeats/pairs pending 111 |
| Live reads/event <=1 (1:1); <=0.3 (:16) | **FAIL** | live:pull:1000:1:1 / on / equivalent / L64 / hint: 1.001 <= 1 (059); live:pull:1000:1:1 / on / checkpoint / L64 / hint: 1.003 <= 1 (060); +4 fail comparisons (details below); 6 other comparisons insufficient |
| Live-minus-matching-twin cost <=30% of today | **FAIL** | live:pull:1000:1:1 / on / equivalent / L64 / hint: (5.218−3.353)/(4.921−3.353)=1.189 (059,058,057); live:pull:1000:1:1 / on / checkpoint / L64 / hint: (4.763−3.353)/(4.921−3.353)=0.8992 (060,058,057); +4 fail comparisons (details below); 12 other comparisons insufficient |
| Avoid STOP: equivalent live writes <today | **INSUFFICIENT** | live:pull:1000:1:16 / on / equivalent / L64 / hint: pull 0.11 vs today 0.847 writes/event (071,070); live:pull:1000:1:16 / on / equivalent / L64 / poll: pull n/a vs today 0.847 writes/event (070); +5 insufficient comparisons (details below) |
| Durable gap <=FLUSH_MS+150ms | **FAIL** | catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: 242.9 <= 200 (013,018,021); catchup:pull:10000:10000:64 / off / equivalent / L64 / hint: 505.7 <= 200 (027); 16 other comparisons insufficient |
| Live effect−append <=today OR <=100ms | **INSUFFICIENT** | live:pull:1000:1:16 / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none); live:pull:1000:1:16 / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none); +12 insufficient comparisons (details below) |

**Decision: INVESTIGATE — not GO; pending/invalid evidence remains.** Missing evidence is never a pass. The plan limits investigation to two days; this script does not infer its start time.

## Three exit-2 rows

- **023** (catchup:reactor:10000:10000:64, profile=on, JSONL 39): DID_NOT_FINISH/exit 2; 10000/10000 exact deliveries, but no durable completion within the 600s measurement budget. Probe job captured status 7, 1590 successful/3797 failed of 10000 steps. Confirmed today-engine #4954 signature: retained probe-step exceptions name the default 30s IObserver.ReportHandledEvents timeout. These are not pull failures and are excluded from performance medians; teardown status is not success.
- **026** (catchup:reactor:10000:10000:64, profile=off, JSONL 42): DID_NOT_FINISH/exit 2; 10000/10000 exact deliveries, but no durable completion within the 600s measurement budget. Probe job captured status 4, 594 successful/5377 failed of 10000 steps. Confirmed today-engine #4954 signature: retained probe-step exceptions name the default 30s IObserver.ReportHandledEvents timeout. These are not pull failures and are excluded from performance medians; teardown status is not success.
- **045** (catchup:reactor:10000:100:64, profile=off, JSONL 61): INVALID/exit 2 **during seeding**, gRPC Cancelled: `gRPC call disposed`, after 1770/10000 acknowledged appends; system drain/observer registration/measurement never ran (wallMs=0). Timing-only final check also timed out. Unknown outstanding RPC outcomes, not measured delivery loss. The retained kernel log ends after startup/authentication, with no caught #4954 ReportHandledEvents/job-failure evidence. **Today-side setup/transport failure, root cause unproven; do not attribute it to #4954 or pull.**

## Projected catch-up: deciding post-registration and full-clock context

| Mode / thin-key shape | Pull post-reg | Today 1-key ceiling post-reg | Projected post-reg | Projected/today post-reg | Projected full (context only) | Projected/today full (context only) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| equivalent / 2000:2000:64 | 9.199e+04 | 5,761 | 5,761 | 14.31 | 4,232 | 11.95 |
| equivalent / 10000:10000:64 | 8.696e+04 | 5,761 | 5,761 | n/a | 4,232 | n/a |
| checkpoint / 2000:2000:64 | 1.18e+05 | 5,761 | 5,761 | 14.31 | 4,232 | 11.95 |
| checkpoint / 10000:10000:64 | 9.845e+04 | 5,761 | 5,761 | n/a | 4,232 | n/a |

## Method and limitations

- VALID = exit0 + COMPLETE/COMPLETE_TIMING_ONLY + captured membership, payload, append, observer, tail/count and profiler reconciliation checks. INVALID, DID_NOT_FINISH, JOB_FAILED, DEFECT and EXPECTED_DROP are never performance samples. Historical smokes and the stopped pre-fix attempt are excluded by status cutoff, scenario/options matching, completion windows and occurrence order; no fixed JSONL offset is assumed. Mapping errors fail closed. Untimed setup failures use physical order between neighboring timed runs.
- Group by full scenario, profile mode, stats mode, lanes, wake, page and flush. Values are median [min–max] of valid samples, not confidence intervals. Pending repeats prevent a PASS; an already observed threshold violation is shown as FAIL but may change when repeats finish. Failed prescribed repeats lower n and are explicitly flagged; remaining valid medians are still computed.
- Timing gates use profile-off COMPLETE_TIMING_ONLY, post-registration ev/s. Projected thin-key rate = min(pull median post-reg, fresh today 1-key median post-reg). The 100-key and sparse ratios use the uncapped relevant-event rate. Full ev/s is context only, per the handoff superseding the earlier full-clock gate. Registration observation is approximate and can lag today's start of work. L1 and poll controls remain separate; no pooling with default L64/hint.
- Live reads/event = pullReadsAtDurable/expected; full-window reads are shown separately, never silently subtracted. Progress writes/event = actual dedicated-client wire attempts/expected, including handled-count bulks in equivalent mode. Although the wire snapshot ends after quiescence, for VALID finite exact-tail runs all Save writes precede DurableMs: W is then at final tail, Save requires W movement, and Dispose writes nothing (harness/ProbePull.cs:139–178; ProbePullMeasure.cs:120–159). There are no per-command timestamps in JSONL; this equality depends on that harness invariant, not on treating quiescent reads as zero. Timing-only wire counters are diagnostic, not profiler cost proof.
- Cost gates use profiled equivalent writes. For total live cost, adjustedCommands replaces ONLY proven coordinator update statements with reconciled wire writes. All reads/system/shared operations remain; inherited raw totals are retained below. Subtract the exact matching fresh shape/profile twin from BOTH pull and today medians. Nonpositive today residual or negative pull residual is INSUFFICIENT/noise, never clamped to zero. Twins are single samples; subtraction includes run-to-run system-observer variance.
- Docs/event are document write attempts, not unique keys or modified documents: pull dedicated-client documentsWritten includes checkpoints and per-key targets; today uses probe-owned update/remove profiler statements plus inserted documents. The profiler's upsert inserted-document field is not reliable, so it is not used to claim exact physical insert counts. Today's catch-up probe-owned write counts exclude shared job machinery; all-write profiler statement counts are also shown. Terminal transaction cost is NOT PROFILED. Batch transaction counters remain checked by the inherited harness; no crash/failover/fencing proof is claimed.
- CPU snapshots span the measurement-through-quiescence window, not just effects; container cgroup and per-process kernel/mongod CPU are separate from client CPU (which includes pull). The 1ms flusher polling remains an optimistic CPU caveat. Step 1 RESULTS.md is context only (mixed generations/profile-on); it is not substituted for fresh paired baselines. Noop removed; projection N/A per run-list.

## Per-scenario valid timing and CPU

All time and CPU values are ms; CPU is consumed CPU time, not elapsed time.

| Scenario / profile / mode / lanes / wake | valid/planned; evidence | full ev/s | post-reg ev/s | registration | effect | durable | durable gap | drain | client CPU | client cores | cgroup CPU | kernel CPU | mongod CPU |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| catchup:reactor:2000:2000:64 / on / today | 3/3; 001,006,009 | 260.5 [223.3–446.3] | 286.7 [242.7–497.4] | 700.7 [459.7–715.1] | 7,677 [4,481–8,955] | 3.017e+04 [1.761e+04–3.742e+04] | 2.249e+04 [1.313e+04–2.846e+04] | 0 [0–0] | 2,588 [1,924–2,927] | 0.08028 [0.07413–0.09789] | 4.891e+04 [3.446e+04–5.389e+04] | 2.872e+04 [2.175e+04–3.222e+04] | 2.016e+04 [1.27e+04–2.164e+04] |
| catchup:pull:2000:2000:64 / on / equivalent / L64 / hint | 3/3; 002,007,010 | 3.133e+04 [1.468e+04–4.795e+04] | 5.331e+04 [3.616e+04–8.47e+04] | 26.33 [18.09–80.91] | 63.84 [41.71–136.2] | 326 [265.6–489.1] | 262.1 [223.9–352.9] | 0 [0–0] | 353.9 [276.1–365.3] | 0.1357 [0.1128–0.155] | 232.9 [225.1–234.1] | 30 [10–40] | 190 [190–200] |
| catchup:pull:2000:2000:64 / on / checkpoint / L64 / hint | 3/3; 003,008,011 | 3.205e+04 [2.509e+04–3.48e+04] | 5.587e+04 [5.277e+04–6.261e+04] | 25.54 [24.51–43.92] | 62.41 [57.48–79.72] | 225.9 [170.2–290.1] | 168.4 [107.8–210.3] | 0 [0–0] | 299.2 [256.7–346.3] | 0.131 [0.1078–0.1412] | 110.8 [106.3–215.6] | 30 [20–60] | 80 [70–130] |
| catchup:pull:2000:2000:64 / on / equivalent / L1 / hint | 1/1; 004 | 2.055e+04 [2.055e+04–2.055e+04] | 3.618e+04 [3.618e+04–3.618e+04] | 42.04 [42.04–42.04] | 97.32 [97.32–97.32] | 371.8 [371.8–371.8] | 274.4 [274.4–274.4] | 0 [0–0] | 290.1 [290.1–290.1] | 0.1164 [0.1164–0.1164] | 253.2 [253.2–253.2] | 60 [60–60] | 180 [180–180] |
| catchup:pull:2000:2000:64 / on / checkpoint / L1 / hint | 1/1; 005 | 1.95e+04 [1.95e+04–1.95e+04] | 4.457e+04 [4.457e+04–4.457e+04] | 57.66 [57.66–57.66] | 102.5 [102.5–102.5] | 231.3 [231.3–231.3] | 128.7 [128.7–128.7] | 0 [0–0] | 249.4 [249.4–249.4] | 0.1063 [0.1063–0.1063] | 101.7 [101.7–101.7] | 20 [20–20] | 60 [60–60] |
| catchup:reactor:2000:2000:64 / off / today | 3/3; 012,017,020 | 354.1 [198.8–540.3] | 402.7 [212.4–639.9] | 645.9 [575.7–681.2] | 5,648 [3,701–1.006e+04] | 1.496e+04 [1.126e+04–2.22e+04] | 9,315 [7,563–1.214e+04] | 0 [0–553.5] | 2,901 [2,862–3,079] | 0.1706 [0.1268–0.2151] | 2.826e+04 [2.82e+04–3.312e+04] | 1.929e+04 [1.927e+04–2.233e+04] | 8,970 [8,910–1.077e+04] |
| catchup:pull:2000:2000:64 / off / equivalent / L64 / hint | 3/3; 013,018,021 | 3.334e+04 [2.117e+04–3.549e+04] | 9.199e+04 [5.608e+04–1.217e+05] | 39.92 [38.24–58.83] | 59.99 [56.35–94.49] | 307.7 [302.9–324.1] | 242.9 [229.6–251.4] | 0 [0–0] | 313 [307.3–346.9] | 0.1276 [0.1273–0.1425] | 174.4 [167.4–192.9] | 40 [30–50] | 110 [110–140] |
| catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint | 3/3; 014,019,022 | 6.501e+04 [1.322e+04–8.863e+04] | 1.18e+05 [2.726e+04–1.54e+05] | 13.81 [9.577–77.92] | 30.76 [22.57–151.3] | 133.8 [124.8–297.9] | 103 [102.3–146.6] | 0 [0–0] | 336.1 [322.6–366.4] | 0.1507 [0.119–0.1685] | 141.1 [106.7–319.8] | 80 [40–170] | 60 [50–70] |
| catchup:pull:2000:2000:64 / off / equivalent / L1 / hint | 1/1; 015 | 3.903e+04 [3.903e+04–3.903e+04] | 8.639e+04 [8.639e+04–8.639e+04] | 28.1 [28.1–28.1] | 51.25 [51.25–51.25] | 170.8 [170.8–170.8] | 119.6 [119.6–119.6] | 0 [0–0] | 297.2 [297.2–297.2] | 0.1296 [0.1296–0.1296] | 164.6 [164.6–164.6] | 40 [40–40] | 110 [110–110] |
| catchup:pull:2000:2000:64 / off / checkpoint / L1 / hint | 1/1; 016 | 2.789e+04 [2.789e+04–2.789e+04] | 6.043e+04 [6.043e+04–6.043e+04] | 38.62 [38.62–38.62] | 71.72 [71.72–71.72] | 193.2 [193.2–193.2] | 121.5 [121.5–121.5] | 0 [0–0] | 300.8 [300.8–300.8] | 0.1321 [0.1321–0.1321] | 106.6 [106.6–106.6] | 30 [30–30] | 60 [60–60] |
| catchup:reactor:10000:10000:64 / on / today | 0/1; 023 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:10000:10000:64 / on / equivalent / L64 / hint | 1/1; 024 | 8.284e+04 [8.284e+04–8.284e+04] | 1.04e+05 [1.04e+05–1.04e+05] | 24.6 [24.6–24.6] | 120.7 [120.7–120.7] | 922.3 [922.3–922.3] | 801.6 [801.6–801.6] | 0 [0–0] | 854.5 [854.5–854.5] | 0.2673 [0.2673–0.2673] | 890.8 [890.8–890.8] | 40 [40–40] | 840 [840–840] |
| catchup:pull:10000:10000:64 / on / checkpoint / L64 / hint | 1/1; 025 | 1.385e+05 [1.385e+05–1.385e+05] | 1.597e+05 [1.597e+05–1.597e+05] | 9.581 [9.581–9.581] | 72.18 [72.18–72.18] | 171.9 [171.9–171.9] | 99.77 [99.77–99.77] | 0 [0–0] | 478 [478–478] | 0.2151 [0.2151–0.2151] | 91.75 [91.75–91.75] | 30 [30–30] | 40 [40–40] |
| catchup:reactor:10000:10000:64 / off / today | 0/1; 026 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:10000:10000:64 / off / equivalent / L64 / hint | 1/1; 027 | 5.837e+04 [5.837e+04–5.837e+04] | 8.696e+04 [8.696e+04–8.696e+04] | 56.34 [56.34–56.34] | 171.3 [171.3–171.3] | 677.1 [677.1–677.1] | 505.7 [505.7–505.7] | 0 [0–0] | 684 [684–684] | 0.2342 [0.2342–0.2342] | 477.4 [477.4–477.4] | 30 [30–30] | 430 [430–430] |
| catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint | 1/1; 028 | 7.036e+04 [7.036e+04–7.036e+04] | 9.845e+04 [9.845e+04–9.845e+04] | 40.56 [40.56–40.56] | 142.1 [142.1–142.1] | 214.8 [214.8–214.8] | 72.69 [72.69–72.69] | 0 [0–0] | 341.6 [341.6–341.6] | 0.1471 [0.1471–0.1471] | 108.6 [108.6–108.6] | 30 [30–30] | 60 [60–60] |
| catchup:reactor:10000:100:64 / on / today | 3/3; 029,034,037 | 6,438 [4,850–6,548] | 1.029e+04 [7,267–1.12e+04] | 660.2 [555.4–685.7] | 1,553 [1,527–2,062] | 2,666 [2,412–3,611] | 1,113 [884.9–1,549] | 0 [0–0] | 962.1 [914.8–1,440] | 0.2149 [0.1931–0.2533] | 4,200 [3,798–5,153] | 2,930 [2,650–3,490] | 1,250 [1,130–1,640] |
| catchup:pull:10000:100:64 / on / equivalent / L64 / hint | 3/3; 030,035,038 | 5.897e+04 [5.281e+04–1.294e+05] | 7.18e+04 [6.329e+04–1.487e+05] | 30.3 [10.02–31.38] | 169.6 [77.27–189.4] | 340.9 [153.6–396.1] | 171.3 [76.29–206.7] | 0 [0–0] | 517.6 [488.6–631.8] | 0.2203 [0.2082–0.2587] | 163.6 [90.9–175.2] | 30 [10–30] | 120 [60–130] |
| catchup:pull:10000:100:64 / on / checkpoint / L64 / hint | 3/3; 031,036,039 | 7.683e+04 [7.524e+04–1.383e+05] | 9.534e+04 [8.882e+04–1.605e+05] | 20.31 [10.01–25.28] | 130.2 [72.3–132.9] | 203.7 [149.6–205.9] | 75.74 [70.75–77.35] | 0 [0–0] | 568.5 [355.6–630.2] | 0.2542 [0.1569–0.2732] | 161.7 [131.2–175.3] | 70 [30–80] | 100 [80–100] |
| catchup:pull:10000:100:64 / on / equivalent / L1 / hint | 1/1; 032 | 5.416e+04 [5.416e+04–5.416e+04] | 7.278e+04 [7.278e+04–7.278e+04] | 47.23 [47.23–47.23] | 184.6 [184.6–184.6] | 325.3 [325.3–325.3] | 140.7 [140.7–140.7] | 0 [0–0] | 296.4 [296.4–296.4] | 0.1234 [0.1234–0.1234] | 158.9 [158.9–158.9] | 30 [30–30] | 120 [120–120] |
| catchup:pull:10000:100:64 / on / checkpoint / L1 / hint | 1/1; 033 | 4.985e+04 [4.985e+04–4.985e+04] | 7.628e+04 [7.628e+04–7.628e+04] | 69.52 [69.52–69.52] | 200.6 [200.6–200.6] | 268.4 [268.4–268.4] | 67.75 [67.75–67.75] | 0 [0–0] | 557.5 [557.5–557.5] | 0.2342 [0.2342–0.2342] | 165.5 [165.5–165.5] | 30 [30–30] | 110 [110–110] |
| catchup:reactor:10000:100:64 / off / today | 2/3; 040,045,048 | 9,637 [6,266–1.301e+04] | 1.842e+04 [1.068e+04–2.616e+04] | 523.1 [386.5–659.8] | 1,182 [768.7–1,596] | 1,751 [1,066–2,436] | 568.6 [297.3–839.9] | 0 [0–0] | 1,110 [918.4–1,303] | 0.2911 [0.2893–0.293] | 3,572 [3,392–3,751] | 2,415 [2,080–2,750] | 1,130 [970–1,290] |
| catchup:pull:10000:100:64 / off / equivalent / L64 / hint | 3/3; 041,046,049 | 7.15e+04 [4.423e+04–1.503e+05] | 9.26e+04 [6.527e+04–1.758e+05] | 31.86 [9.659–72.88] | 139.9 [66.55–226.1] | 248.9 [146.6–325.6] | 99.55 [80.06–109] | 0 [0–0] | 506.2 [458.6–594.9] | 0.2051 [0.1887–0.2522] | 186.1 [165.9–217.8] | 50 [50–90] | 100 [80–100] |
| catchup:pull:10000:100:64 / off / checkpoint / L64 / hint | 3/3; 042,047,050 | 8.129e+04 [6.97e+04–1.518e+05] | 9.698e+04 [9.544e+04–1.866e+05] | 18.23 [12.29–40.37] | 123 [65.89–143.5] | 193.3 [148.2–199.6] | 76.59 [49.83–82.31] | 0 [0–0] | 596.4 [514.6–680] | 0.2661 [0.2341–0.2997] | 113.5 [94.16–149.3] | 30 [30–80] | 60 [60–60] |
| catchup:pull:10000:100:64 / off / equivalent / L1 / hint | 1/1; 043 | 1.451e+05 [1.451e+05–1.451e+05] | 1.696e+05 [1.696e+05–1.696e+05] | 9.946 [9.946–9.946] | 68.91 [68.91–68.91] | 147.2 [147.2–147.2] | 78.28 [78.28–78.28] | 0 [0–0] | 561 [561–561] | 0.255 [0.255–0.255] | 276.9 [276.9–276.9] | 30 [30–30] | 240 [240–240] |
| catchup:pull:10000:100:64 / off / checkpoint / L1 / hint | 1/1; 044 | 1.032e+05 [1.032e+05–1.032e+05] | 1.302e+05 [1.302e+05–1.302e+05] | 20.13 [20.13–20.13] | 96.93 [96.93–96.93] | 184 [184–184] | 87.1 [87.1–87.1] | 0 [0–0] | 421.7 [421.7–421.7] | 0.1856 [0.1856–0.1856] | 115.2 [115.2–115.2] | 30 [30–30] | 60 [60–60] |
| catchup:reactor:10000:1:64 / on / today | 1/1; 051 | 2,874 [2,874–2,874] | 4,572 [4,572–4,572] | 1,293 [1,293–1,293] | 3,480 [3,480–3,480] | 4,005 [4,005–4,005] | 525.7 [525.7–525.7] | 0 [0–0] | 1,027 [1,027–1,027] | 0.1698 [0.1698–0.1698] | 3,331 [3,331–3,331] | 3,000 [3,000–3,000] | 330 [330–330] |
| catchup:pull:10000:1:64 / on / equivalent / L64 / hint | 1/1; 052 | 8.97e+04 [8.97e+04–8.97e+04] | 1.028e+05 [1.028e+05–1.028e+05] | 14.2 [14.2–14.2] | 111.5 [111.5–111.5] | 163.4 [163.4–163.4] | 51.88 [51.88–51.88] | 0 [0–0] | 410.7 [410.7–410.7] | 0.1848 [0.1848–0.1848] | 136.5 [136.5–136.5] | 40 [40–40] | 80 [80–80] |
| catchup:pull:10000:1:64 / on / checkpoint / L64 / hint | 1/1; 053 | 5.707e+04 [5.707e+04–5.707e+04] | 8.999e+04 [8.999e+04–8.999e+04] | 64.11 [64.11–64.11] | 175.2 [175.2–175.2] | 255.3 [255.3–255.3] | 80.08 [80.08–80.08] | 0 [0–0] | 317.8 [317.8–317.8] | 0.1359 [0.1359–0.1359] | 133.2 [133.2–133.2] | 30 [30–30] | 100 [100–100] |
| catchup:reactor:10000:1:64 / off / today | 1/1; 054 | 4,232 [4,232–4,232] | 5,761 [5,761–5,761] | 627.1 [627.1–627.1] | 2,363 [2,363–2,363] | 2,789 [2,789–2,789] | 425.9 [425.9–425.9] | 0 [0–0] | 988.7 [988.7–988.7] | 0.2036 [0.2036–0.2036] | 2,894 [2,894–2,894] | 2,620 [2,620–2,620] | 260 [260–260] |
| catchup:pull:10000:1:64 / off / equivalent / L64 / hint | 1/1; 055 | 6.287e+04 [6.287e+04–6.287e+04] | 8.042e+04 [8.042e+04–8.042e+04] | 34.71 [34.71–34.71] | 159.1 [159.1–159.1] | 324.6 [324.6–324.6] | 165.5 [165.5–165.5] | 0 [0–0] | 626.6 [626.6–626.6] | 0.261 [0.261–0.261] | 151.7 [151.7–151.7] | 50 [50–50] | 100 [100–100] |
| catchup:pull:10000:1:64 / off / checkpoint / L64 / hint | 1/1; 056 | 5.937e+04 [5.937e+04–5.937e+04] | 8.769e+04 [8.769e+04–8.769e+04] | 54.41 [54.41–54.41] | 168.4 [168.4–168.4] | 228.5 [228.5–228.5] | 60.08 [60.08–60.08] | 0 [0–0] | 535.2 [535.2–535.2] | 0.2309 [0.2309–0.2309] | 116.3 [116.3–116.3] | 20 [20–20] | 80 [80–80] |
| appendonly:reactor:1000:1:1 / on / today | 1/1; 057 | 176.1 [176.1–176.1] | 176.1 [176.1–176.1] | 1.26 [1.26–1.26] | 5,679 [5,679–5,679] | n/a | n/a | 0 [0–0] | 2,080 [2,080–2,080] | 0.2657 [0.2657–0.2657] | 1.163e+04 [1.163e+04–1.163e+04] | 9,720 [9,720–9,720] | 1,900 [1,900–1,900] |
| live:reactor:1000:1:1 / on / today | 1/1; 058 | 38.33 [38.33–38.33] | 38.35 [38.35–38.35] | 11.25 [11.25–11.25] | 2.609e+04 [2.609e+04–2.609e+04] | 2.62e+04 [2.62e+04–2.62e+04] | 112.1 [112.1–112.1] | 25.37 [25.37–25.37] | 2,251 [2,251–2,251] | 0.07963 [0.07963–0.07963] | 2.336e+04 [2.336e+04–2.336e+04] | 1.712e+04 [1.712e+04–1.712e+04] | 6,210 [6,210–6,210] |
| live:pull:1000:1:1 / on / equivalent / L64 / hint | 1/1; 059 | 57.88 [57.88–57.88] | 57.88 [57.88–57.88] | 0.0509 [0.0509–0.0509] | 1.728e+04 [1.728e+04–1.728e+04] | 1.742e+04 [1.742e+04–1.742e+04] | 141.7 [141.7–141.7] | 3.06 [3.06–3.06] | 3,585 [3,585–3,585] | 0.1839 [0.1839–0.1839] | 2.262e+04 [2.262e+04–2.262e+04] | 1.604e+04 [1.604e+04–1.604e+04] | 6,570 [6,570–6,570] |
| live:pull:1000:1:1 / on / checkpoint / L64 / hint | 1/1; 060 | 41.23 [41.23–41.23] | 41.23 [41.23–41.23] | 0.0687 [0.0687–0.0687] | 2.425e+04 [2.425e+04–2.425e+04] | 2.437e+04 [2.437e+04–2.437e+04] | 113 [113–113] | 1.002 [1.002–1.002] | 3,837 [3,837–3,837] | 0.1452 [0.1452–0.1452] | 2.209e+04 [2.209e+04–2.209e+04] | 1.554e+04 [1.554e+04–1.554e+04] | 6,540 [6,540–6,540] |
| live:pull:1000:1:1 / on / equivalent / L64 / poll | 1/1; 061 | 57.81 [57.81–57.81] | 57.81 [57.81–57.81] | 0.0175 [0.0175–0.0175] | 1.73e+04 [1.73e+04–1.73e+04] | 1.741e+04 [1.741e+04–1.741e+04] | 108 [108–108] | 41.34 [41.34–41.34] | 2,938 [2,938–2,938] | 0.1509 [0.1509–0.1509] | 2.15e+04 [2.15e+04–2.15e+04] | 1.604e+04 [1.604e+04–1.604e+04] | 5,440 [5,440–5,440] |
| live:pull:1000:1:1 / on / checkpoint / L64 / poll | 1/1; 062 | 43.13 [43.13–43.13] | 43.13 [43.13–43.13] | 0.0183 [0.0183–0.0183] | 2.318e+04 [2.318e+04–2.318e+04] | 2.326e+04 [2.326e+04–2.326e+04] | 73.45 [73.45–73.45] | 57.77 [57.77–57.77] | 3,816 [3,816–3,816] | 0.1505 [0.1505–0.1505] | 2.26e+04 [2.26e+04–2.26e+04] | 1.694e+04 [1.694e+04–1.694e+04] | 5,640 [5,640–5,640] |
| appendonly:reactor:1000:1:1 / off / today | 1/1; 063 | 59.92 [59.92–59.92] | 59.93 [59.93–59.93] | 1.446 [1.446–1.446] | 1.669e+04 [1.669e+04–1.669e+04] | n/a | n/a | 0 [0–0] | 1,911 [1,911–1,911] | 0.101 [0.101–0.101] | 2.225e+04 [2.225e+04–2.225e+04] | 1.79e+04 [1.79e+04–1.79e+04] | 4,320 [4,320–4,320] |
| live:reactor:1000:1:1 / off / today | 1/1; 064 | 49.43 [49.43–49.43] | 49.46 [49.46–49.46] | 10.67 [10.67–10.67] | 2.023e+04 [2.023e+04–2.023e+04] | 2.026e+04 [2.026e+04–2.026e+04] | 30.41 [30.41–30.41] | 0 [0–0] | 2,630 [2,630–2,630] | 0.1177 [0.1177–0.1177] | 2.36e+04 [2.36e+04–2.36e+04] | 1.864e+04 [1.864e+04–1.864e+04] | 4,930 [4,930–4,930] |
| live:pull:1000:1:1 / off / equivalent / L64 / hint | 1/1; 065 | 64.33 [64.33–64.33] | 64.33 [64.33–64.33] | 0.0165 [0.0165–0.0165] | 1.554e+04 [1.554e+04–1.554e+04] | 1.566e+04 [1.566e+04–1.566e+04] | 112.8 [112.8–112.8] | 0.6746 [0.6746–0.6746] | 4,087 [4,087–4,087] | 0.2308 [0.2308–0.2308] | 1.564e+04 [1.564e+04–1.564e+04] | 1.191e+04 [1.191e+04–1.191e+04] | 3,710 [3,710–3,710] |
| live:pull:1000:1:1 / off / checkpoint / L64 / hint | 1/1; 066 | 133.7 [133.7–133.7] | 133.7 [133.7–133.7] | 0.0176 [0.0176–0.0176] | 7,481 [7,481–7,481] | 7,595 [7,595–7,595] | 113.1 [113.1–113.1] | 0.5793 [0.5793–0.5793] | 4,532 [4,532–4,532] | 0.469 [0.469–0.469] | 1.78e+04 [1.78e+04–1.78e+04] | 1.459e+04 [1.459e+04–1.459e+04] | 3,200 [3,200–3,200] |
| live:pull:1000:1:1 / off / equivalent / L64 / poll | 1/1; 067 | 60.99 [60.99–60.99] | 60.99 [60.99–60.99] | 0.0164 [0.0164–0.0164] | 1.64e+04 [1.64e+04–1.64e+04] | 1.65e+04 [1.65e+04–1.65e+04] | 100.4 [100.4–100.4] | 26.59 [26.59–26.59] | 3,386 [3,386–3,386] | 0.1821 [0.1821–0.1821] | 1.823e+04 [1.823e+04–1.823e+04] | 1.417e+04 [1.417e+04–1.417e+04] | 4,050 [4,050–4,050] |
| live:pull:1000:1:1 / off / checkpoint / L64 / poll | 1/1; 068 | 49.39 [49.39–49.39] | 49.39 [49.39–49.39] | 0.0167 [0.0167–0.0167] | 2.025e+04 [2.025e+04–2.025e+04] | 2.038e+04 [2.038e+04–2.038e+04] | 131.7 [131.7–131.7] | 0 [0–0] | 3,718 [3,718–3,718] | 0.1655 [0.1655–0.1655] | 2.116e+04 [2.116e+04–2.116e+04] | 1.677e+04 [1.677e+04–1.677e+04] | 4,360 [4,360–4,360] |
| appendonly:reactor:1000:1:16 / on / today | 1/1; 069 | 68.2 [68.2–68.2] | 68.26 [68.26–68.26] | 11.48 [11.48–11.48] | 1.466e+04 [1.466e+04–1.466e+04] | n/a | n/a | 0 [0–0] | 1,647 [1,647–1,647] | 0.09781 [0.09781–0.09781] | 1.427e+04 [1.427e+04–1.427e+04] | 1.125e+04 [1.125e+04–1.125e+04] | 3,000 [3,000–3,000] |
| live:reactor:1000:1:16 / on / today | 1/3; 070 | 85.69 [85.69–85.69] | 85.74 [85.74–85.74] | 8.037 [8.037–8.037] | 1.167e+04 [1.167e+04–1.167e+04] | 1.177e+04 [1.177e+04–1.177e+04] | 98.18 [98.18–98.18] | 42.52 [42.52–42.52] | 1,969 [1,969–1,969] | 0.1422 [0.1422–0.1422] | 1.869e+04 [1.869e+04–1.869e+04] | 1.501e+04 [1.501e+04–1.501e+04] | 3,660 [3,660–3,660] |
| live:pull:1000:1:16 / on / equivalent / L64 / hint | 1/3; 071 | 305.1 [305.1–305.1] | 305.1 [305.1–305.1] | 0.0203 [0.0203–0.0203] | 3,278 [3,278–3,278] | 3,301 [3,301–3,301] | 23.13 [23.13–23.13] | 0 [0–0] | 4,163 [4,163–4,163] | 0.7678 [0.7678–0.7678] | 1.208e+04 [1.208e+04–1.208e+04] | 9,660 [9,660–9,660] | 2,400 [2,400–2,400] |
| live:pull:1000:1:16 / on / checkpoint / L64 / hint | 1/3; 072 | 81.62 [81.62–81.62] | 81.62 [81.62–81.62] | 0.0167 [0.0167–0.0167] | 1.225e+04 [1.225e+04–1.225e+04] | 1.242e+04 [1.242e+04–1.242e+04] | 167.8 [167.8–167.8] | 0 [0–0] | 3,029 [3,029–3,029] | 0.2086 [0.2086–0.2086] | 1.407e+04 [1.407e+04–1.407e+04] | 1.042e+04 [1.042e+04–1.042e+04] | 3,640 [3,640–3,640] |

## Per-scenario valid cost

| Scenario / profile / mode / lanes / wake | evidence | reads/event through durable | reads/event full window | observer progress wire writes/event | observer document attempts/event | raw all ops/event | adjusted all ops/event | all write profiler statements/event | all document attempts/event |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| catchup:reactor:2000:2000:64 / on / today | 001,006,009 | n/a | n/a | 6.978 [6.864–6.989] | 6.978 [6.864–6.989] | 12.05 [11.93–12.06] | 12.05 [11.93–12.06] | 9.995 [9.879–10] | 9.995 [9.879–10] |
| catchup:pull:2000:2000:64 / on / equivalent / L64 / hint | 002,007,010 | 0.0025 [0.0025–0.003] | 0.012 [0.0115–0.012] | 0.002 [0.001–0.002] | 1.001 [1–1.001] | 1.016 [1.012–1.016] | 0.016 [0.0135–0.0165] | 1.002 [1.001–1.002] | 1.002 [1.001–1.002] |
| catchup:pull:2000:2000:64 / on / checkpoint / L64 / hint | 003,008,011 | 0.002 [0.002–0.0025] | 0.011 [0.011–0.0115] | 0.001 [0.001–0.001] | 0.001 [0.001–0.001] | 0.014 [0.0135–0.021] | 0.014 [0.0135–0.021] | 0.002 [0.002–0.0095] | 0.002 [0.002–0.0095] |
| catchup:pull:2000:2000:64 / on / equivalent / L1 / hint | 004 | 0.0025 [0.0025–0.0025] | 0.012 [0.012–0.012] | 0.002 [0.002–0.002] | 1.001 [1.001–1.001] | 1.014 [1.014–1.014] | 0.0155 [0.0155–0.0155] | 1.001 [1.001–1.001] | 1.001 [1.001–1.001] |
| catchup:pull:2000:2000:64 / on / checkpoint / L1 / hint | 005 | 0.002 [0.002–0.002] | 0.011 [0.011–0.011] | 0.001 [0.001–0.001] | 0.001 [0.001–0.001] | 0.015 [0.015–0.015] | 0.015 [0.015–0.015] | 0.002 [0.002–0.002] | 0.002 [0.002–0.002] |
| catchup:reactor:2000:2000:64 / off / today | 012,017,020 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:2000:2000:64 / off / equivalent / L64 / hint | 013,018,021 | 0.0025 [0.0025–0.0025] | 0.0115 [0.0115–0.0115] | 0.002 [0.001–0.002] | 1.001 [1–1.001] | n/a | n/a | n/a | n/a |
| catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint | 014,019,022 | 0.002 [0.002–0.002] | 0.011 [0.0105–0.0115] | 0.001 [0.001–0.001] | 0.001 [0.001–0.001] | n/a | n/a | n/a | n/a |
| catchup:pull:2000:2000:64 / off / equivalent / L1 / hint | 015 | 0.002 [0.002–0.002] | 0.011 [0.011–0.011] | 0.002 [0.002–0.002] | 1.001 [1.001–1.001] | n/a | n/a | n/a | n/a |
| catchup:pull:2000:2000:64 / off / checkpoint / L1 / hint | 016 | 0.002 [0.002–0.002] | 0.011 [0.011–0.011] | 0.001 [0.001–0.001] | 0.001 [0.001–0.001] | n/a | n/a | n/a | n/a |
| catchup:reactor:10000:10000:64 / on / today | 023 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:10000:10000:64 / on / equivalent / L64 / hint | 024 | 0.0018 [0.0018–0.0018] | 0.0039 [0.0039–0.0039] | 0.0004 [0.0004–0.0004] | 1 [1–1] | 1.004 [1.004–1.004] | 0.0046 [0.0046–0.0046] | 1 [1–1] | 1 [1–1] |
| catchup:pull:10000:10000:64 / on / checkpoint / L64 / hint | 025 | 0.0011 [0.0011–0.0011] | 0.0031 [0.0031–0.0031] | 0.0006 [0.0006–0.0006] | 0.0006 [0.0006–0.0006] | 0.004 [0.004–0.004] | 0.004 [0.004–0.004] | 0.0008 [0.0008–0.0008] | 0.0008 [0.0008–0.0008] |
| catchup:reactor:10000:10000:64 / off / today | 026 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:10000:10000:64 / off / equivalent / L64 / hint | 027 | 0.0015 [0.0015–0.0015] | 0.0035 [0.0035–0.0035] | 0.0004 [0.0004–0.0004] | 1 [1–1] | n/a | n/a | n/a | n/a |
| catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint | 028 | 0.0011 [0.0011–0.0011] | 0.003 [0.003–0.003] | 0.0004 [0.0004–0.0004] | 0.0004 [0.0004–0.0004] | n/a | n/a | n/a | n/a |
| catchup:reactor:10000:100:64 / on / today | 029,034,037 | n/a | n/a | 0.0717 [0.0717–0.0717] | 0.0717 [0.0717–0.0717] | 0.1252 [0.1249–0.1264] | 0.1252 [0.1249–0.1264] | 0.0949 [0.0947–0.0962] | 0.0949 [0.0947–0.0962] |
| catchup:pull:10000:100:64 / on / equivalent / L64 / hint | 030,035,038 | 0.0012 [0.0011–0.0012] | 0.0031 [0.003–0.0031] | 0.0004 [0.0004–0.0004] | 0.0202 [0.0202–0.0202] | 0.0233 [0.0233–0.0235] | 0.0035 [0.0035–0.0037] | 0.0202 [0.0202–0.0204] | 0.0202 [0.0202–0.0204] |
| catchup:pull:10000:100:64 / on / checkpoint / L64 / hint | 031,036,039 | 0.0011 [0.0011–0.0011] | 0.0031 [0.003–0.0031] | 0.0004 [0.0003–0.0004] | 0.0004 [0.0003–0.0004] | 0.0035 [0.0034–0.0037] | 0.0035 [0.0034–0.0037] | 0.0004 [0.0004–0.0005] | 0.0004 [0.0004–0.0005] |
| catchup:pull:10000:100:64 / on / equivalent / L1 / hint | 032 | 0.0012 [0.0012–0.0012] | 0.003 [0.003–0.003] | 0.0004 [0.0004–0.0004] | 0.0202 [0.0202–0.0202] | 0.0232 [0.0232–0.0232] | 0.0034 [0.0034–0.0034] | 0.0202 [0.0202–0.0202] | 0.0202 [0.0202–0.0202] |
| catchup:pull:10000:100:64 / on / checkpoint / L1 / hint | 033 | 0.0011 [0.0011–0.0011] | 0.0029 [0.0029–0.0029] | 0.0007 [0.0007–0.0007] | 0.0007 [0.0007–0.0007] | 0.0039 [0.0039–0.0039] | 0.0039 [0.0039–0.0039] | 0.0009 [0.0009–0.0009] | 0.0009 [0.0009–0.0009] |
| catchup:reactor:10000:100:64 / off / today | 040,045,048 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:10000:100:64 / off / equivalent / L64 / hint | 041,046,049 | 0.0011 [0.0011–0.0012] | 0.003 [0.0029–0.003] | 0.0004 [0.0004–0.0006] | 0.0202 [0.0202–0.0303] | n/a | n/a | n/a | n/a |
| catchup:pull:10000:100:64 / off / checkpoint / L64 / hint | 042,047,050 | 0.0011 [0.0011–0.0011] | 0.003 [0.003–0.0031] | 0.0004 [0.0003–0.0006] | 0.0004 [0.0003–0.0006] | n/a | n/a | n/a | n/a |
| catchup:pull:10000:100:64 / off / equivalent / L1 / hint | 043 | 0.0011 [0.0011–0.0011] | 0.0031 [0.0031–0.0031] | 0.0006 [0.0006–0.0006] | 0.0303 [0.0303–0.0303] | n/a | n/a | n/a | n/a |
| catchup:pull:10000:100:64 / off / checkpoint / L1 / hint | 044 | 0.0011 [0.0011–0.0011] | 0.0031 [0.0031–0.0031] | 0.0005 [0.0005–0.0005] | 0.0005 [0.0005–0.0005] | n/a | n/a | n/a | n/a |
| catchup:reactor:10000:1:64 / on / today | 051 | n/a | n/a | 0.0026 [0.0026–0.0026] | 0.0026 [0.0026–0.0026] | 0.0167 [0.0167–0.0167] | 0.0167 [0.0167–0.0167] | 0.0058 [0.0058–0.0058] | 0.0058 [0.0058–0.0058] |
| catchup:pull:10000:1:64 / on / equivalent / L64 / hint | 052 | 0.0011 [0.0011–0.0011] | 0.003 [0.003–0.003] | 0.0008 [0.0008–0.0008] | 0.0008 [0.0008–0.0008] | 0.0041 [0.0041–0.0041] | 0.0041 [0.0041–0.0041] | 0.0008 [0.0008–0.0008] | 0.0008 [0.0008–0.0008] |
| catchup:pull:10000:1:64 / on / checkpoint / L64 / hint | 053 | 0.0011 [0.0011–0.0011] | 0.003 [0.003–0.003] | 0.0005 [0.0005–0.0005] | 0.0005 [0.0005–0.0005] | 0.0035 [0.0035–0.0035] | 0.0035 [0.0035–0.0035] | 0.0005 [0.0005–0.0005] | 0.0005 [0.0005–0.0005] |
| catchup:reactor:10000:1:64 / off / today | 054 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| catchup:pull:10000:1:64 / off / equivalent / L64 / hint | 055 | 0.0012 [0.0012–0.0012] | 0.003 [0.003–0.003] | 0.0012 [0.0012–0.0012] | 0.0012 [0.0012–0.0012] | n/a | n/a | n/a | n/a |
| catchup:pull:10000:1:64 / off / checkpoint / L64 / hint | 056 | 0.0011 [0.0011–0.0011] | 0.003 [0.003–0.003] | 0.0008 [0.0008–0.0008] | 0.0008 [0.0008–0.0008] | n/a | n/a | n/a | n/a |
| appendonly:reactor:1000:1:1 / on / today | 057 | n/a | n/a | 0 [0–0] | 0 [0–0] | 3.353 [3.353–3.353] | 3.353 [3.353–3.353] | 3.352 [3.352–3.352] | 3.352 [3.352–3.352] |
| live:reactor:1000:1:1 / on / today | 058 | n/a | n/a | 1.912 [1.912–1.912] | 1.912 [1.912–1.912] | 4.921 [4.921–4.921] | 4.921 [4.921–4.921] | 4.918 [4.918–4.918] | 4.918 [4.918–4.918] |
| live:pull:1000:1:1 / on / equivalent / L64 / hint | 059 | 1.001 [1.001–1.001] | 1.019 [1.019–1.019] | 0.448 [0.448–0.448] | 0.448 [0.448–0.448] | 5.218 [5.218–5.218] | 5.218 [5.218–5.218] | 4.196 [4.196–4.196] | 4.196 [4.196–4.196] |
| live:pull:1000:1:1 / on / checkpoint / L64 / hint | 060 | 1.003 [1.003–1.003] | 1.022 [1.022–1.022] | 0.311 [0.311–0.311] | 0.311 [0.311–0.311] | 4.763 [4.763–4.763] | 4.763 [4.763–4.763] | 3.739 [3.739–3.739] | 3.739 [3.739–3.739] |
| live:pull:1000:1:1 / on / equivalent / L64 / poll | 061 | 0.157 [0.157–0.157] | 0.175 [0.175–0.175] | 0.312 [0.312–0.312] | 0.312 [0.312–0.312] | 4.37 [4.37–4.37] | 4.37 [4.37–4.37] | 4.193 [4.193–4.193] | 4.193 [4.193–4.193] |
| live:pull:1000:1:1 / on / checkpoint / L64 / poll | 062 | 0.209 [0.209–0.209] | 0.227 [0.227–0.227] | 0.209 [0.209–0.209] | 0.209 [0.209–0.209] | 3.982 [3.982–3.982] | 3.982 [3.982–3.982] | 3.753 [3.753–3.753] | 3.753 [3.753–3.753] |
| appendonly:reactor:1000:1:1 / off / today | 063 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| live:reactor:1000:1:1 / off / today | 064 | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a |
| live:pull:1000:1:1 / off / equivalent / L64 / hint | 065 | 1.001 [1.001–1.001] | 1.02 [1.02–1.02] | 0.364 [0.364–0.364] | 0.364 [0.364–0.364] | n/a | n/a | n/a | n/a |
| live:pull:1000:1:1 / off / checkpoint / L64 / hint | 066 | 1.001 [1.001–1.001] | 1.02 [1.02–1.02] | 0.133 [0.133–0.133] | 0.133 [0.133–0.133] | n/a | n/a | n/a | n/a |
| live:pull:1000:1:1 / off / equivalent / L64 / poll | 067 | 0.152 [0.152–0.152] | 0.172 [0.172–0.172] | 0.304 [0.304–0.304] | 0.304 [0.304–0.304] | n/a | n/a | n/a | n/a |
| live:pull:1000:1:1 / off / checkpoint / L64 / poll | 068 | 0.185 [0.185–0.185] | 0.204 [0.204–0.204] | 0.184 [0.184–0.184] | 0.184 [0.184–0.184] | n/a | n/a | n/a | n/a |
| appendonly:reactor:1000:1:16 / on / today | 069 | n/a | n/a | 0 [0–0] | 0 [0–0] | 2.072 [2.072–2.072] | 2.072 [2.072–2.072] | 2.071 [2.071–2.071] | 2.071 [2.071–2.071] |
| live:reactor:1000:1:16 / on / today | 070 | n/a | n/a | 0.847 [0.847–0.847] | 0.847 [0.847–0.847] | 2.746 [2.746–2.746] | 2.746 [2.746–2.746] | 2.743 [2.743–2.743] | 2.743 [2.743–2.743] |
| live:pull:1000:1:16 / on / equivalent / L64 / hint | 071 | 0.996 [0.996–0.996] | 1.015 [1.015–1.015] | 0.11 [0.11–0.11] | 0.11 [0.11–0.11] | 3.134 [3.134–3.134] | 3.134 [3.134–3.134] | 2.118 [2.118–2.118] | 2.118 [2.118–2.118] |
| live:pull:1000:1:16 / on / checkpoint / L64 / hint | 072 | 0.954 [0.954–0.954] | 0.972 [0.972–0.972] | 0.155 [0.155–0.155] | 0.155 [0.155–0.155] | 3.185 [3.185–3.185] | 3.185 [3.185–3.185] | 2.211 [2.211–2.211] | 2.211 [2.211–2.211] |

## Detailed gate comparisons

### Correctness / validity, every run

- INSUFFICIENT: 69/72 valid; invalid/failed 023,026,045; 68 pending; pull structural defects none

### 2k projected >=10x (equivalent)

- PASS: catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: 14.31 >= 10 (013,018,021,054,012,017,020)

### Avoid STOP: 2k projected >=5x (equivalent)

- PASS: catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: 14.31 >= 5 (013,018,021,054,012,017,020)

### 10k finishes; projected >=min(2000,ceiling) (equivalent)

- PASS: catchup:pull:10000:10000:64 / off / equivalent / L64 / hint: 5,761 >= 2,000 (027,054)

### 10k/100-key rate >=0.5 (equivalent)

- PASS: catchup:pull:10000:10000:64 / off / equivalent / L64 / hint: 0.939 >= 0.5 (027,041,046,049)

### 100 keys >=today (equivalent)

- PASS: catchup:pull:10000:100:64 / off / equivalent / L64 / hint: 5.027 >= 1 (041,046,049,040,045,048)

### Sparse relevant rate >=3x (equivalent)

- INSUFFICIENT: catchup:pull:2000:2000:64:sparse9 / off / equivalent / L64 / hint: n/a >= 3 (none); repeats/pairs pending 137,136

### 2k projected >=10x (checkpoint)

- PASS: catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint: 14.31 >= 10 (014,019,022,054,012,017,020)

### Avoid STOP: 2k projected >=5x (checkpoint)

- PASS: catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint: 14.31 >= 5 (014,019,022,054,012,017,020)

### 10k finishes; projected >=min(2000,ceiling) (checkpoint)

- PASS: catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint: 5,761 >= 2,000 (028,054)

### 10k/100-key rate >=0.5 (checkpoint)

- PASS: catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint: 1.015 >= 0.5 (028,042,047,050)

### 100 keys >=today (checkpoint)

- PASS: catchup:pull:10000:100:64 / off / checkpoint / L64 / hint: 5.264 >= 1 (042,047,050,040,045,048)

### Sparse relevant rate >=3x (checkpoint)

- INSUFFICIENT: catchup:pull:2000:2000:64:sparse9 / off / checkpoint / L64 / hint: n/a >= 3 (none); repeats/pairs pending 138,136

### Equivalent catch-up writes/event <=0.02

- PASS: catchup:pull:2000:2000:64 / on / equivalent / L64 / hint: 0.002 <= 0.02 (002,007,010)
- PASS: catchup:pull:2000:2000:64 / on / equivalent / L1 / hint: 0.002 <= 0.02 (004)
- PASS: catchup:pull:10000:10000:64 / on / equivalent / L64 / hint: 0.0004 <= 0.02 (024)
- PASS: catchup:pull:10000:100:64 / on / equivalent / L64 / hint: 0.0004 <= 0.02 (030,035,038)
- PASS: catchup:pull:10000:100:64 / on / equivalent / L1 / hint: 0.0004 <= 0.02 (032)
- PASS: catchup:pull:10000:1:64 / on / equivalent / L64 / hint: 0.0008 <= 0.02 (052)
- INSUFFICIENT: catchup:pull:2000:2000:64:sparse9 / on / equivalent / L64 / hint: n/a <= 0.02 (none); repeats/pairs pending 134
- INSUFFICIENT: catchup:pull:10000:100:64:overlap1000 / on / equivalent / L64 / hint: n/a <= 0.02 (none); repeats/pairs pending 139

### Equivalent live writes/event <=0.2 (>=200 paced; 1:16)

- INSUFFICIENT: live:pull:1000:1:16 / on / equivalent / L64 / hint: 0.11 <= 0.2 (071); repeats/pairs pending 076,079
- INSUFFICIENT: live:pull:1000:1:16 / on / equivalent / L64 / poll: n/a <= 0.2 (none); repeats/pairs pending 073
- INSUFFICIENT: live:pull:300:1:1:rate=200 / on / equivalent / L64 / hint: n/a <= 0.2 (none); repeats/pairs pending 119
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / on / equivalent / L64 / hint: n/a <= 0.2 (none); repeats/pairs pending 127

### Equivalent paced20 writes/event <=2

- INSUFFICIENT: live:pull:300:1:1:rate=20 / on / equivalent / L64 / hint: n/a <= 2 (none); repeats/pairs pending 111

### Live reads/event <=1 (1:1); <=0.3 (:16)

- FAIL: live:pull:1000:1:1 / on / equivalent / L64 / hint: 1.001 <= 1 (059)
- FAIL: live:pull:1000:1:1 / on / checkpoint / L64 / hint: 1.003 <= 1 (060)
- PASS: live:pull:1000:1:1 / on / equivalent / L64 / poll: 0.157 <= 1 (061)
- PASS: live:pull:1000:1:1 / on / checkpoint / L64 / poll: 0.209 <= 1 (062)
- FAIL: live:pull:1000:1:1 / off / equivalent / L64 / hint: 1.001 <= 1 (065)
- FAIL: live:pull:1000:1:1 / off / checkpoint / L64 / hint: 1.001 <= 1 (066)
- PASS: live:pull:1000:1:1 / off / equivalent / L64 / poll: 0.152 <= 1 (067)
- PASS: live:pull:1000:1:1 / off / checkpoint / L64 / poll: 0.185 <= 1 (068)
- FAIL: live:pull:1000:1:16 / on / equivalent / L64 / hint: 0.996 <= 0.3 (071); repeats/pairs pending 076,079
- FAIL: live:pull:1000:1:16 / on / checkpoint / L64 / hint: 0.954 <= 0.3 (072); repeats/pairs pending 077,080
- INSUFFICIENT: live:pull:1000:1:16 / on / equivalent / L64 / poll: n/a <= 0.3 (none); repeats/pairs pending 073
- INSUFFICIENT: live:pull:1000:1:16 / on / checkpoint / L64 / poll: n/a <= 0.3 (none); repeats/pairs pending 074
- INSUFFICIENT: live:pull:1000:1:16 / off / equivalent / L64 / hint: n/a <= 0.3 (none); repeats/pairs pending 083,088,091
- INSUFFICIENT: live:pull:1000:1:16 / off / checkpoint / L64 / hint: n/a <= 0.3 (none); repeats/pairs pending 084,089,092
- INSUFFICIENT: live:pull:1000:1:16 / off / equivalent / L64 / poll: n/a <= 0.3 (none); repeats/pairs pending 085
- INSUFFICIENT: live:pull:1000:1:16 / off / checkpoint / L64 / poll: n/a <= 0.3 (none); repeats/pairs pending 086

### Live-minus-matching-twin cost <=30% of today

- FAIL: live:pull:1000:1:1 / on / equivalent / L64 / hint: (5.218−3.353)/(4.921−3.353)=1.189 (059,058,057)
- FAIL: live:pull:1000:1:1 / on / checkpoint / L64 / hint: (4.763−3.353)/(4.921−3.353)=0.8992 (060,058,057)
- FAIL: live:pull:1000:1:1 / on / equivalent / L64 / poll: (4.37−3.353)/(4.921−3.353)=0.6486 (061,058,057)
- FAIL: live:pull:1000:1:1 / on / checkpoint / L64 / poll: (3.982−3.353)/(4.921−3.353)=0.4011 (062,058,057)
- FAIL: live:pull:1000:1:16 / on / equivalent / L64 / hint: (3.134−2.072)/(2.746−2.072)=1.576 (071,070,069)
- FAIL: live:pull:1000:1:16 / on / checkpoint / L64 / hint: (3.185−2.072)/(2.746−2.072)=1.651 (072,070,069)
- INSUFFICIENT: live:pull:1000:1:16 / on / equivalent / L64 / poll: (n/a−2.072)/(2.746−2.072)=n/a (070,069)
- INSUFFICIENT: live:pull:1000:1:16 / on / checkpoint / L64 / poll: (n/a−2.072)/(2.746−2.072)=n/a (070,069)
- INSUFFICIENT: live:pull:1000:1000:16 / on / equivalent / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:1000:1000:16 / on / checkpoint / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:1000:1:1:batch / on / equivalent / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:1000:1:1:batch / on / checkpoint / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:300:1:1:rate=20 / on / equivalent / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:300:1:1:rate=20 / on / checkpoint / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:300:1:1:rate=200 / on / equivalent / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:300:1:1:rate=200 / on / checkpoint / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / on / equivalent / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / on / checkpoint / L64 / hint: (n/a−n/a)/(n/a−n/a)=n/a (none)

### Avoid STOP: equivalent live writes <today

- PASS: live:pull:1000:1:1 / on / equivalent / L64 / hint: pull 0.448 vs today 1.912 writes/event (059,058)
- PASS: live:pull:1000:1:1 / on / equivalent / L64 / poll: pull 0.312 vs today 1.912 writes/event (061,058)
- INSUFFICIENT: live:pull:1000:1:16 / on / equivalent / L64 / hint: pull 0.11 vs today 0.847 writes/event (071,070)
- INSUFFICIENT: live:pull:1000:1:16 / on / equivalent / L64 / poll: pull n/a vs today 0.847 writes/event (070)
- INSUFFICIENT: live:pull:1000:1000:16 / on / equivalent / L64 / hint: pull n/a vs today n/a writes/event (none)
- INSUFFICIENT: live:pull:1000:1:1:batch / on / equivalent / L64 / hint: pull n/a vs today n/a writes/event (none)
- INSUFFICIENT: live:pull:300:1:1:rate=20 / on / equivalent / L64 / hint: pull n/a vs today n/a writes/event (none)
- INSUFFICIENT: live:pull:300:1:1:rate=200 / on / equivalent / L64 / hint: pull n/a vs today n/a writes/event (none)
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / on / equivalent / L64 / hint: pull n/a vs today n/a writes/event (none)

### Durable gap <=FLUSH_MS+150ms

- FAIL: catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: 242.9 <= 200 (013,018,021)
- PASS: catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint: 103 <= 200 (014,019,022)
- PASS: catchup:pull:2000:2000:64 / off / equivalent / L1 / hint: 119.6 <= 200 (015)
- PASS: catchup:pull:2000:2000:64 / off / checkpoint / L1 / hint: 121.5 <= 200 (016)
- FAIL: catchup:pull:10000:10000:64 / off / equivalent / L64 / hint: 505.7 <= 200 (027)
- PASS: catchup:pull:10000:10000:64 / off / checkpoint / L64 / hint: 72.69 <= 200 (028)
- PASS: catchup:pull:10000:100:64 / off / equivalent / L64 / hint: 99.55 <= 200 (041,046,049)
- PASS: catchup:pull:10000:100:64 / off / checkpoint / L64 / hint: 76.59 <= 200 (042,047,050)
- PASS: catchup:pull:10000:100:64 / off / equivalent / L1 / hint: 78.28 <= 200 (043)
- PASS: catchup:pull:10000:100:64 / off / checkpoint / L1 / hint: 87.1 <= 200 (044)
- PASS: catchup:pull:10000:1:64 / off / equivalent / L64 / hint: 165.5 <= 200 (055)
- PASS: catchup:pull:10000:1:64 / off / checkpoint / L64 / hint: 60.08 <= 200 (056)
- PASS: live:pull:1000:1:1 / off / equivalent / L64 / hint: 112.8 <= 200 (065)
- PASS: live:pull:1000:1:1 / off / checkpoint / L64 / hint: 113.1 <= 200 (066)
- PASS: live:pull:1000:1:1 / off / equivalent / L64 / poll: 100.4 <= 200 (067)
- PASS: live:pull:1000:1:1 / off / checkpoint / L64 / poll: 131.7 <= 200 (068)
- INSUFFICIENT: live:pull:1000:1:16 / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 083,088,091
- INSUFFICIENT: live:pull:1000:1:16 / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 084,089,092
- INSUFFICIENT: live:pull:1000:1:16 / off / equivalent / L64 / poll: n/a <= 200 (none); repeats/pairs pending 085
- INSUFFICIENT: live:pull:1000:1:16 / off / checkpoint / L64 / poll: n/a <= 200 (none); repeats/pairs pending 086
- INSUFFICIENT: live:pull:1000:1000:16 / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 099
- INSUFFICIENT: live:pull:1000:1000:16 / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 100
- INSUFFICIENT: live:pull:1000:1:1:batch / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 107
- INSUFFICIENT: live:pull:1000:1:1:batch / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 108
- INSUFFICIENT: live:pull:300:1:1:rate=20 / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 115
- INSUFFICIENT: live:pull:300:1:1:rate=20 / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 116
- INSUFFICIENT: live:pull:300:1:1:rate=200 / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 123
- INSUFFICIENT: live:pull:300:1:1:rate=200 / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 124
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 131
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 132
- INSUFFICIENT: catchup:pull:2000:2000:64:sparse9 / off / equivalent / L64 / hint: n/a <= 200 (none); repeats/pairs pending 137
- INSUFFICIENT: catchup:pull:2000:2000:64:sparse9 / off / checkpoint / L64 / hint: n/a <= 200 (none); repeats/pairs pending 138

### Live effect−append <=today OR <=100ms

- PASS: live:pull:1000:1:1 / off / equivalent / L64 / hint: pull 0.6746 vs today 0 ms; <=100 alternative (065,064)
- PASS: live:pull:1000:1:1 / off / checkpoint / L64 / hint: pull 0.5793 vs today 0 ms; <=100 alternative (066,064)
- PASS: live:pull:1000:1:1 / off / equivalent / L64 / poll: pull 26.59 vs today 0 ms; <=100 alternative (067,064)
- PASS: live:pull:1000:1:1 / off / checkpoint / L64 / poll: pull 0 vs today 0 ms; <=100 alternative (068,064)
- INSUFFICIENT: live:pull:1000:1:16 / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:16 / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:16 / off / equivalent / L64 / poll: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:16 / off / checkpoint / L64 / poll: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1000:16 / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1000:16 / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:1:batch / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:1:batch / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:300:1:1:rate=20 / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:300:1:1:rate=20 / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:300:1:1:rate=200 / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:300:1:1:rate=200 / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / off / equivalent / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)
- INSUFFICIENT: live:pull:1000:1:16:rate=1000 / off / checkpoint / L64 / hint: pull n/a vs today n/a ms; <=100 alternative (none)

## Lanes/client-bound diagnostic (not a formal numeric gate)

- catchup:pull:2000:2000:64 / off / equivalent / L64 / hint: L64/L1 post-reg=1.065; L64 client cores=0.1276 (013,018,021,015). No joint client-bound signal (near-core heuristic >=0.9; not a proof).
- catchup:pull:2000:2000:64 / off / checkpoint / L64 / hint: L64/L1 post-reg=1.953; L64 client cores=0.1507 (014,019,022,016). No joint client-bound signal (near-core heuristic >=0.9; not a proof).
- catchup:pull:10000:100:64 / off / equivalent / L64 / hint: L64/L1 post-reg=0.546; L64 client cores=0.2051 (041,046,049,043). No joint client-bound signal (near-core heuristic >=0.9; not a proof).
- catchup:pull:10000:100:64 / off / checkpoint / L64 / hint: L64/L1 post-reg=0.7448; L64 client cores=0.2661 (042,047,050,044). No joint client-bound signal (near-core heuristic >=0.9; not a proof).

## Mapping and dispositions

| Run-list row | JSONL line | Exit | Disposition | Validation exclusions |
| --- | --- | --- | --- | --- |
| 001 | 17 | 0 | COMPLETE | VALID |
| 002 | 18 | 0 | COMPLETE | VALID |
| 003 | 19 | 0 | COMPLETE | VALID |
| 004 | 20 | 0 | COMPLETE | VALID |
| 005 | 21 | 0 | COMPLETE | VALID |
| 006 | 22 | 0 | COMPLETE | VALID |
| 007 | 23 | 0 | COMPLETE | VALID |
| 008 | 24 | 0 | COMPLETE | VALID |
| 009 | 25 | 0 | COMPLETE | VALID |
| 010 | 26 | 0 | COMPLETE | VALID |
| 011 | 27 | 0 | COMPLETE | VALID |
| 012 | 28 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 013 | 29 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 014 | 30 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 015 | 31 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 016 | 32 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 017 | 33 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 018 | 34 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 019 | 35 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 020 | 36 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 021 | 37 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 022 | 38 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 023 | 39 | 2 | DID_NOT_FINISH | Measurement deadline 600s reached (total internal budget 720s, finalization reserve 120s); acknowledged membership only. Worker finalization: A task was canceled. |
| 024 | 40 | 0 | COMPLETE | VALID |
| 025 | 41 | 0 | COMPLETE | VALID |
| 026 | 42 | 2 | DID_NOT_FINISH | Measurement deadline 600s reached (total internal budget 720s, finalization reserve 120s); acknowledged membership only. Worker finalization: A task was canceled. |
| 027 | 43 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 028 | 44 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 029 | 45 | 0 | COMPLETE | VALID |
| 030 | 46 | 0 | COMPLETE | VALID |
| 031 | 47 | 0 | COMPLETE | VALID |
| 032 | 48 | 0 | COMPLETE | VALID |
| 033 | 49 | 0 | COMPLETE | VALID |
| 034 | 50 | 0 | COMPLETE | VALID |
| 035 | 51 | 0 | COMPLETE | VALID |
| 036 | 52 | 0 | COMPLETE | VALID |
| 037 | 53 | 0 | COMPLETE | VALID |
| 038 | 54 | 0 | COMPLETE | VALID |
| 039 | 55 | 0 | COMPLETE | VALID |
| 040 | 56 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 041 | 57 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 042 | 58 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 043 | 59 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 044 | 60 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 045 | 61 | 2 | INVALID | observer-list proof absent; append incomplete; missing; distinct count |
| 046 | 62 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 047 | 63 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 048 | 64 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 049 | 65 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 050 | 66 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 051 | 67 | 0 | COMPLETE | VALID |
| 052 | 68 | 0 | COMPLETE | VALID |
| 053 | 69 | 0 | COMPLETE | VALID |
| 054 | 70 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 055 | 71 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 056 | 72 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 057 | 73 | 0 | COMPLETE | VALID |
| 058 | 74 | 0 | COMPLETE | VALID |
| 059 | 75 | 0 | COMPLETE | VALID |
| 060 | 76 | 0 | COMPLETE | VALID |
| 061 | 77 | 0 | COMPLETE | VALID |
| 062 | 78 | 0 | COMPLETE | VALID |
| 063 | 79 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 064 | 80 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 065 | 81 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 066 | 82 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 067 | 83 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 068 | 84 | 0 | COMPLETE_TIMING_ONLY | VALID |
| 069 | 85 | 0 | COMPLETE | VALID |
| 070 | 86 | 0 | COMPLETE | VALID |
| 071 | 87 | 0 | COMPLETE | VALID |
| 072 | 88 | 0 | COMPLETE | VALID |
