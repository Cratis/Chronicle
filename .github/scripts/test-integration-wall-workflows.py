# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Guard the integration wall: slot caps and the partitioned parallel coverage merge (#5875)."""

import fnmatch
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import unittest

SCRIPTS = Path(__file__).resolve().parent


def _load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


sparse = _load(SCRIPTS / "test-sparse-integration-workflows.py", "sparse_integration_workflows")
workflow = sparse.workflow
prerequisites_of = sparse.prerequisites_of

# What the coverage merges wait for and require; the final merge additionally needs the partials.
LANE_PREREQUISITES = {"specs", "integration", "integration-api", "mongodb", "neo4j"}

# Slot budget (#5875): the client cap plus the capped lanes.
INTEGRATION_CAPS = {
    "client-integration-specs": 40,
    "kernel-integration-specs": 2,
    "clustering-integration-specs": 1,
    "sql-clustering-integration-specs": 1,
    "sql-clustering-rolling-upgrade": 1,
}

LANE_ARTIFACT_PREFIXES = (
    "coverage-results-client-", "coverage-results-kernel-", "coverage-results-clustering-",
    "coverage-results-specs-other-", "coverage-results-specs-sql-", "coverage-results-neo4j-",
    "coverage-results-integration-api", "coverage-results-mongodb")


def expand_braces(pattern):
    """Expand one {a,b} alternation, which the artifact glob supports and fnmatch does not."""
    match = re.search(r"\{([^{}]*)\}", pattern)
    if not match:
        return [pattern]
    head, tail = pattern[:match.start()], pattern[match.end():]
    return [expanded for option in match.group(1).split(",") for expanded in expand_braces(head + option + tail)]


def matches_pattern(pattern, name):
    return any(fnmatch.fnmatchcase(name, expanded) for expanded in expand_braces(pattern))


def generated_matrix(suite):
    completed = subprocess.run(
        ["python3", "-B", str(SCRIPTS / "generate-integration-matrix.py"), "--suite", suite, "--all-providers"],
        check=True, capture_output=True, text=True, timeout=60)
    return json.loads(completed.stdout)["include"]


def coverage_artifact_names(jobs):
    """Every coverage-results-* artifact name an all-provider run can upload, from the matrices that name them."""
    names = ["coverage-results-integration-api", "coverage-results-mongodb"]
    names += [f"coverage-results-client-{e['namespace']}-{e['shard']}-{e['mode']}-{e['database']}"
              for e in generated_matrix("client")]
    for suite in ("kernel", "clustering"):
        names += [f"coverage-results-{suite}-{e['namespace']}" for e in generated_matrix(suite)]
    for lane in ("specs-other", "specs-sql"):
        names += [f"coverage-results-{lane}-{shard}" for shard in jobs[lane]["strategy"]["matrix"]["shard"]]
    names += [f"coverage-results-neo4j-{entry['lane']}" for entry in jobs["neo4j"]["strategy"]["matrix"]["include"]]
    return names


def assert_requires_every_dependency(test, job):
    test.assertIn("github.event_name != 'pull_request_target'", job["if"])
    test.assertIn("!cancelled()", job["if"])
    for dependency in prerequisites_of(job):
        test.assertIn(f"needs.{dependency}.result == 'success'", job["if"])


class IntegrationWallWorkflows(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build = workflow("dotnet-build.yml")
        cls.integration = workflow("integration.yml")

    def test_the_final_merge_waits_for_the_lanes_and_the_partials(self):
        coverage = self.build["jobs"]["coverage-merge-and-cache"]
        self.assertEqual(LANE_PREREQUISITES | {"coverage-partial"}, set(prerequisites_of(coverage)))
        assert_requires_every_dependency(self, coverage)

    def test_every_partial_merge_requires_the_same_lanes_as_the_final_merge(self):
        partial = self.build["jobs"]["coverage-partial"]
        self.assertEqual(LANE_PREREQUISITES, set(prerequisites_of(partial)))
        self.assertEqual("false", partial["strategy"]["fail-fast"])
        assert_requires_every_dependency(self, partial)

    def test_an_empty_group_is_skipped_never_failed(self):
        steps = self.build["jobs"]["coverage-partial"]["steps"]
        for step in steps[2:]:
            self.assertIn("steps.inputs.outputs.count != '0'", step["if"])

    def test_the_coverage_groups_are_named_once(self):
        groups = self.build["jobs"]["coverage-partial"]["strategy"]["matrix"]["include"]
        self.assertEqual(len(groups), len({group["group"] for group in groups}))

    def test_every_lane_contributes_coverage_artifacts(self):
        names = coverage_artifact_names(self.build["jobs"])
        for prefix in LANE_ARTIFACT_PREFIXES:
            self.assertTrue(any(name.startswith(prefix) for name in names), prefix)

    def test_the_coverage_groups_partition_every_coverage_artifact_exactly_once(self):
        groups = self.build["jobs"]["coverage-partial"]["strategy"]["matrix"]["include"]
        for name in coverage_artifact_names(self.build["jobs"]):
            owners = [group["group"] for group in groups if matches_pattern(group["pattern"], name)]
            with self.subTest(artifact=name):
                self.assertEqual(1, len(owners), owners)

    def test_the_integration_matrices_are_capped_so_the_run_peak_stays_bounded(self):
        jobs = self.integration["jobs"]
        caps = {job: int(jobs[job]["strategy"]["max-parallel"]) for job in INTEGRATION_CAPS}
        self.assertEqual(INTEGRATION_CAPS, caps)
        self.assertEqual("1", self.build["jobs"]["neo4j"]["strategy"]["max-parallel"])


if __name__ == "__main__":
    unittest.main()
