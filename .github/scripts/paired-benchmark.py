# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Build two refs, measure alternating pairs, and compare raw clustering records (stdlib only)."""

import argparse
from collections import Counter
from datetime import datetime, timezone
import json
import math
import os
from pathlib import Path
import platform
import shutil
import signal
import statistics
import subprocess
import sys
import time

PROJECT = Path("Benchmarks/Chronicle.Benchmarks.Clustering")
SIDES = ("before", "after")
REBUILD_SERIES = {
    "events-per-second": True,
    "request-to-swap-seconds": False,
    "post-swap-catch-up-seconds": False,
    "mongo-commands-per-event": False,
    "grain-calls-per-event": False,
}
REBUILD_FIELDS = ("StartupFenceCoverageSeconds", "PeakWorkingSetBytes", "AllocatedBytes",
                  "ServedList.P99Ms", "KernelFindByKey.P99Ms")
REPLAY_FIELDS = ("ReplaySeconds", "EventsPerSecond", "MongoCommandsPerEvent", "GrainCallsPerEvent")
ACTIVATIONS = "ProjectionObserverSubscriber@observers"


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def append_summary(text, path=None):
    destination = path or os.environ.get("GITHUB_STEP_SUMMARY")
    if destination:
        with Path(destination).open("a", encoding="utf-8") as stream:
            stream.write(text + "\n")


def validate_inputs(repetitions, before, after, timeout_seconds=1200):
    if not 1 <= int(repetitions) <= 10:
        raise ValueError("repetitions must be 1-10")
    if not 1 <= int(timeout_seconds) <= 3000:
        raise ValueError("timeout_seconds must be 1-3000")
    for side, ref in zip(SIDES, (before, after)):
        if not ref.strip():
            raise ValueError(f"{side} ref must not be empty or whitespace")


def order(n):
    return [side for index in range(n) for side in (SIDES if index % 2 == 0 else SIDES[::-1])]


def matrix(target, scenarios, topologies):
    if target == "replay-shape":
        return [{"cell": "replay-shape", "scenario": "", "topology": ""}]
    if target != "rebuild":
        raise ValueError("target must be rebuild or replay-shape")
    scenarios = validated_list(scenarios, ("S1", "S2", "S3", "S4"), "scenario")
    topologies = validated_list(topologies, ("single", "two", "split-jobs", "three"), "topology")
    if len(scenarios) * len(topologies) > 8:
        raise ValueError("matrix must contain at most 8 cells")
    return [{"cell": f"rebuild-{scenario}-{topology}", "scenario": scenario, "topology": topology}
            for scenario in scenarios for topology in topologies]


def validated_list(value, choices, label):
    items = list(dict.fromkeys(item.strip() for item in value.split(",")))
    if any(item not in choices for item in items):
        raise ValueError(f"{label} must be one of {', '.join(choices)}")
    return items


def validate_size(target, size):
    if target != "replay-shape" or size == "small":
        return
    if not size.isdecimal() or int(size) <= 0:
        raise ValueError("replay-shape size must be small or a positive numeric event count")


def plan(_args):
    validate_inputs(os.environ["REPETITIONS"], os.environ["BEFORE_SHA"], os.environ["AFTER_SHA"],
                    os.environ.get("TIMEOUT_SECONDS", "1200"))
    validate_size(os.environ["TARGET"], os.environ.get("SIZE", "small"))
    cells = matrix(os.environ["TARGET"], os.environ["SCENARIOS"], os.environ["TOPOLOGIES"])
    line = "matrix=" + json.dumps({"include": cells})
    with Path(os.environ["GITHUB_OUTPUT"]).open("a", encoding="utf-8") as stream:
        stream.write(line + "\n")
    return 0


def first_matching_line(path, prefix):
    try:
        return next((line.split(":", 1)[1].strip() for line in Path(path).read_text().splitlines()
                     if line.startswith(prefix)), None)
    except OSError:
        return None


