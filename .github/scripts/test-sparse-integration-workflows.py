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
        cls.integration = workflow("integration.yml")

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

    def test_hot_core_detection_precedes_one_requested_matrix(self):
        jobs = self.requested["jobs"]
        build = jobs["requested-integration"]
        self.assertEqual(["requested-detect-hot-core"], build["needs"])
        self.assertEqual("./.github/workflows/dotnet-build.yml", build["uses"])
        self.assertEqual("true", build["with"]["integration_requested"])
        self.assertEqual("${{ needs.requested-detect-hot-core.outputs.touched == 'true' }}",
                         build["with"]["all_providers"])
        self.assertFalse(any(job.get("uses") == "./.github/workflows/integration.yml"
                             for job in jobs.values()))
        self.assertEqual(1, sum(job.get("uses") == "./.github/workflows/integration.yml"
                                for job in self.build["jobs"].values()))
        self.assertEqual({"requested-integration", "requested-detect-hot-core"},
                         set(jobs["hot-core-gate-requested"]["needs"]))
        for job in ("dotnet-build-development", "integration", "integration-api", "mongodb"):
            self.assertIn("inputs.integration_requested", self.build["jobs"][job]["if"])
        for name in ("Build development Docker image", "Push development Docker image"):
            step = next(step for step in self.build["jobs"]["dotnet-build"]["steps"] if step.get("name") == name)
            self.assertIn("inputs.integration_requested", step["if"])

    def test_hot_core_gate_receives_the_selected_provider_set(self):
        self.assertIn("all_providers", self.build["on"]["workflow_call"]["inputs"])
        self.assertEqual("${{ github.event_name != 'pull_request' || inputs.all_providers }}",
                         self.build["jobs"]["integration"]["with"]["all-providers"])
        self.assertEqual("${{ jobs.integration.outputs.all-providers }}",
                         self.build["on"]["workflow_call"]["outputs"]["all_providers"]["value"])
        self.assertEqual("${{ jobs.discover.outputs.all-providers }}",
                         self.integration["on"]["workflow_call"]["outputs"]["all-providers"]["value"])
        gate = self.requested["jobs"]["hot-core-gate-requested"]["steps"][0]
        self.assertEqual("${{ needs.requested-integration.outputs.all_providers }}",
                         gate["env"]["ALL_PROVIDERS"])
        self.assertIn('if [ "$ALL_PROVIDERS" != "true" ]', gate["run"])

    def test_called_workflow_can_push_the_image_and_forwards_secrets_to_shards(self):
        build = self.requested["jobs"]["requested-integration"]
        self.assertEqual("write", build["permissions"]["packages"])
        self.assertEqual("inherit", build["secrets"])
        self.assertEqual("inherit", self.build["jobs"]["integration"]["secrets"])
        self.assertEqual("read", self.build["jobs"]["integration"]["permissions"]["packages"])
        self.assertIn("github.event.pull_request.number", self.requested["concurrency"]["group"])
        self.assertEqual("${{ github.workflow }}-${{ github.ref }}", self.build["concurrency"]["group"])

    def test_ordinary_pr_checks_the_dockerfile_without_pushing(self):
        steps = self.build["jobs"]["dotnet-build"]["steps"]
        smoke = next(step for step in steps if step.get("name") == "Verify local Docker image without push")
        self.assertEqual("github.event_name == 'pull_request' && !inputs.integration_requested", smoke["if"])
        self.assertIn("Source/Kernel/Server/bin/Release/net10.0/.", smoke["run"])
        self.assertIn("docker build", smoke["run"])
        self.assertIn("Docker/Local/Dockerfile", smoke["run"])
        self.assertNotIn("docker push", smoke["run"])
        self.assertLess(steps.index(smoke), next(index for index, step in enumerate(steps)
                                                if step.get("name") == "Pack Release build output"))

    def test_fork_request_is_skipped_with_a_notice_not_a_build_failure(self):
        jobs = self.requested["jobs"]
        for job in ("requested-integration", "requested-detect-hot-core"):
            self.assertIn("github.event.pull_request.head.repo.full_name == github.repository", jobs[job]["if"])
        gate = jobs["hot-core-gate-requested"]["steps"][0]
        self.assertEqual("${{ github.event.pull_request.head.repo.full_name != github.repository }}",
                         gate["env"]["FORK"])
        self.assertLess(gate["run"].index('if [ "$FORK" = "true" ]'),
                        gate["run"].index('if [ "$DETECT" != "success" ]'))
        self.assertIn("::notice title=Integration unavailable for fork PR", gate["run"])
        self.assertNotIn("pull_request_target", self.requested["on"])

    def test_hot_core_summary_spec_runs_in_ci(self):
        step = next(step for step in self.hot_core["jobs"]["detect"]["steps"]
                    if step.get("name") == "Assert the hot-core contract is intact")
        self.assertIn("python3 .github/scripts/test-detect-hot-core-changes.py", step["run"])

    def test_coverage_cannot_be_cached_if_any_integration_dependency_failed_or_was_skipped(self):
        coverage = self.build["jobs"]["coverage-merge-and-cache"]
        self.assertEqual({"specs", "integration", "integration-api", "mongodb"}, set(coverage["needs"]))
        self.assertEqual("github.event_name != 'pull_request_target'", coverage["if"])


if __name__ == "__main__":
    unittest.main()
