#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for generate-integration-matrix.py.

The matrix generator decides what every requested pull-request and nightly run
actually verifies, so a silent change in what it selects is a silent change in
coverage. These specs pin the selection rules -- what a labeled pull request covers,
what --all-providers covers, that an explicit --databases set is honored, and
that an unknown backend fails loudly rather than quietly yielding nothing.

Run with: python3 .github/scripts/test-generate-integration-matrix.py
(from the repository root, which is where the generator reads Integration/Client).
"""

import argparse
import importlib.util
import json
import os
import subprocess
import sys
import unittest

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(SCRIPT_DIRECTORY, "generate-integration-matrix.py")
REPOSITORY_ROOT = os.path.dirname(os.path.dirname(SCRIPT_DIRECTORY))


def _load():
    """Import the generator, whose filename is not a valid module name."""
    spec = importlib.util.spec_from_file_location("generate_integration_matrix", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


matrix = _load()


def _run(*arguments):
    """Run the generator from the repository root and return the parsed matrix."""
    completed = subprocess.run(
        [sys.executable, SCRIPT, *arguments],
        cwd=REPOSITORY_ROOT,
        capture_output=True,
        text=True,
        check=True)
    return json.loads(completed.stdout)["include"]


def _databases_of(include):
    return {entry["database"] for entry in include}


class for_selected_databases(unittest.TestCase):
    """The backend selection rules, isolated from the file system walk."""

    @staticmethod
    def _args(all_providers=False, databases=None):
        return argparse.Namespace(all_providers=all_providers, databases=databases)

    def test_pull_requests_cover_more_than_mongodb(self):
        # A pull request covering only MongoDB is what let MongoDB-shaped
        # assumptions merge green and break the nightly.
        self.assertIn("mongodb", matrix.PULL_REQUEST_DATABASES)
        self.assertGreater(len(matrix.PULL_REQUEST_DATABASES), 1)

    def test_defaults_to_the_pull_request_databases(self):
        self.assertEqual(
            matrix.PULL_REQUEST_DATABASES,
            matrix.selected_databases(self._args()))

    def test_all_providers_covers_every_backend(self):
        self.assertEqual(
            matrix.ALL_DATABASES,
            matrix.selected_databases(self._args(all_providers=True)))

    def test_all_providers_wins_over_an_explicit_set(self):
        self.assertEqual(
            matrix.ALL_DATABASES,
            matrix.selected_databases(self._args(all_providers=True, databases=("sqlite",))))

    def test_an_explicit_set_is_honored(self):
        self.assertEqual(
            ("sqlite",),
            matrix.selected_databases(self._args(databases=("sqlite",))))

    def test_every_pull_request_database_is_a_known_backend(self):
        for database in matrix.PULL_REQUEST_DATABASES:
            self.assertIn(database, matrix.ALL_DATABASES)


class for_parsing_databases(unittest.TestCase):
    """The --databases option, which must never silently select nothing."""

    def test_parses_a_comma_separated_list(self):
        self.assertEqual(("mongodb", "sqlite"), matrix._databases("mongodb, sqlite"))

    def test_rejects_an_unknown_backend(self):
        with self.assertRaises(argparse.ArgumentTypeError):
            matrix._databases("mongodb,mysql")

    def test_rejects_an_empty_selection(self):
        with self.assertRaises(argparse.ArgumentTypeError):
            matrix._databases(" , ")


class for_inherited_storage_specs(unittest.TestCase):
    def test_counts_the_shared_contract_facts(self):
        text = (
            "using Contract = Cratis.Chronicle.Storage.Alerts.for_IAlertIncidentsStorage.when_applying;\n"
            "public class and_competing_inserts_arrive : Contract.and_competing_inserts_arrive<Harness> {}"
        )
        self.assertEqual(1, matrix._inherited_fact_count(text))

    def test_includes_the_sql_server_parity_family_in_executed_filters(self):
        include = _run("--all-providers")
        parity = [entry for entry in include if "for_SqlServerAlertIncidentsStorage" in entry["filter"]]
        self.assertTrue(parity)
        self.assertEqual({"mssql"}, _databases_of(parity))
        self.assertEqual(1, len(parity))
        self.assertTrue(all("and_competing_inserts_arrive" in entry["filter"] for entry in parity))


class for_generating_the_matrix(unittest.TestCase):
    """The generated matrix, against the repository's real integration namespaces."""

    def test_container_startup_runs_only_in_the_fast_unit_lane(self):
        for arguments in ((), ("--all-providers",)):
            include = _run(*arguments)
            self.assertFalse(any(entry["namespace"].endswith(".for_ContainerStartup") for entry in include))

    def test_a_pull_request_run_covers_the_pull_request_databases(self):
        self.assertEqual(set(matrix.PULL_REQUEST_DATABASES), _databases_of(_run()))

    def test_a_scheduled_run_covers_every_backend(self):
        self.assertEqual(set(matrix.ALL_DATABASES), _databases_of(_run("--all-providers")))

    def test_the_shards_are_the_same_whatever_the_backends(self):
        # Backend selection must not change ownership of configurable-runtime shards.
        # The direct SQL Server contract family exists only in its provider lane (specified above).
        def shards(include):
            return {
                (entry["namespace"], entry["shard"], entry["filter"])
                for entry in include
                if entry["namespace"] != "Cratis.Chronicle.Integration.SqlServerAlertIncidents"
            }

        self.assertEqual(shards(_run()), shards(_run("--all-providers")))

    def test_every_entry_carries_the_fields_the_workflow_reads(self):
        for entry in _run():
            self.assertEqual(
                {"namespace", "shard", "filter", "members", "mode", "database", "needs-docker"},
                set(entry))

    def test_an_unknown_backend_fails_the_run(self):
        completed = subprocess.run(
            [sys.executable, SCRIPT, "--databases", "mysql"],
            cwd=REPOSITORY_ROOT,
            capture_output=True,
            text=True,
            check=False)
        self.assertNotEqual(0, completed.returncode)