def machine(args):
    try:
        version = subprocess.check_output(["dotnet", "--version"], text=True, timeout=15).splitlines()[0]
    except (OSError, subprocess.SubprocessError, IndexError):
        version = None
    write_json(args.out, {
        "uname": platform.uname()._asdict(), "cores": os.cpu_count(),
        "cpuModel": first_matching_line("/proc/cpuinfo", "model name"),
        "memory": first_matching_line("/proc/meminfo", "MemTotal:"),
        "runner": {key: os.environ.get(key) for key in ("ImageOS", "ImageVersion", "RUNNER_NAME", "RUNNER_ARCH")},
        "dotnetVersion": version,
    })
    return 0


def git_value(directory, revision):
    return subprocess.check_output(["git", "-C", str(directory), "rev-parse", revision], text=True).strip()


def harness_supports(side_dir, target):
    try:
        return f'"--{target}"' in (Path(side_dir) / PROJECT / "Program.cs").read_text(encoding="utf-8")
    except OSError:
        return False


def build_product(directory, log):
    with log.open("w", encoding="utf-8") as stream:
        result = subprocess.run([
            "dotnet", "build", str(directory / PROJECT / "Chronicle.Benchmarks.Clustering.csproj"),
            "-c", "Release", "/p:TreatWarningsAsErrors=false", "/p:DisableDockerBuild=true",
            "/p:CratisProxiesOutputPath=",
        ], stdout=stream, stderr=subprocess.STDOUT, check=False)
    return result.returncode


def build_side(name, directory, args):
    started = time.monotonic()
    harness = args.harness.resolve() if args.harness else None
    log = args.out / f"build-{name}.log"
    info = {"directory": str(directory), "sha": git_value(directory, "HEAD"),
            "productBenchmarkTree": git_value(directory, "HEAD:Benchmarks"), "overlaid": False}
    info["harnessSha"] = git_value(harness, "HEAD") if harness else info["sha"]
    info["harnessTree"] = git_value(harness, "HEAD:Benchmarks") if harness else info["productBenchmarkTree"]
    if harness:
        info["overlaid"] = info["harnessTree"] != info["productBenchmarkTree"]
        destination = directory / "Benchmarks"
        if destination.exists():
            shutil.rmtree(destination)
        shutil.copytree(harness / "Benchmarks", destination)
    if not harness_supports(directory, args.target):
        message = (f"{name} product {info['sha']} harness {info['harnessSha']} lacks --{args.target}; "
                   "set harness_ref to a compatible benchmark harness.")
        log.write_text(message + "\n", encoding="utf-8")
        info.update(exitCode=1, error=message)
    else:
        info["exitCode"] = build_product(directory, log)
    info["durationSeconds"] = time.monotonic() - started
    return info


def report_build_failure(name, info, log):
    title = "Harness/product incompatibility" if info["overlaid"] else "Product build failed"
    lines = log.read_text(encoding="utf-8", errors="replace").splitlines() if log.exists() else []
    errors = list(dict.fromkeys(line for line in lines if "error" in line.lower()))[:30]
    if info.get("error"):
        errors.insert(0, info["error"])
    append_summary(f"## {title}\n\nSide: {name}; product: {info.get('sha', 'unknown')}; "
                   f"harness: {info.get('harnessSha', 'unknown')}\n\n```text\n" + "\n".join(errors) + "\n```\n")


def build(args):
    args.out.mkdir(parents=True, exist_ok=True)
    sides = {}
    for value in args.side:
        name, directory = value.split(":", 1)
        if name not in SIDES:
            raise ValueError("side must be before or after")
        try:
            info = build_side(name, Path(directory).resolve(), args)
        except (OSError, subprocess.SubprocessError) as error:
            info = {"directory": str(Path(directory).resolve()), "exitCode": 1,
                    "overlaid": bool(args.harness), "error": str(error)}
        sides[name] = info
        if info["exitCode"]:
            report_build_failure(name, info, args.out / f"build-{name}.log")
    write_json(args.out / "sides.json", sides)
    return int(any(info["exitCode"] for info in sides.values()))


def extract_record(stdout):
    for line in reversed(stdout.splitlines()):
        try:
            value = json.loads(line)
            if isinstance(value, dict):
                return value
        except ValueError:
            pass
    return None


