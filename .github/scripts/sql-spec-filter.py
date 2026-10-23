# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Partition SQL specs by their existing xUnit collection, not provider naming conventions."""

import argparse
from pathlib import Path
import re

SPEC_ROOT = Path(__file__).resolve().parents[2] / "Source/Kernel/Storage.Sql.Specs"
COLLECTIONS = {"postgresql": "PostgreSqlCollection", "sqlserver": "SqlServerCollection"}
# SQL Server specs pay roughly 20 s of database setup per class, so that collection is split across this
# many jobs (sqlserver-1 .. sqlserver-N), each with its own container. Classes are dealt round-robin in
# sorted order, so every class lands in exactly one part.
SQLSERVER_PARTS = 4
SHARDS = ["postgresql", *(f"sqlserver-{part}" for part in range(1, SQLSERVER_PARTS + 1)), "remaining"]


def provider_classes(root):
    classes = {provider: [] for provider in COLLECTIONS}
    for path in sorted(root.rglob("*.cs")):
        if "obj" in path.relative_to(root).parts:
            continue
        source = path.read_text(encoding="utf-8")
        if "[Collection(" not in source:
            continue
        namespace = re.search(r"^namespace ([\w.]+);", source, re.MULTILINE)
        matches = re.findall(
            r"\[Collection\((\w+)\.Name\)\]\s*public class (\w+)", source)
        if not namespace or len(matches) != source.count("[Collection("):
            raise ValueError(f"Unsupported collection declaration in {path}")
        for collection, class_name in matches:
            provider = next((key for key, value in COLLECTIONS.items() if value == collection), None)
            if provider is None:
                raise ValueError(f"Unknown SQL spec collection {collection} in {path}")
            classes[provider].append(f"{namespace[1]}.{class_name}.")
    for provider, names in classes.items():
        if not names or len(names) != len(set(names)):
            raise ValueError(f"Expected nonempty, unique {provider} spec classes under {root}")
    return classes


def test_filter(classes, shard):
    if shard == "remaining":
        # The complement includes new uncollected specs automatically, exactly once.
        return "&".join(f"FullyQualifiedName!~{name}"
                        for names in classes.values() for name in names)
    if shard.startswith("sqlserver-"):
        part = int(shard.removeprefix("sqlserver-"))
        if not 1 <= part <= SQLSERVER_PARTS:
            raise ValueError(f"Unknown SQL Server part {shard}")
        names = classes["sqlserver"][part - 1::SQLSERVER_PARTS]
        if not names:
            raise ValueError(f"SQL Server part {shard} has no spec classes")
        return "|".join(f"FullyQualifiedName~{name}" for name in names)
    return "|".join(f"FullyQualifiedName~{name}" for name in classes[shard])


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("shard", choices=[*COLLECTIONS, *SHARDS[1:-1], "remaining"])
    args = parser.parse_args()
    print(test_filter(provider_classes(SPEC_ROOT), args.shard))
