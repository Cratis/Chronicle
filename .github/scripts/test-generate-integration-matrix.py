#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for generate-integration-matrix.py.

The matrix generator decides what every pull request and every nightly run
actually verifies, so a silent change in what it selects is a silent change in
coverage. These specs pin the selection rules -- what a pull request covers,
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


class for_generating_the_matrix(unittest.TestCase):
    """The generated matrix, against the repository's real integration namespaces."""

    def test_a_pull_request_run_covers_the_pull_request_databases(self):
        self.assertEqual(set(matrix.PULL_REQUEST_DATABASES), _databases_of(_run()))

    def test_a_scheduled_run_covers_every_backend(self):
        self.assertEqual(set(matrix.ALL_DATABASES), _databases_of(_run("--all-providers")))

    def test_the_shards_are_the_same_whatever_the_backends(self):
        # Backend selection must only add or remove infrastructure legs -- never
        # change which tests a shard owns.
        def shards(include):
            return {(entry["namespace"], entry["shard"], entry["filter"]) for entry in include}

        self.assertEqual(shards(_run()), shards(_run("--all-providers")))

    def test_every_entry_carries_the_fields_the_workflow_reads(self):
        for entry in _run():
            self.assertEqual(
                {"namespace", "shard", "filter", "mode", "database", "needs-docker"},
                set(entry))

    def test_an_unknown_backend_fails_the_run(self):
        completed = subprocess.run(
            [sys.executable, SCRIPT, "--databases", "mysql"],
            cwd=REPOSITORY_ROOT,
            capture_output=True,
            text=True,
            check=False)
        self.assertNotEqual(0, completed.returncode)


if __name__ == "__main__":
    unittest.main()