def classify(target, exit_code, record, stderr, timed_out):
    if timed_out:
        return "timed-out"
    if "split-jobs needs the cluster Jobs role" in stderr + str((record or {}).get("Error", "")):
        return "product-lacks-jobs-role"
    if exit_code:
        return "failed"
    if record is None:
        return "no-record"
    if record.get("Error"):
        return "failed"
    return classify_record(target, record)


def classify_record(target, record):
    if target == "replay-shape":
        if "Before" not in record or "After" not in record:
            return "failed"
        return "passed" if record["Before"] == record["After"] else "mismatch"
    if record.get("Verdict") == "failed" and record.get("Differences"):
        return "mismatch"
    return "passed" if record.get("Verdict") == "passed" else "failed"


def child_environment(args, sha, warmup, timeout):
    env = dict(os.environ)
    env.pop("GITHUB_SHA", None)
    env["COMMIT_SHA"] = sha
    size = env.get("SIZE", "small")
    if args.target == "rebuild":
        env.update(REBUILD_SIZE="1000" if warmup else size, REBUILD_MODE=env.get("MODE", "live"),
                   REBUILD_TOPOLOGY=args.topology, REBUILD_SEED="5156", REBUILD_TIMEOUT_SECONDS=str(timeout))
    else:
        env["REPLAY_TIMEOUT_SECONDS"] = str(timeout)
        if warmup:
            env.update(REPLAY_ROOTS="100", REPLAY_EVENTS="1000")
        elif size.isdecimal():
            env["REPLAY_EVENTS"] = size
    return env


def wall_timeout(timeout):
    return timeout + 180


def wait_for_exit(process, timeout, deadline):
    remaining = max(0, deadline - time.monotonic())
    limit = wall_timeout(timeout)
    try:
        return process.wait(timeout=min(limit, remaining)), False, False
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        return process.wait(), True, remaining <= limit


def execute(args, side, info, attempt):
    seq, warmup, timeout, deadline = attempt
    directory = Path(info["directory"])
    launcher = args.launcher or ["dotnet", str(directory / PROJECT / "bin/Release/net10.0/Chronicle.Benchmarks.Clustering.dll")]
    command = launcher + (["--rebuild", args.scenario] if args.target == "rebuild" else ["--replay-shape"])
    base = args.results / "runs" / f"{seq:02d}-{side}"
    started_at = datetime.now(timezone.utc).isoformat()
    started = time.monotonic()
    timed_out = False
    budget_exhausted = False
    with base.with_suffix(".stdout.log").open("w") as stdout, base.with_suffix(".stderr.log").open("w") as stderr:
        try:
            process = subprocess.Popen(command, cwd=directory, env=child_environment(args, info["sha"], warmup, timeout),
                                       stdout=stdout, stderr=stderr, start_new_session=True)
            exit_code, timed_out, budget_exhausted = wait_for_exit(process, timeout, deadline)
        except OSError as error:
            stderr.write(str(error))
            exit_code = 1
    raw_stdout = base.with_suffix(".stdout.log").read_text(encoding="utf-8", errors="replace")
    raw_stderr = base.with_suffix(".stderr.log").read_text(encoding="utf-8", errors="replace")
    record = extract_record(raw_stdout)
    result = {"seq": seq, "side": side, "sha": info["sha"], "warmup": warmup, "exitCode": exit_code,
              "status": classify(args.target, exit_code, record, raw_stderr, timed_out),
              "durationSeconds": time.monotonic() - started, "startedAt": started_at,
              "record": record, "stderrTail": raw_stderr[-12000:], "budgetExhausted": budget_exhausted}
    write_json(base.with_suffix(".json"), result)
    print(f"{seq:02d} {side} {'warmup' if warmup else 'measured'}: {result['status']}", flush=True)
    return result


