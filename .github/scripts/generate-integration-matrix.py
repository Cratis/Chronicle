#!/usr/bin/env python3
"""Generate the GitHub Actions matrix for the client integration specs.

Every client integration namespace -- a directory under ``Integration/Client``
that contains ``[Fact]`` tests -- runs against every infrastructure
configuration (runtime mode x storage backend). Large namespaces are split
into several shards that run as separate, parallel jobs so the slowest single
job no longer gates the whole workflow.

Sharding only changes how the work is distributed: every shard of a namespace
together covers exactly the same tests as the un-sharded namespace would, so
coverage across all five infrastructure configurations is preserved.

Balancing is by ``[Fact]`` count across test classes. Each spec file declares
the facts on its first top-level class, so a shard targets a class with
``FullyQualifiedName~<namespace>.<class>.`` (note the trailing dot). Class
fully-qualified names are mutually exclusive as substrings, so each test is
owned by exactly one shard -- never double-run, never dropped. (A class that is
a dotted ancestor of another, e.g. a genuine nested type, is folded into the
same shard so the substring filter still owns it exactly once.)
"""

import json
import os
import re
from collections import defaultdict

CLIENT_ROOT = "Integration/Client"
NAMESPACE_PREFIX = "Cratis.Chronicle.Integration."

# Number of parallel shards per namespace. Any namespace not listed here runs as
# a single job. Tuned from observed CI wall-clock: Projections (617 facts) was
# the critical path of the whole run, and the others have a slow out-of-process
# SQLite configuration whose contention eases when fewer tests share one job.
SHARD_COUNTS = {
    "Projections": 4,
    "for_EventSequence": 2,
    "for_Reactors": 2,
    "for_Reducers": 2,
}

# mode | database | needs-docker. Out-of-process exercises every storage backend
# through the real Chronicle container; in-process covers MongoDB.
INFRA_CONFIGS = [
    ("inprocess", "mongodb", False),
    ("outofprocess", "mongodb", True),
    ("outofprocess", "sqlite", True),
    ("outofprocess", "postgresql", True),
    ("outofprocess", "mssql", True),
]

_NAMESPACE_RE = re.compile(r"^\s*namespace\s+([A-Za-z0-9_.]+)", re.MULTILINE)
# A top-level type declaration starts at column 0 (no leading whitespace). The
# nested setup/`context` types and helper records are indented, so the first
# match is the spec class that declares the file's [Fact] methods.
_TOP_LEVEL_TYPE_RE = re.compile(
    r"^(?:public |internal |sealed |abstract |static |partial )*(?:class|record)\s+([A-Za-z_]\w*)",
    re.MULTILINE,
)


def _read(path):
    with open(path, encoding="utf-8", errors="ignore") as handle:
        return handle.read()


def _namespaces_with_facts():
    """Top-level directories under CLIENT_ROOT that contain at least one [Fact]."""
    namespaces = []
    for entry in sorted(os.listdir(CLIENT_ROOT)):
        directory = os.path.join(CLIENT_ROOT, entry)
        if not os.path.isdir(directory) or entry in ("bin", "obj"):
            continue
        if _test_class_counts(directory):
            namespaces.append(entry)
    return namespaces


def _test_class_counts(namespace_dir):
    """Map each test class fully-qualified name to its number of [Fact] tests.

    A spec file declares all of its facts on its first top-level class, so the
    facts are attributed to ``<namespace>.<first-top-level-class>``.
    """
    counts = defaultdict(int)
    for dirpath, dirnames, filenames in os.walk(namespace_dir):
        dirnames[:] = [d for d in dirnames if d not in ("bin", "obj")]
        for filename in filenames:
            if not filename.endswith(".cs"):
                continue
            text = _read(os.path.join(dirpath, filename))
            facts = text.count("[Fact]")
            if not facts:
                continue
            namespace = _NAMESPACE_RE.search(text)
            declaring_type = _TOP_LEVEL_TYPE_RE.search(text)
            if namespace and declaring_type:
                counts[f"{namespace.group(1)}.{declaring_type.group(1)}"] += facts
    return counts


def _group_by_ancestor(counts):
    """Fold every namespace into its shortest dotted ancestor present in the set.

    This makes each group own a disjoint subtree, so a ``~group.`` substring
    filter never matches a test that belongs to another group.
    """
    namespaces = sorted(counts)  # shorter (ancestor) namespaces sort first
    groups = defaultdict(int)
    for namespace in namespaces:
        owner = namespace
        for candidate in namespaces:
            if namespace.startswith(candidate + "."):
                owner = candidate  # first match is the shortest ancestor
                break
        groups[owner] += counts[namespace]
    return groups


def _pack(groups, num_shards):
    """Greedy largest-first bin-packing of groups into balanced shards."""
    buckets = [[] for _ in range(num_shards)]
    loads = [0] * num_shards
    for namespace, count in sorted(groups.items(), key=lambda kv: (-kv[1], kv[0])):
        target = loads.index(min(loads))
        buckets[target].append(namespace)
        loads[target] += count
    return [bucket for bucket in buckets if bucket]


def _shards_for(namespace):
    """Return a list of (shard_label, test_filter) for a namespace."""
    fully_qualified = NAMESPACE_PREFIX + namespace
    shard_count = SHARD_COUNTS.get(namespace, 1)
    if shard_count <= 1:
        # Identical to the historical behavior: one job, one namespace filter.
        return [("all", f"FullyQualifiedName~{fully_qualified}")]

    groups = _group_by_ancestor(_test_class_counts(os.path.join(CLIENT_ROOT, namespace)))
    buckets = _pack(groups, shard_count)
    shards = []
    for index, bucket in enumerate(buckets, start=1):
        test_filter = "|".join(f"FullyQualifiedName~{ns}." for ns in sorted(bucket))
        shards.append((f"{index}of{len(buckets)}", test_filter))
    return shards


def main():
    include = []
    for namespace in _namespaces_with_facts():
        fully_qualified = NAMESPACE_PREFIX + namespace
        for shard_label, test_filter in _shards_for(namespace):
            for mode, database, needs_docker in INFRA_CONFIGS:
                include.append(
                    {
                        "namespace": fully_qualified,
                        "shard": shard_label,
                        "filter": test_filter,
                        "mode": mode,
                        "database": database,
                        "needs-docker": needs_docker,
                    }
                )
    print(json.dumps({"include": include}))


if __name__ == "__main__":
    main()
