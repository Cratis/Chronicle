#!/usr/bin/env python3
"""Generate the GitHub Actions matrix for the client integration specs.

Every client integration namespace -- a directory under ``Integration/Client``
that contains ``[Fact]`` tests -- runs against a set of infrastructure
configurations (runtime mode x storage backend). Large namespaces are split
into several shards that run as separate, parallel jobs so the slowest single
job no longer gates the whole workflow.

Sharding only changes how the work is distributed: every shard of a namespace
together covers exactly the same tests as the un-sharded namespace would, so
coverage across the selected infrastructure configurations is preserved.

Which configurations are selected depends on the backend selection flags. Pull
requests run ``PULL_REQUEST_DATABASES``; the nightly schedule and manual
dispatch pass ``--all-providers`` to run every backend. ``--databases`` names an
explicit set for anything in between.

Balancing is by ``[Fact]`` count across test classes. Each spec file declares
the facts on its first top-level class, so a shard targets a class with
``FullyQualifiedName~<namespace>.<class>.`` (note the trailing dot). Class
fully-qualified names are mutually exclusive as substrings, so each test is
owned by exactly one shard -- never double-run, never dropped. (A class that is
a dotted ancestor of another, e.g. a genuine nested type, is folded into the
same shard so the substring filter still owns it exactly once.)
"""

import argparse
import json
import os
import re
from collections import defaultdict

CLIENT_ROOT = "Integration/Client"
NAMESPACE_PREFIX = "Cratis.Chronicle.Integration."

# The three suites the integration workflow fans out. Client namespaces are the directories that contain
# [Fact] tests and run against infrastructure configurations. Kernel and Clustering run every directory
# once, and match a namespace with a trailing dot so "Events" does not also run "EventSequences".
SUITES = {
    "client": {"root": CLIENT_ROOT, "prefix": NAMESPACE_PREFIX},
    "kernel": {"root": "Integration/Kernel", "prefix": "Cratis.Chronicle.Kernel.Integration."},
    "clustering": {"root": "Integration/Clustering", "prefix": "Cratis.Chronicle.Integration.Clustering."},
}

# Namespaces whose runs are too short to justify a job of their own share one job. The batches are static
# and reviewable: a namespace that is not listed runs alone, so a new namespace never joins a batch silently.
# Each batch stays under about 240 s of p50 test time, below the slowest solo shard
# (for_EventSequence: 339 s p50 test, 441 s p50 job), so the critical path does not grow.
# Client p50 test seconds: for_ReadModelReactors 66, for_ContextStorageIsolation 68, for_ExplicitArtifacts 68
# (202 s); for_ShortLivedProcess 69, for_PIIManager 85, for_PublicClientApis 86 (240 s).
# Every member keeps its own zero-test guard (the assert-tests-ran action's prefixes input).
# To take a namespace out of its batch, delete it from its line here.
BATCHES = {
    "client": [
        ["for_ReadModelReactors", "for_ContextStorageIsolation", "for_ExplicitArtifacts"],
        ["for_ShortLivedProcess", "for_PIIManager", "for_PublicClientApis"],
    ],
    "kernel": [
        ["Jobs", "EventStores", "Patches", "Alerts", "Projections"],
        ["Patterns", "EventRouting", "EventSequences", "for_JobsManager"],
        ["Events", "for_DataProtection"],
    ],
    "clustering": [
        ["for_ScaledOutClients", "for_DataProtection", "for_Clustering"],
    ],
}

# Namespaces that run in a restricted set of configurations, or not at all in this matrix.
CONFIG_RESTRICTED = (
    "SqlServerAlertIncidents", "for_KernelTlsLifecycle", "for_PerProcessActivation",
    "for_OccurrenceProcessRecovery", "for_ContainerStartup", "GraphProjections",
)

# The Neo4j lane of GraphProjections that runs with a database per namespace: Neo4j Enterprise, under its Evaluation Agreement (Cratis/Chronicle#5739).
GRAPH_ENTERPRISE_SHARD = "enterprise"
GRAPH_ENTERPRISE_IMAGE = "neo4j:5-enterprise"

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

# Every backend any configuration covers, in the order INFRA_CONFIGS declares them.
ALL_DATABASES = tuple(dict.fromkeys(database for _, database, _ in INFRA_CONFIGS))

# Storage backends covered on every pull request. Running all five configurations
# per PR meant each shard was built and started five times over, but running
# MongoDB alone let MongoDB-shaped assumptions merge green and break the nightly:
# specs that assumed a sink's row order, and a SQL path that never provisioned a
# namespace's database, both landed that way. SQLite is the one non-Mongo backend
# that needs no database container of its own -- the kernel container writes to a
# SQLite file -- so it puts the whole Storage.Sql/EF Core path under every PR for
# one extra job per shard. PostgreSQL and SQL Server each add a second container
# per job and stay on the nightly schedule -- see --all-providers.
PULL_REQUEST_DATABASES = ("mongodb", "sqlite")

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


