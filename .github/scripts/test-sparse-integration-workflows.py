# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Guard trigger separation and check names so unrelated labels cannot mask failures."""

from pathlib import Path
import unittest

import yaml

WORKFLOWS = Path(__file__).resolve().parents[1] / "workflows"


def workflow(filename):
    # PyYAML treats `on` as YAML 1.1's boolean true; BaseLoader keeps GitHub's key intact.
    with (WORKFLOWS / filename).open(encoding="utf-8") as file:
        return yaml.load(file, Loader=yaml.BaseLoader)


class SparseIntegrationWorkflows(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build = workflow("dotnet-build.yml")
        cls.hot_core = workflow("hot-core-gate.yml")
        cls.requested = workflow("requested-integration.yml")

    def test_ordinary_checks_never_run_for_any_label_event(self):
        for ordinary in (self.build, self.hot_core):
            with self.subTest(workflow=ordinary["name"]):
                self.assertEqual(["opened", "synchronize", "reopened"],
                                 ordinary["on"]["pull_request"]["types"])
                self.assertNotIn("push", ordinary["on"])

    def test_build_keeps_the_explicit_path_contract_and_nightly_schedule(self):
        paths = self.build["on"]["pull_request"]["paths"]
        self.assertIn("Source/**", paths)
        self.assertIn("Directory.Packages.props", paths)
        self.assertNotIn("**", paths)
        self.assertEqual("0 2 * * *", self.build["on"]["schedule"][0]["cron"])
        self.assertIn("workflow_dispatch", self.build["on"])
        self.assertIn("workflow_dispatch", self.hot_core["on"])

    def test_requested_workflow_is_label_only_with_no_path_filter(self):
        self.assertEqual(["labeled"], self.requested["on"]["pull_request"]["types"])
        self.assertNotIn("paths", self.requested["on"]["pull_request"])
        self.assertEqual("${{ github.workflow }}-${{ github.event.pull_request.number || github.ref }}-${{ github.event.label.name || 'none' }}",
                         self.requested["concurrency"]["group"])
        self.assertEqual("true", self.requested["concurrency"]["cancel-in-progress"])

    def test_unrelated_label_skips_distinctly_named_checks(self):
        ordinary_names = set(self.build["jobs"]) | set(self.hot_core["jobs"])
        for job_id, job in self.requested["jobs"].items():
            with self.subTest(job=job_id):
                self.assertNotIn(job_id, ordinary_names)
                self.assertIn("github.event.label.name == 'run-integration'", job["if"])
                self.assertEqual(f"{job_id}-${{{{ github.event.label.name }}}}", job["name"])

    def test_requested_run_reuses_build_and_enforces_both_matrices(self):
        build = self.requested["jobs"]["requested-integration"]
        self.assertEqual("./.github/workflows/dotnet-build.yml", build["uses"])
        self.assertEqual("true", build["with"]["integration_requested"])
        self.assertEqual("./.github/workflows/integration.yml",
                         self.requested["jobs"]["requested-hot-core-matrix"]["uses"])
        self.assertEqual("true", self.requested["jobs"]["requested-hot-core-matrix"]["with"]["all-providers"])
        self.assertIn("requested-hot-core-matrix", self.requested["jobs"]["hot-core-gate-requested"]["needs"])
        for job in ("dotnet-build-development", "integration", "integration-api", "mongodb"):
            self.assertIn("inputs.integration_requested", self.build["jobs"][job]["if"])
        for name in ("Build development Docker image", "Push development Docker image"):
            step = next(step for step in self.build["jobs"]["dotnet-build"]["steps"] if step.get("name") == name)
            self.assertIn("inputs.integration_requested", step["if"])

    def test_coverage_cannot_be_cached_if_any_integration_dependency_failed_or_was_skipped(self):
        coverage = self.build["jobs"]["coverage-merge-and-cache"]
        self.assertEqual({"specs", "integration", "integration-api", "mongodb"}, set(coverage["needs"]))
        self.assertEqual("github.event_name != 'pull_request_target'", coverage["if"])


if __name__ == "__main__":
    unittest.main()