def _directories(root):
    return sorted(entry for entry in os.listdir(os.path.join(REPOSITORY_ROOT, root))
                  if os.path.isdir(os.path.join(REPOSITORY_ROOT, root, entry)) and entry not in ("bin", "obj"))


def _build(suite, batches=None):
    """Generate in-process, so a spec can swap the batch table without touching the generator's own."""
    return matrix.build_include(suite, matrix.PULL_REQUEST_DATABASES, matrix.BATCHES if batches is None else batches)


def _members(entry):
    return [member for member in entry["members"].split(",") if member]


class for_batched_namespaces(unittest.TestCase):
    """Short namespaces share a job; nothing may be dropped, duplicated or silently joined."""

    CONFIG_RESTRICTED = ("SqlServerAlertIncidents", "for_KernelTlsLifecycle", "for_ContainerStartup")

    def test_the_kernel_and_clustering_suites_cover_every_directory_exactly_once(self):
        for suite, root in (("kernel", "Integration/Kernel"), ("clustering", "Integration/Clustering")):
            with self.subTest(suite=suite):
                prefix = matrix.SUITES[suite]["prefix"]
                covered = []
                for entry in _run("--suite", suite):
                    covered += _members(entry) or [entry["namespace"]]
                self.assertEqual(sorted(prefix + name for name in _directories(root)), sorted(covered))

    def test_the_kernel_and_clustering_suites_have_the_expected_job_counts(self):
        self.assertEqual(5, len(_run("--suite", "kernel")))
        self.assertEqual(2, len(_run("--suite", "clustering")))

    def test_kernel_and_clustering_filters_end_with_a_dot_so_a_namespace_is_not_a_prefix(self):
        for suite in ("kernel", "clustering"):
            for entry in _run("--suite", suite):
                for part in entry["filter"].split("|"):
                    self.assertRegex(part, r"^FullyQualifiedName~[\w.]+\.$")

    def test_batch_members_exist_are_disjoint_and_are_not_sharded_or_config_restricted(self):
        seen = set()
        for suite, batches in matrix.BATCHES.items():
            root = matrix.SUITES[suite]["root"]
            for batch in batches:
                self.assertGreater(len(batch), 1)
                for name in batch:
                    with self.subTest(suite=suite, member=name):
                        self.assertTrue(os.path.isdir(os.path.join(REPOSITORY_ROOT, root, name)))
                        self.assertNotIn((suite, name), seen)
                        seen.add((suite, name))
                        if suite == "client":
                            self.assertNotIn(name, matrix.SHARD_COUNTS)
                            self.assertNotIn(name, self.CONFIG_RESTRICTED)

    def test_every_client_batch_member_has_a_plain_namespace_filter(self):
        # The parity-wrapper union path is not a plain namespace filter, so a member there cannot be batched.
        for entry in _run():
            if entry["members"]:
                for member in _members(entry):
                    self.assertIn(f"FullyQualifiedName~{member}", entry["filter"].split("|"))

    def test_the_filters_in_a_batched_run_equal_the_filters_of_the_unbatched_run(self):
        for suite in ("client", "kernel", "clustering"):
            with self.subTest(suite=suite):
                solo = _build(suite, {})
                batched = _build(suite)
                configs = {(e.get("mode"), e.get("database")) for e in solo}
                self.assertEqual(configs, {(e.get("mode"), e.get("database")) for e in batched})
                for config in configs:
                    def union(include):
                        return {part for e in include if (e.get("mode"), e.get("database")) == config
                                for part in e["filter"].split("|")}
                    self.assertEqual(union(solo), union(batched))

    def test_every_entry_has_members_and_labels_are_unique_per_configuration(self):
        for suite in ("client", "kernel", "clustering"):
            include = _run("--suite", suite)
            labels = set()
            for entry in include:
                self.assertIn("members", entry)
                label = (entry["namespace"], entry["shard"], entry.get("mode"), entry.get("database"))
                self.assertNotIn(label, labels)
                labels.add(label)
                if entry["members"]:
                    self.assertEqual(
                        matrix.SUITES[suite]["prefix"] + "+".join(m[len(matrix.SUITES[suite]["prefix"]):] for m in _members(entry)),
                        entry["namespace"])
                    self.assertEqual("all", entry["shard"])

    def test_batching_removes_twelve_client_jobs_from_a_pull_request_run(self):
        self.assertEqual(len(_build("client", {})) - 12, len(_build("client")))

    def test_a_namespace_in_a_batch_that_is_sharded_is_rejected(self):
        with self.assertRaises(ValueError):
            _build("client", {"client": [["Projections", "for_Reactors"]]})


if __name__ == "__main__":
    unittest.main()
