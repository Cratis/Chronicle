#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Offline #4748 analysis. No subprocesses, benchmarks, dependencies, or input writes.

Usage: python3 analyze-pull.py [--through-row 72] [--directory PATH]
       python3 analyze-pull.py --self-test

Prints Markdown to stdout. Exit 0 means analysis completed, NOT benchmark GO;
exit 2 means input/mapping could not be analyzed. Gate verdicts are in the report.
Repeat identity is scenario + profile + pull options; occurrence order and status
completion timestamps disambiguate identical repeats. Historical results before
status-pre-fix.tsv's last completion are excluded, not pooled with this matrix.
"""
import argparse
import collections
import datetime as dt
import json
import math
from pathlib import Path
import re
import statistics
import sys

VALID = {"COMPLETE", "COMPLETE_TIMING_ONLY"}
SYSTEM = {
    "$system.Cratis.Chronicle.Observation.Webhooks.WebhookReactor",
    "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
    "$system.Cratis.Chronicle.EventSequences.Migrations.EventTypeGenerationMigrationReactor",
    "$system.Cratis.Chronicle.EventSequences.EventSequencesReactor", "$system.patterns",
}
EVENT = "c0b93c8e-3f3f-4f3f-8f3f-3f3f3f3f3f3f"


def timestamp(value):
    if not value:
        return None
    value = value.replace("Z", "+00:00")
    # .NET emits 0–7 fractional digits; older Python accepts only 3 or 6.
    value = re.sub(r"\.(\d+)", lambda m: "." + (m.group(1) + "000000")[:6], value)
    return dt.datetime.fromisoformat(value)


def record_time(row):
    wire = row.get("pullWireCommands") or {}
    mongo = row.get("mongo") or {}
    values = [wire.get("endUtc"), row.get("systemDrainStartedUtc")]
    values += [b.get("start") for b in mongo.get("boundary", [])]
    return next((timestamp(v) for v in values if v), None)


def signature(row):
    options = row.get("pullOptions")
    base = (row["scenario"], bool(row["profileEnabled"]))
    return base + (tuple(options.get(k) for k in ("stats", "lanes", "wake", "page", "flushMs")) if options else ())


def parse_plan(text):
    headings = list(re.finditer(r"^(\d+)\. \*\*([^;]+); profile=(on|off); ([^;]+); lanes=(\d+); wake=(\w+)\*\*", text, re.M))
    plan = {}
    for index, match in enumerate(headings):
        number, scenario, profile, mode, lanes, wake = match.groups()
        block = text[match.end():headings[index + 1].start() if index + 1 < len(headings) else len(text)]
        page = int(re.search(r"PULL_PAGE=(\d+)", block).group(1))
        flush = int(re.search(r"PULL_FLUSH_MS=(\d+)", block).group(1))
        key = (scenario, profile == "on")
        if ":pull:" in scenario:
            key += (mode, int(lanes), wake, page, flush)
        plan[int(number)] = key
    if not plan or list(plan) != list(range(1, len(plan) + 1)):
        raise ValueError("Run-list is empty or non-contiguous")
    return plan


def statuses(path):
    result = {}
    if path.exists():
        for line in path.read_text().splitlines():
            number, attempt, code, when = line.split("\t")
            # Admission-only attempts do not produce a benchmark row.
            if int(code) != 75:
                if int(number) in result:
                    raise ValueError(f"Duplicate substantive status for row {number}")
                result[int(number)] = (int(code), timestamp(when))
    return result


def map_results(directory, through):
    plan = parse_plan((directory / "run-list.md").read_text())
    status = statuses(directory / "status.tsv")
    prior = statuses(directory / "status-pre-fix.tsv")
    cutoff = max((when for _, when in prior.values()), default=None)
    lines = (directory / "results.jsonl").read_bytes().splitlines()
    raw = []
    partial = 0
    for index, line in enumerate(lines, 1):
        try:
            raw.append((index, json.loads(line)))
        except json.JSONDecodeError:
            if index != len(lines):
                raise
            partial += 1  # A concurrent append is not a completed sample.
    mapped, cursor, previous = {}, 0, cutoff
    for number, (code, end) in sorted(status.items()):
        if through is not None and number > through:
            continue
        key = plan[number]
        candidates = []
        for offset in range(cursor, len(raw)):
            _, row = raw[offset]
            when = record_time(row)
            if signature(row) != key or row.get("deliberateDrop"):
                continue
            if when and ((previous and when <= previous) or when > end):
                continue
            candidates.append(offset)
        if not candidates:
            raise ValueError(f"No result matches run-list {number:03d}: {key}")
        # Untimed setup failures are mapped by physical order between neighboring
        # timed results; never synthesize a timing or performance value for them.
        offset = candidates[0]
        if len(candidates) > 1 and any(record_time(raw[c][1]) is not None for c in candidates[1:]):
            raise ValueError(f"Ambiguous result mapping for run-list {number:03d}")
        line, row = raw[offset]
        row = dict(row, _number=number, _line=line, _exit=code)
        mapped[number] = row
        cursor, previous = offset + 1, end
    if not mapped:
        raise ValueError("No completed post-fix matrix rows")
    return plan, mapped, len(raw), partial


def bson(value):
    if isinstance(value, str):
        return json.loads(value)
    return value


def scalar(value):
    if isinstance(value, dict):
        return float(value.get("$numberDecimal", value.get("$numberLong")))
    return value


def correctness(row):
    """Check captured evidence as well as harness disposition (not COMPLETE alone)."""
    problems = []
    appendonly = row["scenario"].startswith("appendonly:")
    if row.get("observerListPassed") is not True:
        problems.append("observer-list proof absent")
    if row.get("appendFinished") is not True or row.get("expected") != row.get("requested"):
        problems.append("append incomplete")
    if not appendonly:
        accounting = row.get("accounting") or {}
        for name in ("missing", "unexpected", "duplicates", "firstDeliveryOrderViolations", "payloadMismatches"):
            if name not in accounting or accounting[name]:
                problems.append(name)
        if accounting.get("distinctDelivered") != row.get("expected"):
            problems.append("distinct count")
        if row.get("payloadMismatches") != 0:
            problems.append("payload")
    if row.get("isPull"):
        for name in ("observers", "endObservers"):
            ids = [bson(doc)["_id"] for doc in row.get(name, [])]
            if len(ids) != 5 or set(ids) != SYSTEM:
                problems.append(name + " not exactly five system observers")
        if row.get("pullCorrectnessFailure") or row.get("pullTailExact") is not True:
            problems.append("pull tail/gap/handled invariant")
        progress = bson(row.get("pullProgressDocument") or "{}")
        warmup = int(row["scenario"].startswith("live:"))
        tail = row["expected"] + row.get("unrelatedAppended", 0) + warmup - 1
        if scalar(progress.get("w")) != tail or scalar(progress.get("r")) != tail:
            problems.append("captured w/r != expected tail")
        if progress.get("handled") != row["expected"] + warmup:
            problems.append("checkpoint handled count")
        if (row.get("pullOptions") or {}).get("stats") == "equivalent":
            counts = [bson(doc) for doc in row.get("pullCounts", [])]
            if sum(doc.get("counts", {}).get(EVENT, 0) for doc in counts) != row["expected"] + warmup:
                problems.append("per-key count total")
    if row.get("profileEnabled"):
        mongo = row.get("mongo") or {}
        if not any(op["owner"] == "probe" and op["commands"] > 0 for op in mongo.get("rows", [])) and not appendonly:
            problems.append("no probe-owned operations")
        if not mongo.get("boundary") or any(not b.get("retained") or not b.get("registeredRetained") for b in mongo["boundary"]):
            problems.append("profile boundary lost")
        if mongo.get("unparsed") != 0:
            problems.append("unparsed profiler operations")
        if row.get("isPull"):
            wire = row.get("pullWireCommands") or {}
            account = mongo.get("pullCommandAccounting") or {}
            if account.get("writes") != wire.get("writes") or account.get("writeStatements") != wire.get("documentsWritten"):
                problems.append("wire/profiler reconciliation")
            if account.get("adjustedCommands") != mongo.get("commands", 0) - account.get("writeStatements", 0) + account.get("writes", 0):
                problems.append("adjusted total inconsistent")
    return problems


def valid(row):
    return row["_exit"] == 0 and row["status"] in VALID and not row.get("deliberateDrop") and not correctness(row)


def safe_ratio(a, b):
    return a / b if a is not None and b is not None and b > 0 else None


def metrics(row):
    n = row["expected"]
    mongo = row.get("mongo")
    wire = row.get("pullWireCommands") or {}
    result = {"full": row.get("eventsPerSecond"), "post": row.get("eventsPerSecondFromRegistered"),
              "registered": row.get("registeredMs"), "effect": row.get("effectMs"), "durable": row.get("durableMs"),
              "gap": row.get("durableGapMs"), "client": row.get("clientCpuMs"), "cores": row.get("clientCpuCores")}
    if row.get("effectMs") is not None and row.get("appendMs") is not None:
        result["drain"] = max(0, row["effectMs"] - row["appendMs"])
    if row.get("isPull"):
        result["readsDurable"] = safe_ratio(row.get("pullReadsAtDurable"), n)
        result["readsFull"] = safe_ratio(row.get("pullReads"), n)
        # For a valid finite log at exact w=r=tail, every normal Save has completed
        # before DurableMs; Flush requires W movement, and Dispose has no write.
        # Thus wire writes through quiescence equal writes through DurableMs.
        # Reads do NOT have that property: use the explicit AtDurable snapshot.
        if row.get("pullTailExact") and row.get("durableMs") is not None:
            result["writes"] = safe_ratio(wire.get("writes"), n)
            result["docs"] = safe_ratio(wire.get("documentsWritten"), n)
    if mongo:
        result["raw"] = mongo["commands"] / n
        result["adjusted"] = mongo["pullCommandAccounting"]["adjustedCommands"] / n
        writes = [op for op in mongo["rows"] if op["op"] in ("insert", "update", "remove")]
        owned = [op for op in writes if op["owner"] == "probe"]
        if not row.get("isPull"):
            result["writes"] = sum(op["commands"] for op in owned) / n
            result["docs"] = sum(op["documentsInserted"] if op["op"] == "insert" else op["commands"] for op in owned) / n
        result["allWrites"] = sum(op["commands"] for op in writes) / n
        result["allDocs"] = sum(op["documentsInserted"] if op["op"] == "insert" else op["commands"] for op in writes) / n
    cpu = row.get("containerCpu") or {}
    start, end = cpu.get("start"), cpu.get("end")
    if start and end:
        result["combinedCpu"] = (end["usageUsec"] - start["usageUsec"]) / 1000
        old = {p["pid"]: p for p in start["processes"]}
        for process in end["processes"]:
            before = old.get(process["pid"])
            if before and start["hz"] == end["hz"]:
                name = "mongoCpu" if process["name"] == "mongod" else "kernelCpu"
                result[name] = (process["userTicks"] + process["systemTicks"] - before["userTicks"] - before["systemTicks"]) * 1000 / end["hz"]
    return result


def median(rows, metric):
    values = [metrics(row).get(metric) for row in rows if valid(row)]
    values = [v for v in values if v is not None and math.isfinite(v)]
    return statistics.median(values) if values else None


def fmt(value):
    return "n/a" if value is None else f"{value:,.4g}"


def spread(rows, metric):
    values = [metrics(r).get(metric) for r in rows if valid(r)]
    values = [v for v in values if v is not None and math.isfinite(v)]
    return f"{fmt(statistics.median(values))} [{fmt(min(values))}–{fmt(max(values))}]" if values else "n/a"


def refs(rows):
    return ",".join(f"{r['_number']:03d}" for r in rows) or "none"


def key_label(key):
    return key[0] + (f" / {'on' if key[1] else 'off'}" + (f" / {key[2]} / L{key[3]} / {key[4]}" if len(key) > 2 else " / today"))


class Analysis:
    def __init__(self, plan, mapped):
        self.plan, self.mapped = plan, mapped
        self.groups = collections.defaultdict(list)
        self.planned = collections.defaultdict(list)
        for number, key in plan.items():
            self.planned[key].append(number)
        for row in mapped.values():
            self.groups[signature(row)].append(row)
        self.gates, self.details, self.alerts = [], [], []

    def rows(self, key):
        return self.groups.get(key, [])

    def gate(self, name, checks):
        states = [state for state, _ in checks]
        state = "FAIL" if "FAIL" in states else "PASS" if states and all(s == "PASS" for s in states) else "INSUFFICIENT"
        relevant = [(s, text) for s, text in checks if s == state]
        evidence = "; ".join(text for _, text in relevant[:2])
        if len(relevant) > 2:
            evidence += f"; +{len(relevant) - 2} {state.lower()} comparisons (details below)"
        pending = sum(s == "INSUFFICIENT" for s in states)
        if state == "FAIL" and pending:
            evidence += f"; {pending} other comparisons insufficient"
        self.gates.append((name, state, evidence or "No completed evidence"))
        self.details.append((name, checks))

    def compare(self, key, metric, threshold, direction="le", other=None, cap=None):
        rows = self.rows(key)
        value = median(rows, metric)
        supporting = list(rows)
        if cap:
            supporting += self.rows(cap)
            ceiling = median(self.rows(cap), metric)
            value = min(value, ceiling) if value is not None and ceiling is not None else None
        if other:
            supporting += self.rows(other)
            value = safe_ratio(value, median(self.rows(other), metric))
        state = "INSUFFICIENT" if value is None else "PASS" if (value <= threshold if direction == "le" else value >= threshold) else "FAIL"
        pending = [n for k in (key, other, cap) if k for n in self.planned.get(k, []) if n not in self.mapped]
        evidence = f"{key_label(key)}: {fmt(value)} {'<=' if direction == 'le' else '>='} {fmt(threshold)} ({refs(supporting)})"
        if pending:
            evidence += "; repeats/pairs pending " + ",".join(f"{n:03d}" for n in pending)
            if state == "PASS":
                state = "INSUFFICIENT"
        return state, evidence

    def evaluate(self):
        rows = list(self.mapped.values())
        defects = [r for r in rows if r.get("isPull") and (r["status"] == "DEFECT" or r.get("pullCorrectnessFailure"))]
        if defects:
            self.alerts.append("STOP: pull correctness failure in " + refs(defects))
        invalid = [r for r in rows if not valid(r)]
        pending = len(self.plan) - len(rows)
        self.gate("Correctness / validity, every run", [(
            "FAIL" if defects or any(r["status"] in VALID and correctness(r) for r in rows) else "INSUFFICIENT" if pending or invalid else "PASS",
            f"{sum(valid(r) for r in rows)}/{len(rows)} valid; invalid/failed {refs(invalid)}; {pending} pending; pull structural defects {refs(defects)}")])
        cap = ("catchup:reactor:10000:1:64", False)
        for mode in ("equivalent", "checkpoint"):
            a = ("catchup:pull:2000:2000:64", False, mode, 64, "hint", 1000, 50)
            b = ("catchup:pull:10000:10000:64", False, mode, 64, "hint", 1000, 50)
            c = ("catchup:pull:10000:100:64", False, mode, 64, "hint", 1000, 50)
            today2k = ("catchup:reactor:2000:2000:64", False)
            ratio = self.compare(a, "post", 10, "ge", today2k, cap)
            self.gate(f"2k projected >=10x ({mode})", [ratio])
            self.gate(f"Avoid STOP: 2k projected >=5x ({mode})", [self.compare(a, "post", 5, "ge", today2k, cap)])
            ceiling = median(self.rows(cap), "post")
            projected = self.compare(b, "post", min(2000, ceiling) if ceiling is not None else 2000, "ge", cap=cap)
            self.gate(f"10k finishes; projected >=min(2000,ceiling) ({mode})", [projected])
            self.gate(f"10k/100-key rate >=0.5 ({mode})", [self.compare(b, "post", .5, "ge", c)])
            self.gate(f"100 keys >=today ({mode})", [self.compare(c, "post", 1, "ge", ("catchup:reactor:10000:100:64", False))])
            sparse = ("catchup:pull:2000:2000:64:sparse9", False, mode, 64, "hint", 1000, 50)
            self.gate(f"Sparse relevant rate >=3x ({mode})", [self.compare(sparse, "post", 3, "ge", ("catchup:reactor:2000:2000:64:sparse9", False))])
            stop = self.compare(a, "post", 5, "ge", ("catchup:reactor:2000:2000:64", False), cap)
            if stop[0] == "FAIL":
                self.alerts.append("STOP trigger: projected 2k catch-up <5x: " + stop[1])
        pullkeys = [k for k in self.planned if len(k) > 2]
        costs = [k for k in pullkeys if k[1] and k[2] == "equivalent"]
        catchup = [k for k in costs if k[0].startswith("catchup:")]
        self.gate("Equivalent catch-up writes/event <=0.02", [self.compare(k, "writes", .02) for k in catchup])
        high = [k for k in costs if k[0].startswith("live:") and (k[0] == "live:pull:1000:1:16" or any(f"rate={n}" in k[0] for n in (200, 1000)))]
        low = [k for k in costs if k[0].endswith("rate=20")]
        self.gate("Equivalent live writes/event <=0.2 (>=200 paced; 1:16)", [self.compare(k, "writes", .2) for k in high])
        self.gate("Equivalent paced20 writes/event <=2", [self.compare(k, "writes", 2) for k in low])
        livekeys = [k for k in pullkeys if k[0].startswith("live:")]
        reads = [k for k in livekeys if k[0] in ("live:pull:1000:1:1", "live:pull:1000:1:16")]
        self.gate("Live reads/event <=1 (1:1); <=0.3 (:16)", [self.compare(k, "readsDurable", .3 if k[0].endswith(":16") else 1) for k in reads])
        totals, stops = [], []
        for key in [k for k in livekeys if k[1]]:
            twin = (key[0].replace("live:pull:", "appendonly:reactor:"), True)
            today = (key[0].replace("live:pull:", "live:reactor:"), True)
            base = median(self.rows(twin), "adjusted")
            current = median(self.rows(today), "adjusted")
            pull = median(self.rows(key), "adjusted")
            numerator = pull - base if pull is not None and base is not None else None
            denominator = current - base if current is not None and base is not None else None
            ratio = safe_ratio(numerator, denominator) if numerator is not None and numerator >= 0 else None
            pending = any(n not in self.mapped for k in (key, twin, today) for n in self.planned[k])
            state = "INSUFFICIENT" if ratio is None else "FAIL" if ratio > .3 else "INSUFFICIENT" if pending else "PASS"
            evidence = f"{key_label(key)}: ({fmt(pull)}−{fmt(base)})/({fmt(current)}−{fmt(base)})={fmt(ratio)} ({refs(self.rows(key) + self.rows(today) + self.rows(twin))})"
            totals.append((state, evidence))
            if key[2] == "equivalent":
                p, t = median(self.rows(key), "writes"), median(self.rows(today), "writes")
                state = "INSUFFICIENT" if p is None or t is None else "FAIL" if p >= t else "INSUFFICIENT" if pending else "PASS"
                stops.append((state, f"{key_label(key)}: pull {fmt(p)} vs today {fmt(t)} writes/event ({refs(self.rows(key) + self.rows(today))})"))
        self.gate("Live-minus-matching-twin cost <=30% of today", totals)
        self.gate("Avoid STOP: equivalent live writes <today", stops)
        timing = [k for k in pullkeys if not k[1]]
        self.gate("Durable gap <=FLUSH_MS+150ms", [self.compare(k, "gap", k[-1] + 150) for k in timing])
        lag = []
        for key in [k for k in livekeys if not k[1]]:
            today = (key[0].replace("live:pull:", "live:reactor:"), False)
            p, t = median(self.rows(key), "drain"), median(self.rows(today), "drain")
            state = "INSUFFICIENT" if p is None or (p > 100 and t is None) else "PASS" if p <= 100 or p <= t else "FAIL"
            if state == "PASS" and any(n not in self.mapped for k in (key, today) for n in self.planned[k]):
                state = "INSUFFICIENT"
            lag.append((state, f"{key_label(key)}: pull {fmt(p)} vs today {fmt(t)} ms; <=100 alternative ({refs(self.rows(key) + self.rows(today))})"))
        self.gate("Live effect−append <=today OR <=100ms", lag)
        for key, numbers in self.planned.items():
            if len(numbers) > 1 and all(n in self.mapped for n in numbers):
                good = sum(valid(self.mapped[n]) for n in numbers)
                if good < len(numbers):
                    self.alerts.append(f"Coverage cannot recover within remaining matrix: {key_label(key)} has {good}/{len(numbers)} valid repeats ({','.join(f'{n:03d}' for n in numbers)}). Metrics use the valid-sample median, not an invented third sample.")
        for name, state, evidence in self.gates:
            if state == "FAIL" and name.startswith("Avoid STOP"):
                self.alerts.append("STOP trigger observed: " + evidence)
        return self


def failure_explanations(mapped):
    lines = []
    for number in (23, 26, 45):
        row = mapped.get(number)
        if row is None:
            continue
        if number in (23, 26):
            jobs = [bson(j) for j in row.get("persistedJobs", [])]
            job = next((j for j in jobs if j.get("details") == "probe-4748"), {})
            progress = job.get("progress", {})
            failures = [bson(j) for j in row.get("failedJobSteps", [])]
            messages = [message for j in failures if j.get("observerKey", {}).get("observerId") == "probe-4748" for change in j.get("statusChanges", []) for message in change.get("exceptionMessages", [])]
            confirmed = any("00:00:30" in m and "ReportHandledEvents" in m for m in messages)
            accounting = row.get("accounting") or {}
            lines.append(f"- **{number:03d}** ({row['scenario']}, profile={'on' if row['profileEnabled'] else 'off'}, JSONL {row['_line']}): {row['status']}/exit {row['_exit']}; {accounting.get('distinctDelivered')}/{row['expected']} exact deliveries, but no durable completion within the 600s measurement budget. Probe job captured status {job.get('status')}, {progress.get('successfulSteps')} successful/{progress.get('failedSteps')} failed of {progress.get('totalSteps')} steps. {'Confirmed today-engine #4954 signature: retained probe-step exceptions name the default 30s IObserver.ReportHandledEvents timeout.' if confirmed else 'Cause not proven by retained exceptions.'} These are not pull failures and are excluded from performance medians; teardown status is not success.")
        else:
            lines.append(f"- **045** ({row['scenario']}, profile=off, JSONL {row['_line']}): INVALID/exit {row['_exit']} **during seeding**, gRPC Cancelled: `gRPC call disposed`, after {row['expected']}/{row['requested']} acknowledged appends; system drain/observer registration/measurement never ran (wallMs={row['wallMs']}). Timing-only final check also timed out. Unknown outstanding RPC outcomes, not measured delivery loss. The retained kernel log ends after startup/authentication, with no caught #4954 ReportHandledEvents/job-failure evidence. **Today-side setup/transport failure, root cause unproven; do not attribute it to #4954 or pull.**")
    return lines


def report(directory, plan, mapped, raw_count, partial):
    analysis = Analysis(plan, mapped).evaluate()
    print("# Step 2 pull coordinator — interim/offline matrix analysis\n")
    print(f"Snapshot: run-list rows {min(mapped):03d}–{max(mapped):03d}; {len(mapped)}/{len(plan)} status-completed rows; {raw_count} JSONL records read; {partial} partial trailing records ignored. Engine d5d0eea66d; harness e27b3ff5ab per handoff. No build, benchmark, process control, GitHub operation, or source edit performed.\n")
    print("## Immediate flags\n")
    for text in analysis.alerts:
        print("- " + text)
    if not any(r.get("isPull") and (r["status"] == "DEFECT" or r.get("pullCorrectnessFailure")) for r in mapped.values()):
        print("- No captured pull structural correctness failure. Performance FAIL is not delivery DEFECT.")
    failures = [name for name, state, _ in analysis.gates if state == "FAIL"]
    if failures:
        print("- Observed gate failures: " + "; ".join(failures) + ". Remaining different scenarios cannot replace completed single-sample/complete-repeat results; unfinished repeat medians remain provisional.")
    print("\n## Section 7 gates\n\n| Gate | Verdict | Evidence (run-list rows) |\n| --- | --- | --- |")
    for name, state, evidence in analysis.gates:
        print(f"| {name} | **{state}** | {evidence} |")
    states = [s for _, s, _ in analysis.gates]
    stop = any(text.startswith("STOP") for text in analysis.alerts)
    decision = "STOP trigger observed" if stop else "GO" if states and all(s == "PASS" for s in states) else "INVESTIGATE — not GO; pending/invalid evidence remains" if "INSUFFICIENT" in states else "INVESTIGATE (valid performance gate failures)"
    print(f"\n**Decision: {decision}.** Missing evidence is never a pass. The plan limits investigation to two days; this script does not infer its start time.\n")
    print("## Three exit-2 rows\n")
    print("\n".join(failure_explanations(mapped)))
    print("\n## Projected catch-up: deciding post-registration and full-clock context\n")
    print("| Mode / thin-key shape | Pull post-reg | Today 1-key ceiling post-reg | Projected post-reg | Projected/today post-reg | Projected full (context only) | Projected/today full (context only) |\n| --- | ---: | ---: | ---: | ---: | ---: | ---: |")
    ceiling_rows = analysis.rows(("catchup:reactor:10000:1:64", False))
    for mode in ("equivalent", "checkpoint"):
        for shape in ("2000:2000:64", "10000:10000:64"):
            pull = analysis.rows(("catchup:pull:" + shape, False, mode, 64, "hint", 1000, 50))
            today = analysis.rows(("catchup:reactor:" + shape, False))
            p, c = median(pull, "post"), median(ceiling_rows, "post")
            pf, cf = median(pull, "full"), median(ceiling_rows, "full")
            projected = min(p, c) if p is not None and c is not None else None
            full = min(pf, cf) if pf is not None and cf is not None else None
            print(f"| {mode} / {shape} | {fmt(p)} | {fmt(c)} | {fmt(projected)} | {fmt(safe_ratio(projected, median(today, 'post')))} | {fmt(full)} | {fmt(safe_ratio(full, median(today, 'full')))} |")
    print("\n## Method and limitations\n")
    print("- VALID = exit0 + COMPLETE/COMPLETE_TIMING_ONLY + captured membership, payload, append, observer, tail/count and profiler reconciliation checks. INVALID, DID_NOT_FINISH, JOB_FAILED, DEFECT and EXPECTED_DROP are never performance samples. Historical smokes and the stopped pre-fix attempt are excluded by status cutoff, scenario/options matching, completion windows and occurrence order; no fixed JSONL offset is assumed. Mapping errors fail closed. Untimed setup failures use physical order between neighboring timed runs.")
    print("- Group by full scenario, profile mode, stats mode, lanes, wake, page and flush. Values are median [min–max] of valid samples, not confidence intervals. Pending repeats prevent a PASS; an already observed threshold violation is shown as FAIL but may change when repeats finish. Failed prescribed repeats lower n and are explicitly flagged; remaining valid medians are still computed.")
    print("- Timing gates use profile-off COMPLETE_TIMING_ONLY, post-registration ev/s. Projected thin-key rate = min(pull median post-reg, fresh today 1-key median post-reg). The 100-key and sparse ratios use the uncapped relevant-event rate. Full ev/s is context only, per the handoff superseding the earlier full-clock gate. Registration observation is approximate and can lag today's start of work. L1 and poll controls remain separate; no pooling with default L64/hint.")
    print("- Live reads/event = pullReadsAtDurable/expected; full-window reads are shown separately, never silently subtracted. Progress writes/event = actual dedicated-client wire attempts/expected, including handled-count bulks in equivalent mode. Although the wire snapshot ends after quiescence, for VALID finite exact-tail runs all Save writes precede DurableMs: W is then at final tail, Save requires W movement, and Dispose writes nothing (harness/ProbePull.cs:139–178; ProbePullMeasure.cs:120–159). There are no per-command timestamps in JSONL; this equality depends on that harness invariant, not on treating quiescent reads as zero. Timing-only wire counters are diagnostic, not profiler cost proof.")
    print("- Cost gates use profiled equivalent writes. For total live cost, adjustedCommands replaces ONLY proven coordinator update statements with reconciled wire writes. All reads/system/shared operations remain; inherited raw totals are retained below. Subtract the exact matching fresh shape/profile twin from BOTH pull and today medians. Nonpositive today residual or negative pull residual is INSUFFICIENT/noise, never clamped to zero. Twins are single samples; subtraction includes run-to-run system-observer variance.")
    print("- Docs/event are document write attempts, not unique keys or modified documents: pull dedicated-client documentsWritten includes checkpoints and per-key targets; today uses probe-owned update/remove profiler statements plus inserted documents. The profiler's upsert inserted-document field is not reliable, so it is not used to claim exact physical insert counts. Today's catch-up probe-owned write counts exclude shared job machinery; all-write profiler statement counts are also shown. Terminal transaction cost is NOT PROFILED. Batch transaction counters remain checked by the inherited harness; no crash/failover/fencing proof is claimed.")
    print("- CPU snapshots span the measurement-through-quiescence window, not just effects; container cgroup and per-process kernel/mongod CPU are separate from client CPU (which includes pull). The 1ms flusher polling remains an optimistic CPU caveat. Step 1 RESULTS.md is context only (mixed generations/profile-on); it is not substituted for fresh paired baselines. Noop removed; projection N/A per run-list.")
    print("\n## Per-scenario valid timing and CPU\n\nAll time and CPU values are ms; CPU is consumed CPU time, not elapsed time.\n")
    print("| Scenario / profile / mode / lanes / wake | valid/planned; evidence | full ev/s | post-reg ev/s | registration | effect | durable | durable gap | drain | client CPU | client cores | cgroup CPU | kernel CPU | mongod CPU |\n| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |")
    for key in analysis.planned:
        rows = analysis.rows(key)
        if rows:
            cells = [spread(rows, m) for m in ("full", "post", "registered", "effect", "durable", "gap", "drain", "client", "cores", "combinedCpu", "kernelCpu", "mongoCpu")]
            print(f"| {key_label(key)} | {sum(valid(r) for r in rows)}/{len(analysis.planned[key])}; {refs(rows)} | " + " | ".join(cells) + " |")
    print("\n## Per-scenario valid cost\n\n| Scenario / profile / mode / lanes / wake | evidence | reads/event through durable | reads/event full window | observer progress wire writes/event | observer document attempts/event | raw all ops/event | adjusted all ops/event | all write profiler statements/event | all document attempts/event |\n| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |")
    for key in analysis.planned:
        rows = analysis.rows(key)
        if rows:
            cells = [spread(rows, m) for m in ("readsDurable", "readsFull", "writes", "docs", "raw", "adjusted", "allWrites", "allDocs")]
            print(f"| {key_label(key)} | {refs(rows)} | " + " | ".join(cells) + " |")
    print("\n## Detailed gate comparisons\n")
    for name, checks in analysis.details:
        print(f"### {name}\n")
        for state, evidence in checks:
            print(f"- {state}: {evidence}")
        print()
    print("## Lanes/client-bound diagnostic (not a formal numeric gate)\n")
    for key in analysis.planned:
        if len(key) > 2 and not key[1] and key[0].startswith("catchup:") and key[3] == 64 and key[4] == "hint":
            one = key[:3] + (1,) + key[4:]
            if one not in analysis.planned:
                continue
            p, q, cpu = median(analysis.rows(key), "post"), median(analysis.rows(one), "post"), median(analysis.rows(key), "cores")
            bound = p is not None and q is not None and cpu is not None and p <= q and cpu >= .9
            print(f"- {key_label(key)}: L64/L1 post-reg={fmt(safe_ratio(p, q))}; L64 client cores={fmt(cpu)} ({refs(analysis.rows(key) + analysis.rows(one))}). {'Client-bound signal' if bound else 'No joint client-bound signal'} (near-core heuristic >=0.9; not a proof).")
    print("\n## Mapping and dispositions\n\n| Run-list row | JSONL line | Exit | Disposition | Validation exclusions |\n| --- | --- | --- | --- | --- |")
    for number, row in sorted(mapped.items()):
        issues = correctness(row)
        note = "VALID" if valid(row) else "; ".join(issues) or (row.get("reason") or "")[:200]
        print(f"| {number:03d} | {row['_line']} | {row['_exit']} | {row['status']} | {note.replace(chr(10), ' ').replace('|', '/')} |")


def self_test():
    checks = 0
    def check(condition):
        nonlocal checks
        assert condition
        checks += 1
    row = {"scenario": "live:pull:1000:1:16", "profileEnabled": True,
           "pullOptions": dict(stats="equivalent", lanes=64, wake="hint", page=1000, flushMs=50)}
    check(signature(row) == (row["scenario"], True, "equivalent", 64, "hint", 1000, 50))
    check(signature(dict(row, pullOptions=dict(row["pullOptions"], lanes=1))) != signature(row))
    check(signature(dict(row, pullOptions=dict(row["pullOptions"], stats="checkpoint"))) != signature(row))
    check(safe_ratio(2, 0) is None)
    check(safe_ratio(None, 1) is None)
    check(safe_ratio(2, 4) == .5)
    check(record_time({"pullWireCommands": {"endUtc": "2026-10-16T00:00:00Z"}}) == timestamp("2026-10-16T00:00:00Z"))
    check(scalar({"$numberDecimal": "1999"}) == 1999)
    check(timestamp("2026-10-15T23:21:22.60502Z").microsecond == 605020)
    check(timestamp("2026-10-15T23:21:22.1234567Z").microsecond == 123456)
    base = dict(scenario="live:reactor:2:1:1", profileEnabled=False,
                observerListPassed=True, appendFinished=True, expected=2, requested=2,
                payloadMismatches=0, status="COMPLETE_TIMING_ONLY", _exit=0,
                accounting=dict(expected=2, distinctDelivered=2, duplicates=0,
                                firstDeliveryOrderViolations=0, missing=[], unexpected=[], payloadMismatches=0))
    check(valid(base))
    for name, value in (("missing", [1]), ("unexpected", [1]), ("duplicates", 1),
                        ("firstDeliveryOrderViolations", 1), ("payloadMismatches", 1)):
        check(not valid(dict(base, accounting=dict(base["accounting"], **{name: value}))))
    for status in ("INVALID", "DID_NOT_FINISH", "JOB_FAILED", "DEFECT", "EXPECTED_DROP"):
        check(not valid(dict(base, status=status)))
    check(not valid(dict(base, _exit=2)))
    check(not valid(dict(base, deliberateDrop=True)))
    check(median([base, dict(base, _exit=2, effectMs=999999)], "effect") is None)
    values = [dict(base, effectMs=v) for v in (2, 3, 100)]
    check(median(values + [dict(base, _exit=2, effectMs=0)], "effect") == 3)
    check(metrics(base).get("raw") is None)
    sample = "1. **catchup:pull:2:2:64; profile=on; equivalent; lanes=64; wake=hint**\nPULL_PAGE=1000 PULL_FLUSH_MS=50"
    check(parse_plan(sample)[1] == ("catchup:pull:2:2:64", True, "equivalent", 64, "hint", 1000, 50))
    analysis = Analysis({}, {})
    analysis.gate("empty", [])
    check(analysis.gates[-1][1] == "INSUFFICIENT")
    analysis.gate("mixed", [("PASS", "a"), ("FAIL", "b"), ("INSUFFICIENT", "c")])
    check(analysis.gates[-1][1] == "FAIL")
    print(f"Self-test: {checks} assertions passed")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, default=Path(__file__).resolve().parent)
    parser.add_argument("--through-row", type=int, help="Only status-completed rows up to this matrix row")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return
    try:
        report(args.directory, *map_results(args.directory, args.through_row))
    except (ValueError, KeyError, OSError, TypeError) as error:
        print(f"Analysis could not run: {error}", file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