def run(args):
    repetitions = int(os.environ.get("REPETITIONS", "3"))
    sides = read_json(args.results / "sides.json")
    timeout = int(os.environ.get("TIMEOUT_SECONDS", "1200"))
    validate_inputs(repetitions, sides["before"]["sha"], sides["after"]["sha"], timeout)
    validate_size(args.target, os.environ.get("SIZE", "small"))
    budget = float(os.environ.get("RUN_BUDGET_SECONDS", "3300"))
    if timeout <= 0 or budget <= 0:
        raise ValueError("timeout and run budget must be positive")
    (args.results / "runs").mkdir(parents=True, exist_ok=True)
    if any((args.results / "runs").iterdir()):
        raise ValueError("runs directory is not empty; refusing to mix measurements")
    manifest = {"target": args.target, "cell": args.cell, "scenario": args.scenario, "topology": args.topology,
                "repetitions": repetitions, "order": order(repetitions), "status": "INCOMPLETE"}
    write_json(args.results / "run.json", manifest)
    session = RunSession(args, sides, timeout, budget)
    manifest["status"] = session.measure(order(repetitions))
    manifest["measuredRuns"] = len(measured_runs(session.completed))
    write_json(args.results / "run.json", manifest)
    return int(manifest["status"] != "COMPLETE")


def measured_runs(runs):
    return [item for item in runs if not item["warmup"]]


def failed(runs):
    return any(item["status"] != "passed" for item in runs)


class RunSession:
    def __init__(self, args, sides, timeout, budget):
        self.args = args
        self.sides = sides
        self.timeout = timeout
        self.budget = budget
        self.completed = []
        self.started = time.monotonic()
        self.longest = 0

    def measure(self, measured_order):
        schedule = [(side, True) for side in SIDES] + [(side, False) for side in measured_order]
        for side, warmup in schedule:
            if not self.has_budget(warmup):
                return "INCOMPLETE"
            attempt = (len(self.completed) + 1, warmup, self.timeout, self.started + self.budget)
            result = execute(self.args, side, self.sides[side], attempt)
            self.completed.append(result)
            self.longest = max(self.longest, result["durationSeconds"])
            if result["budgetExhausted"]:
                return "INCOMPLETE"
            if self.stop_after_failure(warmup):
                return "FAILED"
        return "COMPLETE"

    def has_budget(self, warmup):
        measured = measured_runs(self.completed)
        estimate = max((item["durationSeconds"] for item in measured), default=self.longest)
        # Re-estimate from full-size runs, reserving a whole pair when starting the next pair.
        runs_needed = 1 if warmup or len(measured) % 2 else 2
        return self.budget - (time.monotonic() - self.started) > 1.5 * estimate * runs_needed

    def stop_after_failure(self, warmup):
        if warmup:
            return self.completed[-1]["status"] != "passed"
        measured = measured_runs(self.completed)
        return len(measured) % 2 == 0 and failed(measured)


def observer_activations(record):
    observers = [silo["Silo"] for silo in record.get("Environment", {}).get("Silos", []) if silo.get("Observers") is True]
    snapshots = {silo["Silo"]: silo for silo in record.get("GrainActivations", [])}
    if not observers:
        return None
    total = 0
    for silo in observers:
        values = [value for name, value in snapshots.get(silo, {}).get("ActivationsByGrainType", {}).items()
                  if "ProjectionObserverSubscriber" in name]
        if not values:
            return None
        total += sum(values)
    return total


