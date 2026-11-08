# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Partition SQL specs by their existing xUnit collection, not provider naming conventions."""

import argparse
from pathlib import Path
import re

SPEC_ROOT = Path(__file__).resolve().parents[2] / "Source/Kernel/Storage.Sql.Specs"
COLLECTIONS = {"postgresql": "PostgreSqlCollection", "sqlserver": "SqlServerCollection"}
# SQL Server specs are about five times slower per spec than PostgreSQL, so one SQL Server shard decided how long
# every pull request took. A provider listed here runs as that many parallel shards named `<provider>-<n>`.
SHARDS = {"sqlserver": 3}


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
    parser.add_argument("shard", choices=shard_names())
    args = parser.parse_args()
    print(test_filter(*provider_classes(SPEC_ROOT), args.shard))