def _inherited_fact_count(text):
    """Count the facts a thin provider wrapper inherits from the shared storage contract."""
    alias = re.search(r"using Contract = Cratis\.Chronicle\.Storage\.([\w.]+);", text)
    base = re.search(r": Contract\.(\w+)<", text)
    if not alias or not base:
        return 0
    path = os.path.join("Source", "Kernel", "Storage.Specs", *alias.group(1).split("."), base.group(1) + ".cs")
    return _read(path).count("[Fact]")


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
            facts = text.count("[Fact]") + _inherited_fact_count(text)
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
    counts = _test_class_counts(os.path.join(CLIENT_ROOT, namespace))
    if shard_count <= 1:
        # Retain the normal namespace filter; moved parity wrappers can keep their kernel namespace.
        if all(name.startswith(fully_qualified + ".") for name in counts):
            return [("all", f"FullyQualifiedName~{fully_qualified}")]
        return [("all", "|".join(f"FullyQualifiedName~{name}." for name in sorted(counts)))]

    groups = _group_by_ancestor(counts)
    buckets = _pack(groups, shard_count)
    shards = []
    for index, bucket in enumerate(buckets, start=1):
        test_filter = "|".join(f"FullyQualifiedName~{ns}." for ns in sorted(bucket))
        shards.append((f"{index}of{len(buckets)}", test_filter))
    return shards


def _databases(value):
    """Parse a comma-separated backend list, rejecting anything not in ALL_DATABASES."""
    selected = tuple(item.strip() for item in value.split(",") if item.strip())
    unknown = [item for item in selected if item not in ALL_DATABASES]
    if unknown:
        raise argparse.ArgumentTypeError(
            f"unknown storage backend(s): {', '.join(unknown)}. "
            f"Known backends: {', '.join(ALL_DATABASES)}")
    if not selected:
        raise argparse.ArgumentTypeError("at least one storage backend is required")
    return selected


def _names(value):
    """Parse a comma-separated namespace list; an explicitly empty list is a valid selection of nothing."""
    return tuple(item.strip() for item in value.split(",") if item.strip())


def selected_databases(args):
    """Resolve which storage backends the matrix covers, widest selection first."""
    if args.all_providers:
        return ALL_DATABASES
    if args.databases:
        return args.databases
    return PULL_REQUEST_DATABASES


def _directories(root):
    """Every top-level directory under a suite root, excluding build output."""
    return [entry for entry in sorted(os.listdir(root))
            if os.path.isdir(os.path.join(root, entry)) and entry not in ("bin", "obj")]


def _solo_units(suite):
    """Map each namespace the suite runs to its list of (shard_label, test_filter)."""
    spec = SUITES[suite]
    if suite == "client":
        # The injected startup specs need neither a runtime configuration nor Docker.
        return {name: _shards_for(name) for name in _namespaces_with_facts() if name != "for_ContainerStartup"}
    return {name: [("all", f"FullyQualifiedName~{spec['prefix']}{name}.")] for name in _directories(spec["root"])}


def _validated_batches(suite, units, batches):
    """Check every batch member can share a job without changing what runs or where."""
    spec = SUITES[suite]
    seen = set()
    for batch in batches.get(suite, []):
        for name in batch:
            if name not in units:
                raise ValueError(f"{suite}: batch member {name} is not a namespace this suite runs")
            if name in seen:
                raise ValueError(f"{suite}: {name} is in more than one batch")
            seen.add(name)
            # SHARD_COUNTS and CONFIG_RESTRICTED name client namespaces; Kernel and Clustering run each directory once.
            if suite == "client" and (name in SHARD_COUNTS or name in CONFIG_RESTRICTED):
                raise ValueError(f"{suite}: {name} is sharded or configuration-restricted and cannot be batched")
            plain = f"FullyQualifiedName~{spec['prefix']}{name}" + ("" if suite == "client" else ".")
            if units[name] != [("all", plain)]:
                raise ValueError(f"{suite}: {name} has no plain namespace filter and cannot be batched")
    return seen


def _restrict(suite, units, batches, namespaces):
    """Narrow the units, and the batches that group them, to the selected namespaces.

    An unknown name is an error, never an empty matrix: a selection that silently ran nothing would read
    as a pass. A batch keeps only its selected members, so a selected namespace never drags its batch
    partners along; a batch left with one member simply runs that namespace on its own.
    """
    unknown = sorted(set(namespaces) - set(units))
    if unknown:
        raise ValueError(f"{suite}: unknown namespace(s) {', '.join(unknown)}. Known: {', '.join(units)}")
    kept = {name: shards for name, shards in units.items() if name in namespaces}
    narrowed = {suite: [members for members in ([m for m in batch if m in namespaces] for batch in batches.get(suite, []))
                        if len(members) > 1]}
    return kept, narrowed


