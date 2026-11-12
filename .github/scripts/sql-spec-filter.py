# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Partition SQL specs by their existing xUnit collection, not provider naming conventions."""

import argparse
from datetime import datetime
import json
import math
from pathlib import Path
import re
import xml.etree.ElementTree as ElementTree

SPEC_ROOT = Path(__file__).resolve().parents[2] / "Source/Kernel/Storage.Sql.Specs"
TIMINGS = Path(__file__).with_name("sql-spec-timings.json")
COLLECTIONS = {"postgresql": "PostgreSqlCollection", "sqlserver": "SqlServerCollection"}
# SQL Server specs are about five times slower per spec than PostgreSQL, so one SQL Server shard decided how long
# every pull request took. A provider listed here runs as that many parallel shards named `<provider>-<n>`.
# The shards are balanced by measured duration (sql-spec-timings.json): splitting by spec count left one shard
# at 978 s of tests and another at 357 s, because a few classes take minutes each.
SHARDS = {"sqlserver": 4}
TRX = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"


def provider_classes(root):
    classes = {provider: [] for provider in COLLECTIONS}
    weights = {}
    for path in sorted(root.rglob("*.cs")):
        if "obj" in path.relative_to(root).parts:
            continue
        source = path.read_text(encoding="utf-8")
        if "[Collection(" not in source:
            continue
        namespace = re.search(r"^namespace ([\w.]+);", source, re.MULTILINE)
        facts = source.count("[Fact]") + source.count("[Theory]")
        matches = re.findall(
            r"\[Collection\((\w+)\.Name\)\]\s*public class (\w+)", source)
        if not namespace or len(matches) != source.count("[Collection("):
            raise ValueError(f"Unsupported collection declaration in {path}")
        for collection, class_name in matches:
            provider = next((key for key, value in COLLECTIONS.items() if value == collection), None)
            if provider is None:
                raise ValueError(f"Unknown SQL spec collection {collection} in {path}")
            classes[provider].append(f"{namespace[1]}.{class_name}.")
            weights[f"{namespace[1]}.{class_name}."] = max(1, facts // len(matches))
    for provider, names in classes.items():
        if not names or len(names) != len(set(names)):
            raise ValueError(f"Expected nonempty, unique {provider} spec classes under {root}")
    return classes, weights


def partition(names, weights, count):
    """Split classes into `count` shards of similar size, so every class lands in exactly one shard.

    Heaviest classes go first, each to the lightest shard so far, with the class name as the tie-break. The
    result depends only on the spec sources, so every shard computes the same partition independently.
    """
    shards = [[] for _ in range(count)]
    totals = [0] * count
    for name in sorted(names, key=lambda name: (-weights[name], name)):
        lightest = min(range(count), key=lambda index: (totals[index], index))
        shards[lightest].append(name)
        totals[lightest] += weights[name]
    return shards


def _is_positive_seconds(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value) and value > 0


def load_timings(path=TIMINGS):
    """Measured seconds per spec class; a missing file means no measurements yet."""
    if not path.exists():
        return {}
    seconds = json.loads(path.read_text(encoding="utf-8"))["seconds"]
    if not all(_is_positive_seconds(value) for value in seconds.values()):
        raise ValueError(f"Every timing in {path} must be a positive, finite number of seconds")
    return seconds


def timed_weights(classes, facts, timings):
    """Weigh each class by its measured duration, estimating unmeasured classes from their spec count.

    An unmeasured class (new since the last refresh) costs its spec count times the provider's measured seconds
    per spec, so it still lands somewhere sensible until the next refresh measures it.
    """
    weights = {}
    for names in classes.values():
        measured = [name for name in names if name in timings]
        spec_count = sum(facts[name] for name in measured)
        rate = sum(timings[name] for name in measured) / spec_count if spec_count else 1
        for name in names:
            weights[name] = timings[name] if name in timings else facts[name] * rate
    return weights


def result_timestamp(result, attribute):
    """TRX timestamps must carry their offset so separate machines' local clocks are not compared."""
    timestamp = datetime.fromisoformat(result.get(attribute, ""))
    if timestamp.tzinfo is None:
        raise ValueError(f"Missing timezone in {attribute}")
    return timestamp


def result_interval(result, trx):
    """Require class identity and timestamped facts rather than falling back to fact durations."""
    test = result.get("testName", "").split("(", 1)[0]
    name, _, fact = test.rpartition(".")
    if not all((name, fact)):
        raise ValueError(f"Missing test class in {trx}")
    start = result_timestamp(result, "startTime")
    end = result_timestamp(result, "endTime")
    if end < start:
        raise ValueError(f"Invalid test interval in {trx}: {test}")
    return name + ".", start, end


def class_completion_intervals(trx):
    """Include inter-class setup time, ordering classes by their last fact's completion."""
    results = [result_interval(result, trx)
               for result in ElementTree.parse(trx).getroot().iter(f"{TRX}UnitTestResult")]
    if not results:
        raise ValueError(f"No test results in {trx}")
    previous = min(start for _, start, _ in results)
    ends = {}
    for name, _, end in results:
        ends[name] = max(ends.get(name, end), end)
    for name in sorted(ends, key=lambda name: (ends[name], name)):
        yield name, (ends[name] - previous).total_seconds()
        previous = ends[name]


def measured_timings(classes, trx_files):
    """Sum per-file class completion gaps, keeping only classes the collections own."""
    owned = set().union(*classes.values())
    seconds = {}
    for trx in trx_files:
        for name, elapsed in class_completion_intervals(trx):
            if name in owned:
                seconds[name] = seconds.get(name, 0) + elapsed
    if not seconds:
        raise ValueError("No owned SQL spec timings in TRX results")
    return {name: round(max(value, 0.1), 1) for name, value in sorted(seconds.items())}


def shard_names():
    """Every shard identifier the workflow matrix must list, in order."""
    return [shard for provider in COLLECTIONS
            for shard in ([f"{provider}-{n}" for n in range(1, SHARDS[provider] + 1)]
                          if provider in SHARDS else [provider])] + ["remaining"]


def test_filter(classes, weights, shard):
    if shard == "remaining":
        # The complement includes new uncollected specs automatically, exactly once.
        return "&".join(f"FullyQualifiedName!~{name}"
                        for names in classes.values() for name in names)
    provider, _, index = shard.partition("-")
    names = classes[provider]
    if provider in SHARDS:
        names = partition(names, weights, SHARDS[provider])[int(index) - 1]
    if not names:
        # An empty expression selects every spec, so an empty shard would silently run the whole project.
        raise ValueError(f"Shard {shard} selects no spec classes")
    return "|".join(f"FullyQualifiedName~{name}" for name in names)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("shard", nargs="?", choices=shard_names())
    parser.add_argument("--refresh-timings", nargs="+", metavar="TRX",
                        help="rewrite sql-spec-timings.json from the TRX results of the SQL spec shards")
    args = parser.parse_args()
    classes, facts = provider_classes(SPEC_ROOT)
    if args.refresh_timings:
        timings = measured_timings(classes, args.refresh_timings)
        TIMINGS.write_text(json.dumps({"seconds": timings}, indent=2) + "\n", encoding="utf-8")
    elif args.shard:
        print(test_filter(classes, timed_weights(classes, facts, load_timings()), args.shard))
    else:
        parser.error("a shard or --refresh-timings is required")