def numeric(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def metrics(target, record):
    values = replay_metrics(record) if target == "replay-shape" else rebuild_metrics(record)
    return {key: value if numeric(value) else None for key, value in values.items()}


def replay_metrics(record):
    values = {key: record.get(key) for key in REPLAY_FIELDS}
    values.update({f"MongoFindCommandsPerEventByTag.{key}": value
                   for key, value in record.get("MongoFindCommandsPerEventByTag", {}).items()})
    return values


def nested_value(record, key):
    for part in key.split("."):
        record = record.get(part) if isinstance(record, dict) else None
    return record


def rebuild_metrics(record):
    values = dict.fromkeys(REBUILD_SERIES)
    for series in record.get("Series", []):
        suffix = series.get("Name", "").rsplit("/", 1)[-1]
        if suffix in REBUILD_SERIES:
            values[suffix] = series.get("Value")
    values.update({key: nested_value(record, key) for key in REBUILD_FIELDS})
    values[ACTIVATIONS] = observer_activations(record)
    return values


def stats(values):
    if not values or any(value is None for value in values):
        return None
    median = statistics.median(values)
    spread = 100 * (max(values) - min(values)) / abs(median) if len(values) > 1 and median else None
    return {"median": median, "spreadPercent": spread, "n": len(values), "min": min(values), "max": max(values)}


def delta_percent(before, after):
    if before is None or after is None:
        return None
    if before["median"] == 0:
        return None
    return 100 * (after["median"] - before["median"]) / abs(before["median"])


def metric_direction(name, delta):
    if delta is None:
        return "n/a"
    if delta == 0:
        return "unchanged"
    higher_is_better = name in ("events-per-second", "EventsPerSecond")
    return "better" if (delta > 0) == higher_is_better else "worse"


def within_noise(delta, sides):
    spreads = [side["spreadPercent"] for side in sides if side is not None]
    spreads = [spread for spread in spreads if spread is not None]
    if delta is None or not spreads:
        return None
    return abs(delta) <= max(spreads)


def metric_comparison(name, before, after):
    delta = delta_percent(before, after)
    return {"before": before, "after": after, "deltaPercent": delta, "direction": metric_direction(name, delta),
            "withinNoise": within_noise(delta, (before, after))}


def validate_settings(target, records):
    if not records:
        return
    key = "Settings" if target == "rebuild" else "Workload"
    if any(record.get(key) != records[0].get(key) for record in records):
        raise ValueError("Settings differ between measured runs; comparison refused")


def metric_names(values):
    names = set()
    for records in values.values():
        for record in records:
            names.update(record)
    return sorted(names)


def side_stats(records, name):
    return stats([record.get(name) for record in records])


def compare_records(target, before, after):
    validate_settings(target, before + after)
    values = {"before": [metrics(target, record) for record in before],
              "after": [metrics(target, record) for record in after]}
    return {name: metric_comparison(name, side_stats(values["before"], name), side_stats(values["after"], name))
            for name in metric_names(values)}


def activation_flag(before, after):
    rows = compare_records("rebuild", before, after)
    activations = rows.get(ACTIVATIONS, {})
    throughput = rows.get("events-per-second", {})
    change = activations.get("deltaPercent")
    gain = throughput.get("deltaPercent")
    if change is None or gain is None:
        return "inconclusive"
    return "flagged" if change > 20 and gain <= 0 else "not flagged"


def format_stats(value):
    if value is None:
        return "n/a"
    spread = value["spreadPercent"]
    return f"{value['median']:.6g} ({spread:.2f}%)" if spread is not None else f"{value['median']:.6g} (no spread)"


def partial_summary(manifest, runs):
    if manifest["status"] == "COMPLETE":
        return []
    lines = ["**Partial medians: this cell did not complete successfully. Only passed measured runs contribute.**", ""]
    for side in SIDES:
        measured = runs_for_side(runs, side)
        lines.append(f"- {side}: {len(passed_records(measured))} passed / {len(measured)} measured / "
                     f"{manifest['repetitions']} requested")
    return lines + [""]


def markdown(comparison, context):
    manifest, sides, machine_info, runs = context
    lines = [f"# Paired benchmark: {manifest['target']} / {manifest['cell']}", "",
             f"Cell status: **{manifest['status']}**", ""]
    lines.extend(partial_summary(manifest, runs))
    for side in SIDES:
        info = sides[side]
        lines.append(f"- {side.title()}: `{info['sha'][:12]}`; harness `{info['harnessSha']}`; overlaid: {info['overlaid']}")
    runner = machine_info.get("runner", {})
    lines.extend(["", f"Runner: {runner.get('ImageOS')} {runner.get('ImageVersion')}; "
                  f"CPU: {machine_info.get('cpuModel')}; cores: {machine_info.get('cores')}; memory: {machine_info.get('memory')}",
                  "", "Measured order: " + " → ".join(run["side"] for run in runs if not run["warmup"]), "",
                  "| Seq | Side | Status | Seconds |", "| --- | --- | --- | ---: |"])
    for run_record in runs:
        label = run_record["side"] + (" (warmup, discarded)" if run_record["warmup"] else "")
        lines.append(f"| {run_record['seq']} | {label} | {run_record['status']} | {run_record['durationSeconds']:.3f} |")
    lines.extend(["", "## Metrics", "", "| Metric | Before median (spread) | After median (spread) | Δ% | Direction | Noise |",
                  "| --- | ---: | ---: | ---: | --- | --- |"])
    for name, row in comparison["metrics"].items():
        delta = "n/a" if row["deltaPercent"] is None else f"{row['deltaPercent']:+.2f}%"
        noise = "within noise" if row["withinNoise"] else "outside spread" if row["withinNoise"] is False else "no spread"
        lines.append(f"| {name} | {format_stats(row['before'])} | {format_stats(row['after'])} | {delta} | {row['direction']} | {noise} |")
    lines.extend(["", "Spread = (max − min) / median; n=1 has no spread. Missing metrics are n/a.", "",
                  "## Flags", "", f"ProjectionObserverSubscriber activation flag: {comparison['activationFlag']}",
                  "Top-15 activation snapshots only; an absent type on any observer silo/run is inconclusive.", "",
                  "## Measured status counts", ""])
    for side, counts in comparison["statusCounts"].items():
        lines.append(f"- {side}: " + (", ".join(f"{status}={count}" for status, count in sorted(counts.items())) or "no measured runs"))
    return "\n".join(lines) + "\n"


def runs_for_side(runs, side):
    return [item for item in measured_runs(runs) if item["side"] == side]


def passed_records(runs):
    return [item["record"] for item in runs if item["status"] == "passed"]


def compare(args):
    manifest = read_json(args.results / "run.json")
    sides = read_json(args.results / "sides.json")
    machine_info = read_json(args.results / "machine.json")
    runs = [read_json(path) for path in sorted((args.results / "runs").glob("*.json"))]
    measured = {side: runs_for_side(runs, side) for side in SIDES}
    records = {side: passed_records(measured[side]) for side in SIDES}
    rows = compare_records(manifest["target"], records["before"], records["after"])
    flag = activation_flag(records["before"], records["after"]) if manifest["target"] == "rebuild" else "not applicable"
    comparison = {"metrics": rows, "activationFlag": flag,
                  "statusCounts": {side: dict(Counter(item["status"] for item in measured[side])) for side in SIDES}}
    text = markdown(comparison, (manifest, sides, machine_info, runs))
    write_json(args.results / "comparison.json", comparison)
    (args.results / "comparison.md").write_text(text, encoding="utf-8")
    append_summary(text, args.summary)
    if flag == "flagged":
        print("::warning::ProjectionObserverSubscriber observer activations increased >20% without an events/s gain (#5167).")
    return 0


def parser():
    result = argparse.ArgumentParser(description=__doc__)
    commands = result.add_subparsers(dest="command", required=True)
    commands.add_parser("matrix").set_defaults(handler=plan)
    machine_parser = commands.add_parser("machine")
    machine_parser.add_argument("--out", type=Path, required=True)
    machine_parser.set_defaults(handler=machine)
    build_parser = commands.add_parser("build")
    build_parser.add_argument("--side", action="append", required=True)
    build_parser.add_argument("--harness", type=Path)
    build_parser.add_argument("--target", choices=("rebuild", "replay-shape"), required=True)
    build_parser.add_argument("--out", type=Path, required=True)
    build_parser.set_defaults(handler=build)
    run_parser = commands.add_parser("run")
    for name, default in (("target", "rebuild"), ("cell", ""), ("scenario", "S2"), ("topology", "two")):
        run_parser.add_argument(f"--{name}", default=os.environ.get(name.upper(), default))
    run_parser.add_argument("--results", type=Path, required=True)
    run_parser.add_argument("--launcher", nargs="+")
    run_parser.set_defaults(handler=run)
    compare_parser = commands.add_parser("compare")
    compare_parser.add_argument("--results", type=Path, required=True)
    compare_parser.add_argument("--summary", type=Path)
    compare_parser.set_defaults(handler=compare)
    return result


def main():
    args = parser().parse_args()
    try:
        return args.handler(args)
    except (OSError, ValueError, KeyError, subprocess.SubprocessError) as error:
        print(f"paired-benchmark: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