def _suite_units(suite, batches, namespaces=None):
    """Return (namespace_label, shard_label, test_filter, members, short_name) per job unit."""
    prefix = SUITES[suite]["prefix"]
    units = _solo_units(suite)
    if namespaces is not None:
        units, batches = _restrict(suite, units, batches, namespaces)
    batched = _validated_batches(suite, units, batches)
    result = []
    for name, shards in units.items():
        if name in batched:
            continue
        for shard_label, test_filter in shards:
            result.append((prefix + name, shard_label, test_filter, "", name))
    for batch in batches.get(suite, []):
        result.append((
            prefix + "+".join(batch), "all", "|".join(units[name][0][1] for name in batch),
            ",".join(prefix + name for name in batch), None))
    return result


def build_include(suite, databases, batches, namespaces=None):
    """Build the matrix entries for a suite, optionally restricted to the named namespaces.

    A batch shares one job and keeps a per-member guard.
    """
    configs = [config for config in INFRA_CONFIGS if config[1] in databases]
    include = []
    for namespace, shard_label, test_filter, members, name in _suite_units(suite, batches, namespaces):
        entry = {"namespace": namespace, "shard": shard_label, "filter": test_filter, "members": members}
        if suite != "client":
            include.append(entry)
            continue
        namespace_configs = configs
        # This family exercises a provider directly, not the configurable Chronicle runtime.
        # Run it once in the SQL Server lane instead of starting SQL Server in every backend lane.
        if name == "SqlServerAlertIncidents":
            namespace_configs = [config for config in configs if config[1] == "mssql"]
        # Kernel TLS lifecycle specs own an out-of-process MongoDB kernel container;
        # PerProcess activation specs need its gRPC listener and cover client identity once.
        if name in {"for_KernelTlsLifecycle", "for_PerProcessActivation"}:
            namespace_configs = [config for config in configs if config[0] == "outofprocess" and config[1] == "mongodb"]
        # Graph projections run on the silo of the client and write to a Neo4j server the fixture starts, so the storage backend of the kernel
        # is not what they exercise. In-process covers MongoDB without building the kernel image; every other backend is covered out of process,
        # which also proves a graph is projected the same way whatever stores the event log.
        if name == "GraphProjections":
            namespace_configs = [config for config in configs if config[0] == "inprocess" or config[1] != "mongodb"]
        # Process recovery owns a PostgreSQL database and an external kernel regardless of lane settings.
        # Schedule it only once, in the lane matching the infrastructure it actually exercises.
        if name == "for_OccurrenceProcessRecovery":
            namespace_configs = [config for config in configs if config[0] == "outofprocess" and config[1] == "postgresql"]
        for mode, database, needs_docker in namespace_configs:
            include.append({**entry, "mode": mode, "database": database, "needs-docker": needs_docker})
        # Database per namespace needs a Neo4j with multiple databases, which only the Enterprise edition has. One extra in-process lane runs the
        # suite on it. It accepts the Neo4j Evaluation Agreement for now (Cratis/Chronicle#5739); the workflow reads the two fields below.
        if name == "GraphProjections":
            include.append({**entry, "shard": GRAPH_ENTERPRISE_SHARD, "mode": "inprocess", "database": "mongodb", "needs-docker": True,
                            "neo4j-image": GRAPH_ENTERPRISE_IMAGE, "neo4j-database-mode": "PerNamespace"})
    return include


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--suite",
        choices=sorted(SUITES),
        default="client",
        help="Which integration project to generate the matrix for. Defaults to client.",
    )
    parser.add_argument(
        "--all-providers",
        action="store_true",
        help="Include every storage backend. This is what the nightly schedule and "
        "manual dispatch run; it takes precedence over --databases.",
    )
    parser.add_argument(
        "--databases",
        type=_databases,
        help="Comma-separated storage backends to cover, e.g. 'mongodb,sqlite'. "
        "Without it the backends in PULL_REQUEST_DATABASES are covered, which is "
        "what pull requests run.",
    )
    parser.add_argument(
        "--namespaces",
        type=_names,
        help="Comma-separated namespaces (directory names under the suite's root) to restrict the matrix to. "
        "Without it every namespace runs. An unknown name is an error, and an empty list is an empty matrix.",
    )
    args = parser.parse_args(argv)

    try:
        include = build_include(args.suite, selected_databases(args), BATCHES, args.namespaces)
    except ValueError as error:
        parser.error(str(error))
    print(json.dumps({"include": include}))


if __name__ == "__main__":
    main()
