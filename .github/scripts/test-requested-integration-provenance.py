# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""The requested decision tools and policy belong to trusted revisions, not the PR checkout."""
from dataclasses import dataclass
import os
from pathlib import Path
import re
import shlex
import subprocess
import tempfile
import unittest

import yaml

WORKFLOWS = Path(__file__).resolve().parents[1] / "workflows"
TRUSTED_REF = "${{ github.event.pull_request.base.sha }}"
WORKFLOW_NAMES = ("hot-core-gate.yml", "hot-core-gate-refresh.yml", "requested-integration.yml")


@dataclass(frozen=True)
class ToolScenario:
    missing: str = None
    missing_option: bool = False
    dispatch: bool = False

    @property
    def uses_default_tools(self):
        return any((self.missing == "base", self.missing_option, self.dispatch))


@dataclass(frozen=True)
class DecisionRun:
    workspace: Path
    env: dict
    tools: Path
    expected: str


def workflow(name):
    return yaml.load((WORKFLOWS / name).read_text(encoding="utf-8"), Loader=yaml.BaseLoader)


def workflow_jobs():
    for filename in WORKFLOW_NAMES:
        for job_id, job in workflow(filename)["jobs"].items():
            yield filename, job_id, job


def workflow_steps():
    for filename, job_id, job in workflow_jobs():
        for step in job.get("steps", []):
            yield filename, job_id, step


def materialisers():
    for filename, job_id, step in workflow_steps():
        if step.get("name") == "Materialise gate tools":
            yield filename, job_id, step


def decision_steps():
    for filename, job_id, _ in materialisers():
        for step in workflow(filename)["jobs"][job_id]["steps"]:
            yield filename, job_id, step


class RequestedIntegrationProvenance(unittest.TestCase):
    def setUp(self):
        self.jobs = workflow("requested-integration.yml")["jobs"]
        self.detect = self.jobs["requested-detect-hot-core"]
        self.steps = self.detect["steps"]

    def step(self, name):
        return next(step for step in self.steps if step.get("name") == name)

    def test_checkout_is_required_and_does_not_persist_credentials(self):
        checkouts = [step for step in self.steps if step.get("uses") == "actions/checkout@v4"]
        self.assertEqual(1, len(checkouts))
        for step in checkouts:
            self.assertEqual("false", step["with"]["persist-credentials"])
            self.assertNotIn("continue-on-error", step)
            self.assertNotIn("if", step)
        trusted = self.step("Materialise gate tools")
        self.assertEqual(TRUSTED_REF, trusted["env"]["BASE_SHA"])
        self.assertLess(self.steps.index(trusted), self.steps.index(self.step("Assert the hot-core contract is intact")))
        self.assertEqual({"contents": "read", "actions": "read"}, self.detect["permissions"])

    def test_detection_reads_only_trusted_policy_without_option_fallbacks(self):
        for name in ("Assert the hot-core contract is intact", "Detect hot-core changes"):
            script = self.step(name)["run"]
            self.assertIn('python3 -I "$GATE_TOOLS"/.github/scripts/detect-hot-core-changes.py --policy-root "$GATE_TOOLS"', script)
            self.assertNotIn("grep", script)
            self.assertNotIn("else", script)
            self.assertNotIn("${{", script)

    def test_selection_and_self_tests_run_trusted_copies_with_pr_git_data_only(self):
        script = self.step("Select the integration lanes this push needs")["run"]
        for suite in ("test-requested-reuse.py", "test-select-integration.py"):
            self.assertIn(f'trusted_python "$GATE_TOOLS"/.github/scripts/{suite}', script)
        self.assertIn('select-integration.py --policy-root "$GATE_TOOLS"', script)
        self.assertIn('python3 -I -c', script)
        self.assertIn('"$GATE_TOOLS/.github/scripts" "$@"', script)
        self.assertNotIn("grep", script)
        self.assertNotIn("cd ", script)
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


