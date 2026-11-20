# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""The requested decision tools and policy belong to the base, not the PR checkout."""
from pathlib import Path
import unittest

import yaml

WORKFLOWS = Path(__file__).resolve().parents[1] / "workflows"
TRUSTED_REF = "${{ github.event_name == 'pull_request' && github.event.pull_request.base.sha || github.sha }}"


def workflow(name):
    return yaml.load((WORKFLOWS / name).read_text(encoding="utf-8"), Loader=yaml.BaseLoader)


class RequestedIntegrationProvenance(unittest.TestCase):
    def setUp(self):
        self.jobs = workflow("requested-integration.yml")["jobs"]
        self.detect = self.jobs["requested-detect-hot-core"]
        self.steps = self.detect["steps"]

    def step(self, name):
        return next(step for step in self.steps if step.get("name") == name)

    def test_checkouts_are_required_and_do_not_persist_credentials(self):
        checkouts = [step for step in self.steps if step.get("uses") == "actions/checkout@v4"]
        self.assertEqual(2, len(checkouts))
        for step in checkouts:
            self.assertEqual("false", step["with"]["persist-credentials"])
            self.assertNotIn("continue-on-error", step)
            self.assertNotIn("if", step)
        trusted = self.step("Checkout gate tools")
        self.assertEqual(TRUSTED_REF, trusted["with"]["ref"])
        self.assertEqual("gate-tools", trusted["with"]["path"])
        self.assertLess(self.steps.index(trusted), self.steps.index(self.step("Assert the hot-core contract is intact")))
        self.assertEqual({"contents": "read", "actions": "read"}, self.detect["permissions"])

    def test_detection_reads_trusted_policy_even_on_older_bases(self):
        for name in ("Assert the hot-core contract is intact", "Detect hot-core changes"):
            script = self.step(name)["run"]
            self.assertIn("grep -q -- '--policy-root' gate-tools/.github/scripts/detect-hot-core-changes.py", script)
            self.assertIn("python3 -E -s gate-tools/.github/scripts/detect-hot-core-changes.py --policy-root gate-tools", script)
            self.assertIn("::notice::", script)
            self.assertIn("cd gate-tools && python3 -E -s .github/scripts/detect-hot-core-changes.py", script)
            self.assertNotIn("${{", script)
        detection = self.step("Detect hot-core changes")["run"]
        self.assertIn('git diff --name-only "$BASE_SHA...$HEAD_SHA"', detection)
        self.assertIn('--files-from "$RUNNER_TEMP/requested-hot-core-files"', detection)

    def test_selection_and_self_tests_run_trusted_copies_with_pr_git_data_only(self):
        script = self.step("Select the integration lanes this push needs")["run"]
        for suite in ("test-requested-reuse.py", "test-select-integration.py"):
            self.assertIn(f"python3 -E -s gate-tools/.github/scripts/{suite}", script)
        self.assertIn("grep -q -- '--policy-root' gate-tools/.github/scripts/select-integration.py", script)
        self.assertIn('policy_args=(--policy-root "$GITHUB_WORKSPACE/gate-tools")', script)
        self.assertIn("::notice::", script)
        self.assertIn('GIT_DIR="$(git rev-parse --absolute-git-dir)"\nexport GIT_DIR', script)
        self.assertIn('export GIT_WORK_TREE="$GITHUB_WORKSPACE"', script)
        self.assertLess(script.index("cd gate-tools"), script.index("python3 -E -s .github/scripts/select-integration.py"))
        self.assertNotIn("${{", script)

    def test_result_is_inline_and_fails_closed_before_accepting_selection(self):
        gate = self.jobs["hot-core-gate-requested"]
        self.assertEqual({}, gate["permissions"])
        self.assertEqual(1, len(gate["steps"]))
        step = gate["steps"][0]
        self.assertEqual("Decide", step["name"])
        self.assertEqual("${{ needs.requested-detect-hot-core.result }}", step["env"]["DETECT"])
        script = step["run"]
        self.assertNotIn("python", script)
        self.assertNotIn(".github/scripts", script)
        self.assertNotIn("${{", script)
        self.assertLess(script.index('"$DETECT" != "success"'), script.index('"$MODE" = "skip"'))
        self.assertIn('echo "::error::Hot-core detection did not complete ($DETECT). Failing closed."\n  exit 1', script)

    def test_pr_copies_are_still_tested_where_pr_code_already_runs(self):
        steps = workflow("dotnet-build.yml")["jobs"]["dotnet-build"]["steps"]
        scripts = "\n".join(step.get("run", "") for step in steps)
        for suite in ("test-requested-reuse.py", "test-select-integration.py", "test-requested-integration-provenance.py"):
            self.assertIn(f"python3 .github/scripts/{suite}", scripts)


if __name__ == "__main__":
    unittest.main()