class TrustedToolsMaterialisation(unittest.TestCase):
    def test_all_four_tool_trees_are_outside_the_workspace(self):
        steps = list(materialisers())
        self.assertEqual(4, len(steps))
        for filename, job_id, step in steps:
            with self.subTest(workflow=filename, job=job_id):
                self.assertIn('tools="$RUNNER_TEMP/gate-tools"', step["run"])
                self.assertIn('echo "GATE_TOOLS=$tools" >> "$GITHUB_ENV"', step["run"])
                self.assertIn('"refs/heads/$DEFAULT_BRANCH"', step["run"])
                self.assertIn('::error::Trusted gate tools missing: $missing', step["run"])
                self.assertIn('::notice::Gate tools revision: $revision', step["run"])
                self.assertEqual("${{ github.event.repository.default_branch }}", step["env"]["DEFAULT_BRANCH"])
                self.assertEqual(TRUSTED_REF, step["env"]["BASE_SHA"])
                self.assertNotIn("github.sha", str(step))
                self.assertNotIn("${{", step["run"])
                self.assertNotIn("GITHUB_WORKSPACE", step["run"])

    def test_no_checkout_places_tools_inside_the_workspace(self):
        for filename, job_id, step in workflow_steps():
            if step.get("uses", "").startswith("actions/checkout@"):
                with self.subTest(workflow=filename, job=job_id):
                    self.assertNotIn("path", step.get("with", {}))

    def test_decision_jobs_never_run_head_scripts_or_nonisolated_python(self):
        for filename, job_id, step in decision_steps():
            with self.subTest(workflow=filename, job=job_id, step=step.get("name")):
                self.assert_trusted_script(step.get("run", ""))

    def assert_trusted_script(self, script):
        self.assertNotIn("python3 -E", script)
        self.assertNotIn("python3 .github", script)
        self.assertNotIn("cd gate-tools", script)
        self.assertNotIn("policy_args", script)
        for line in script.splitlines():
            if "python3 " in line:
                self.assertIn("python3 -I ", line)
            if "trusted_python " in line and not line.strip().startswith("trusted_python()"):
                self.assertIn('trusted_python "$GATE_TOOLS"/.github/scripts/', line)

    def git(self, root, *arguments):
        result = subprocess.run(["git", "-C", str(root), *arguments], check=True, text=True, capture_output=True)
        return result.stdout.strip()

    def required_paths(self, step):
        return shlex.split(re.search(r"required=\((.*?)\)", step["run"], re.S).group(1))

    def commit(self, root, message):
        self.git(root, "add", "-A")
        self.git(root, "commit", "-m", message)
        return self.git(root, "rev-parse", "HEAD")

    def fixture(self, directory, step, scenario):
        root = Path(directory)
        remote = root / "remote" / "repo.git"
        remote.mkdir(parents=True)
        self.git(remote, "init", "-b", "main")
        self.git(remote, "config", "user.email", "spec@example.test")
        self.git(remote, "config", "user.name", "Spec")
        paths = self.required_paths(step)
        tool = remote / paths[0]
        for path in paths:
            target = remote / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text('print("BASE")\n# --policy-root\n', encoding="utf-8")
        if scenario.missing:
            tool.unlink()
        if scenario.missing_option:
            tool.write_text('print("BASE")\n', encoding="utf-8")
        base = self.commit(remote, "Base tools")
        if scenario.uses_default_tools:
            tool.write_text('print("DEFAULT")\n# --policy-root\n', encoding="utf-8")
            self.commit(remote, "Default branch tools")
        workspace = root / "head"
        self.git(root, "clone", str(remote), str(workspace))
        self.git(workspace, "checkout", "-b", "pr-branch")
        (workspace / "gate-tools").symlink_to(".")
        head_tool = workspace / paths[0]
        head_tool.parent.mkdir(parents=True, exist_ok=True)
        head_tool.write_text('raise RuntimeError("HEAD tools must not run")\n', encoding="utf-8")
        (workspace / "runpy.py").write_text('raise RuntimeError("HEAD module must not load")\n', encoding="utf-8")
        self.git(workspace, "config", "user.email", "spec@example.test")
        self.git(workspace, "config", "user.name", "Spec")
        self.commit(workspace, "Head tree with gate-tools symlink")
        self.git(workspace, "push", "origin", "HEAD:refs/heads/pr-branch")
        return root, workspace, base, paths[0]

    def simulate(self, step, scenario=ToolScenario()):
        with tempfile.TemporaryDirectory() as directory:
            root, workspace, base, tool = self.fixture(directory, step, scenario)
            runner = root / "runner-temp"
            runner.mkdir()
            env_file = root / "github-env"
            env_file.touch()
            env = {**os.environ, "RUNNER_TEMP": str(runner), "GITHUB_WORKSPACE": str(workspace),
                   "GITHUB_SERVER_URL": (root / "remote").as_uri(), "GITHUB_REPOSITORY": "repo",
                   "BASE_SHA": "" if scenario.dispatch else base, "DEFAULT_BRANCH": "main", "GITHUB_ENV": str(env_file),
                   "GITHUB_OUTPUT": str(root / "github-output"),
                   "GITHUB_SHA": self.git(workspace, "rev-parse", "HEAD"), "GITHUB_REF": "refs/heads/pr-branch",
                   "GITHUB_EVENT_NAME": "workflow_dispatch" if scenario.dispatch else "pull_request"}
            result = subprocess.run(["bash", "-c", step["run"]], cwd=workspace, env=env, text=True, capture_output=True)
            if scenario.missing == "both":
                self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
                self.assertIn(f"::error::Trusted gate tools missing: {tool}", result.stdout)
                self.assertEqual("", env_file.read_text())
                return
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            tools = runner / "gate-tools"
            expected = "DEFAULT" if scenario.uses_default_tools else "BASE"
            self.assertIn(expected, (tools / tool).read_text())
            self.assertNotIn("HEAD tools", (tools / tool).read_text())
            self.assertEqual(f"GATE_TOOLS={tools}\n", env_file.read_text())
            self.assertTrue((workspace / "gate-tools").is_symlink())
            self.assertIn("HEAD tools", (workspace / tool).read_text())
            self.assertFalse(tools.is_relative_to(workspace))
            self.assertIn("::notice::Gate tools revision:", result.stdout)
            if scenario.dispatch:
                default = self.git(root / "remote" / "repo.git", "rev-parse", "main")
                self.assertIn(f"::notice::Gate tools revision: {default}\n", result.stdout)
                self.assertNotIn(f"::notice::Gate tools revision: {env['GITHUB_SHA']}\n", result.stdout)
            self.run_decisions(step, DecisionRun(workspace, env, tools, expected))

    def run_decisions(self, materialiser, run):
        filename, job_id, _ = next(item for item in materialisers() if item[2] == materialiser)
        steps = workflow(filename)["jobs"][job_id]["steps"]
        scripts = "\n".join(step.get("run", "") for step in steps[steps.index(materialiser) + 1:])
        env = {**run.env, "GATE_TOOLS": str(run.tools), "PYTHONPATH": str(run.workspace), "HEAD_SHA": "HEAD"}
        result = subprocess.run(["bash", "-e", "-c", scripts], cwd=run.workspace, env=env, text=True, capture_output=True)
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn(run.expected, result.stdout)
        self.assertNotIn("HEAD tools", result.stdout + result.stderr)
        self.assertNotIn("HEAD module", result.stdout + result.stderr)

    def test_head_symlink_still_extracts_base_content_under_runner_temp(self):
        for filename, job_id, step in materialisers():
            with self.subTest(workflow=filename, job=job_id):
                self.simulate(step)

    def test_missing_base_tool_uses_default_branch_not_head(self):
        for filename, job_id, step in materialisers():
            with self.subTest(workflow=filename, job=job_id):
                self.simulate(step, ToolScenario(missing="base"))

    def test_missing_tool_in_both_trusted_revisions_fails_closed(self):
        for filename, job_id, step in materialisers():
            with self.subTest(workflow=filename, job=job_id):
                self.simulate(step, ToolScenario(missing="both"))

    def test_non_default_branch_dispatch_uses_default_head_never_github_sha(self):
        for filename, job_id, step in materialisers():
            with self.subTest(workflow=filename, job=job_id):
                self.simulate(step, ToolScenario(dispatch=True))

    def assert_default_resolution_fails(self, step, listing, status):
        mock_git = '''git() {
          if [ "$1" = ls-remote ]; then
            printf '%s\\n' "$LISTING"
            return "$LOOKUP_STATUS"
          fi
          command git "$@"
        }
        '''
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            env_file = root / "github-env"
            env_file.touch()
            env = {**os.environ, "BASE_SHA": "", "DEFAULT_BRANCH": "main", "RUNNER_TEMP": str(root),
                   "GITHUB_SERVER_URL": root.as_uri(), "GITHUB_REPOSITORY": "missing",
                   "GITHUB_ENV": str(env_file), "LISTING": listing, "LOOKUP_STATUS": str(status)}
            result = subprocess.run(["bash", "-c", mock_git + step["run"]], env=env, text=True, capture_output=True)
            self.assertNotEqual(0, result.returncode, result.stdout + result.stderr)
            error = "Cannot resolve" if status else "Invalid"
            self.assertIn(f"::error::{error} trusted gate tools default-branch head: refs/heads/main", result.stderr)
            self.assertEqual("", env_file.read_text())

    def test_missing_or_invalid_default_branch_head_fails_closed(self):
        sha = "a" * 40
        listings = ("", "not-a-sha\trefs/heads/main", f"{sha[:-1]}\trefs/heads/main",
                    f"{'g' * 40}\trefs/heads/main", f"{sha}\trefs/heads/pr-branch",
                    f"{sha}\trefs/heads/main\n{sha}\trefs/heads/main", f"{sha} refs/heads/main")
        for filename, job_id, step in materialisers():
            for listing in listings:
                with self.subTest(workflow=filename, job=job_id, listing=listing):
                    self.assert_default_resolution_fails(step, listing, 0)

    def test_failed_anonymous_default_branch_lookup_fails_closed(self):
        for filename, job_id, step in materialisers():
            with self.subTest(workflow=filename, job=job_id):
                self.assert_default_resolution_fails(step, "", 1)

    def test_missing_base_policy_option_uses_default_branch(self):
        for filename, job_id, step in materialisers():
            if "--policy-root" in step["run"]:
                with self.subTest(workflow=filename, job=job_id):
                    self.simulate(step, ToolScenario(missing_option=True))


if __name__ == "__main__":
    unittest.main()
