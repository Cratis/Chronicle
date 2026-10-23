# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Guard trigger separation and check names so unrelated labels cannot mask failures."""

from dataclasses import dataclass
from itertools import product
import importlib.util
from pathlib import Path
import re
import subprocess
import unittest

import yaml

WORKFLOWS = Path(__file__).resolve().parents[1] / "workflows"
EFFECTIVE_LABEL = ("(github.event.action == 'labeled' && github.event.label.name || "
                   "contains(github.event.pull_request.labels.*.name, 'run-integration') && "
                   "'run-integration' || 'none')")


def workflow(filename):
    # PyYAML treats `on` as YAML 1.1's boolean true; BaseLoader keeps GitHub's key intact.
    with (WORKFLOWS / filename).open(encoding="utf-8") as file:
        return yaml.load(file, Loader=yaml.BaseLoader)


@dataclass(frozen=True)
class ConditionContext:
    event: str
    requested: bool
    results: dict
    cancelled: bool = False
    actor: str = "contributor"

    def bindings(self):
        values = {
            "github.event_name": self.event,
            "github.actor": self.actor,
            "inputs.integration_requested": self.requested,
            "cancelled()": self.cancelled,
        }
        values.update({f"needs.{name}.result": result for name, result in self.results.items()})
        return values


def implicit_success_allows(expression, dependencies, results):
    statuses = ("success()", "failure()", "cancelled()", "always()")
    if any(status in expression for status in statuses):
        return True
    return all(results[dependency] == "success" for dependency in dependencies)


def evaluate_boolean_condition(expression, bindings):
    for token, value in bindings.items():
        expression = expression.replace(token, repr(value))
    expression = expression.replace("&&", " and ").replace("||", " or ")
    expression = re.sub(r"!(?!=)", " not ", expression).strip()
    # Only substituted literals and boolean/comparison operators may reach eval.
    remainder = re.sub(r"'[^']*'|True|False|and|or|not|==|!=|[()\s]", "", expression)
    if remainder:
        raise AssertionError(f"Unsupported condition tokens: {remainder}")
    return eval(expression, {"__builtins__": {}}, {})


def condition_allows(job, context):
    """Evaluate these jobs' boolean conditions, including GitHub's implicit success().

    This checks eligibility after needs complete, not scheduler timing. Separate DAG
    assertions below prove which jobs must complete before DEVELOPMENT is eligible.
    """
    expression = job["if"].removeprefix("${{").removesuffix("}}").strip()
    if not implicit_success_allows(expression, job["needs"], context.results):
        return False
    return evaluate_boolean_condition(expression, context.bindings())


def dependency_ancestors(jobs, job_id):
    ancestors = set(jobs[job_id].get("needs", []))
    for dependency in jobs[job_id].get("needs", []):
        ancestors.update(dependency_ancestors(jobs, dependency))
    return ancestors


class SparseIntegrationWorkflows(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build = workflow("dotnet-build.yml")
        cls.hot_core = workflow("hot-core-gate.yml")
        cls.requested = workflow("requested-integration.yml")
        cls.integration = workflow("integration.yml")
        cls.refresh = workflow("hot-core-gate-refresh.yml")

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

    def test_requested_workflow_runs_on_labels_pushes_and_reopening_without_path_filter(self):
        self.assertEqual(["labeled", "synchronize", "reopened"], self.requested["on"]["pull_request"]["types"])
        self.assertNotIn("paths", self.requested["on"]["pull_request"])
        self.assertEqual("${{ github.workflow }}-${{ github.event.pull_request.number || github.ref }}-"
                         + "${{ " + EFFECTIVE_LABEL + " }}", self.requested["concurrency"]["group"])
        self.assertEqual("true", self.requested["concurrency"]["cancel-in-progress"])

    def test_unrelated_label_skips_distinctly_named_checks(self):
        ordinary_names = set(self.build["jobs"]) | set(self.hot_core["jobs"])
        for job_id, job in self.requested["jobs"].items():
            with self.subTest(job=job_id):
                self.assertNotIn(job_id, ordinary_names)
                self.assertIn(EFFECTIVE_LABEL + " == 'run-integration'", job["if"])
                self.assertEqual(job_id + "-${{ " + EFFECTIVE_LABEL + " }}", job["name"])

    def test_effective_label_preserves_evidence_names_and_job_eligibility(self):
        for (action, label), carries_label, same_repo, cancelled in product(
                (("labeled", "run-integration"), ("labeled", "patch"), ("synchronize", ""), ("reopened", "")),
                (False, True), (False, True), (False, True)):
            with self.subTest(action=action, label=label, carries_label=carries_label,
                              same_repo=same_repo, cancelled=cancelled):
                bindings = {"github.event.action": action, "github.event.label.name": label,
                            "contains(github.event.pull_request.labels.*.name, 'run-integration')": carries_label,
                            "github.event.pull_request.head.repo.full_name == github.repository": same_repo,
                            "cancelled()": cancelled}
                expected = label if action == "labeled" else "run-integration" if carries_label else "none"
                self.assertEqual(expected, evaluate_boolean_condition(EFFECTIVE_LABEL, bindings))
                for job_id, job in self.requested["jobs"].items():
                    expression = job["if"].removeprefix("${{").removesuffix("}}").strip()
                    eligible = expected == "run-integration" and (
                        not cancelled if job_id == "hot-core-gate-requested" else same_repo)
                    self.assertEqual(eligible, evaluate_boolean_condition(expression, bindings))
                    self.assertEqual(job_id + "-" + expected,
                                     job["name"].replace("${{ " + EFFECTIVE_LABEL + " }}", expected))

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
        for job in ("dotnet-build-development-requested", "integration", "integration-api", "mongodb"):
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
        self.assertIn("python3 .github/scripts/test-evaluate-hot-core-gate.py", step["run"])

    def test_refresh_is_default_branch_only_and_least_privilege(self):
        self.assertEqual({"workflow_run": {"workflows": [self.requested["name"]], "types": ["completed"]}},
                         self.refresh["on"])
        self.assertEqual("Requested Integration", self.requested["name"])
        self.assertEqual({}, self.refresh["permissions"])
        job = self.refresh["jobs"]["refresh"]
        self.assertEqual({"contents": "read", "actions": "write", "pull-requests": "read"}, job["permissions"])
        for guard in ("github.event.workflow_run.event == 'pull_request'",
                      "github.event.workflow_run.conclusion == 'success'",
                      "github.event.workflow_run.head_repository.full_name == github.repository"):
            self.assertIn(guard, job["if"])
        for step in job["steps"]:
            if step.get("uses", "").startswith("actions/checkout@"):
                self.assertNotIn("ref", step["with"])
                self.assertEqual("false", step["with"]["persist-credentials"])
            self.assertNotIn("${{", step.get("run", ""))
        self.assertNotIn("pull_request_target", self.refresh["on"])
        refresh_step = job["steps"][-1]
        self.assertEqual("${{ github.event.workflow_run.head_sha }}", refresh_step["env"]["HEAD_SHA"])
        self.assertEqual("python3 .github/scripts/evaluate-hot-core-gate.py refresh", refresh_step["run"])
        self.assertEqual("false", self.refresh["concurrency"]["cancel-in-progress"])
        # workflow_run.event stays pull_request for labeled, synchronize and reopened alike.
        self.assertEqual("github.event.workflow_run.event == 'pull_request' && "
                         "github.event.workflow_run.conclusion == 'success' && "
                         "github.event.workflow_run.head_repository.full_name == github.repository", job["if"])
        self.assertTrue(evaluate_boolean_condition(job["if"], {
            "github.event.workflow_run.event": "pull_request",
            "github.event.workflow_run.conclusion": "success",
            "github.event.workflow_run.head_repository.full_name == github.repository": True,
        }))

    def test_hot_core_gate_reports_on_every_pull_request(self):
        trigger = self.hot_core["on"]["pull_request"]
        self.assertNotIn("paths", trigger)
        self.assertNotIn("paths-ignore", trigger)
        self.assertEqual(["**"], trigger["branches"])
        self.assertNotIn("branches-ignore", trigger)
        job = self.hot_core["jobs"]["hot-core-gate"]
        self.assertEqual("always()", job["if"])
        self.assertEqual("hot-core-gate", job.get("name", "hot-core-gate"))

    def test_gate_keeps_check_name_and_evaluates_the_pr_head(self):
        job = self.hot_core["jobs"]["hot-core-gate"]
        self.assertEqual("hot-core-gate", job.get("name", "hot-core-gate"))
        self.assertEqual("read", job["permissions"]["actions"])
        step = next(step for step in job["steps"] if step.get("name") == "Decide")
        self.assertEqual("${{ github.event.pull_request.head.sha || github.sha }}", step["env"]["HEAD_SHA"])
        self.assertEqual("${{ contains(github.event.pull_request.labels.*.name, 'run-integration') }}",
                         step["env"]["RUN_INTEGRATION"])
        # All three pull_request actions build/detect the merge commit; evidence is keyed to the PR head.
        detect = next(step for step in self.requested["jobs"]["requested-detect-hot-core"]["steps"]
                      if step.get("id") == "detect")
        self.assertEqual("${{ github.sha }}", detect["env"]["HEAD_SHA"])
        self.assertEqual("python3 .github/scripts/evaluate-hot-core-gate.py gate", step["run"])
        self.assertEqual("always()", job["if"])

    def test_requested_run_names_the_head_and_skipped_matrix_fails(self):
        self.assertIn("github.event.pull_request.head.sha", self.requested["run-name"])
        gate = self.requested["jobs"]["hot-core-gate-requested"]["steps"][0]
        self.assertEqual("${{ github.event.pull_request.head.sha }}", gate["env"]["HEAD_SHA"])
        run = gate["run"]
        self.assertIn('if [ -z "$ALL_PROVIDERS" ]', run)
        self.assertLess(run.index('-z "$ALL_PROVIDERS"'), run.index('if [ "$TOUCHED" != "true" ]'))

    def test_gate_reads_the_current_pull_request_number(self):
        step = next(step for step in self.hot_core["jobs"]["hot-core-gate"]["steps"] if step.get("name") == "Decide")
        self.assertEqual("${{ github.event.pull_request.number }}", step["env"]["PR_NUMBER"])

    def test_evidence_job_name_matches_the_requested_workflow(self):
        spec = importlib.util.spec_from_file_location("evaluate_hot_core_gate",
                                                      WORKFLOWS.parent / "scripts/evaluate-hot-core-gate.py")
        evaluator = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(evaluator)
        name = self.requested["jobs"]["hot-core-gate-requested"]["name"]
        self.assertEqual(evaluator.EVIDENCE_JOB, name.replace("${{ " + EFFECTIVE_LABEL + " }}", "run-integration"))
        self.assertEqual("hot-core-gate", evaluator.GATE_JOB)

    def test_pull_request_integration_waits_for_passing_specs(self):
        jobs = self.build["jobs"]
        for job_id in ("integration", "integration-api", "mongodb"):
            with self.subTest(job=job_id):
                job = jobs[job_id]
                self.assertIn("specs", job["needs"])
                self.assertIn("!cancelled()", job["if"])
                self.assertIn("needs.dotnet-build.result == 'success'", job["if"])
                self.assertIn("(github.event_name != 'pull_request' || (inputs.integration_requested && needs.specs.result == 'success'))",
                              job["if"])
        self.assertIn("needs.dotnet-build-development.result == 'success'", jobs["integration"]["if"])
        self.assertIn("github.actor != 'dependabot[bot]'", jobs["integration"]["if"])

    def test_sql_specs_run_in_parallel_without_reducing_coverage(self):
        jobs = self.build["jobs"]
        sql = jobs["specs-sql"]
        self.assertEqual(["postgresql", "sqlserver-1", "sqlserver-2", "sqlserver-3", "sqlserver-4", "remaining"],
                         sql["strategy"]["matrix"]["shard"])
        self.assertEqual("false", sql["strategy"]["fail-fast"])
        steps = sql["steps"]
        run = next(step["run"] for step in steps if step["name"] == "Run SQL specs with coverage")
        self.assertIn("set -euo pipefail", run)
        self.assertIn("sql-spec-filter.py '${{ matrix.shard }}'", run)
        self.assertIn('Storage.Sql.Specs.csproj --no-build', run)
        self.assertIn('--filter "$filter"', run)
        self.assertTrue(any(step.get("uses") == "./.github/actions/assert-tests-ran" for step in steps))
        self.assertEqual("coverage-results-specs-sql-${{ matrix.shard }}", steps[-1]["with"]["name"])
        ordinary = next(step["run"] for step in jobs["specs-other"]["steps"]
                        if step["name"] == "Run tests with coverage")
        self.assertIn("&FullyQualifiedName!~Cratis.Chronicle.Storage.Sql.", ordinary)
        download = next(step for step in jobs["coverage-merge-and-cache"]["steps"]
                        if step["name"] == "Download all coverage artifacts")
        self.assertEqual("coverage-results-*", download["with"]["pattern"])

    def test_stable_specs_check_requires_every_shard_to_succeed(self):
        aggregate = self.build["jobs"]["specs"]
        self.assertEqual("always()", aggregate["if"])
        self.assertEqual(["specs-other", "specs-sql"], aggregate["needs"])
        step = aggregate["steps"][0]
        self.assertEqual("${{ needs.specs-other.result }}", step["env"]["OTHER_RESULT"])
        self.assertEqual("${{ needs.specs-sql.result }}", step["env"]["SQL_RESULT"])
        for ordinary, sql in product(("success", "failure", "cancelled", "skipped"), repeat=2):
            with self.subTest(ordinary=ordinary, sql=sql):
                result = subprocess.run(["bash", "-c", step["run"]],
                                        env={"OTHER_RESULT": ordinary, "SQL_RESULT": sql}, check=False)
                self.assertEqual(ordinary == sql == "success", result.returncode == 0)

    def test_development_paths_preserve_steps_permissions_and_artifact(self):
        jobs = self.build["jobs"]
        parallel = jobs["dotnet-build-development"]
        requested = jobs["dotnet-build-development-requested"]
        self.assertEqual("github.event_name != 'pull_request'", parallel["if"])
        self.assertEqual("github.event_name == 'pull_request' && inputs.integration_requested",
                         requested["if"])
        self.assertEqual(["wire-compatibility", "grain-compatibility"], parallel["needs"])
        self.assertEqual(["wire-compatibility", "grain-compatibility", "specs"], requested["needs"])
        self.assertEqual(["specs-other", "specs-sql"], jobs["specs"]["needs"])
        for job_id in ("specs-other", "specs-sql"):
            self.assertEqual(["dotnet-build"], jobs[job_id]["needs"])
        self.assertEqual(["wire-compatibility", "grain-compatibility"], jobs["dotnet-build"]["needs"])
        for key in ("steps", "permissions", "runs-on", "timeout-minutes"):
            self.assertEqual(parallel[key], requested[key], key)
        steps = requested["steps"]
        self.assertEqual(8, len(steps))
        build = next(step for step in steps if step.get("name") == "Build with DEVELOPMENT symbol")
        self.assertEqual("dotnet build --configuration Release -p:EnableDevelopmentSymbol=true "
                         "-p:DisableProxyGenerator=true -p:DisableDockerBuild=true", build["run"])
        upload = steps[-1]["with"]
        self.assertEqual("build-output-development", upload["name"])
        self.assertEqual("build-output-development.tar.zst", upload["path"])
        self.assertEqual("build-output-development", jobs["integration"]["with"]["build-output-artifact"])
        self.assertEqual("${{ needs.dotnet-build.outputs.chronicle-image }}",
                         jobs["integration"]["with"]["chronicle-image"])

    def test_development_eligibility_for_specs_success_failure_cancel_and_skip(self):
        jobs = self.build["jobs"]
        states = ("success", "failure", "cancelled", "skipped")
        for event, requested, specs, wire, grain in product(
                ("pull_request", "schedule", "workflow_dispatch"), (False, True), states, states, states):
            results = {"wire-compatibility": wire, "grain-compatibility": grain, "specs": specs}
            context = ConditionContext(event, requested, results)
            with self.subTest(event=event, requested=requested, specs=specs, wire=wire, grain=grain):
                gates = wire == "success" and grain == "success"
                self.assertEqual(event != "pull_request" and gates,
                                 condition_allows(jobs["dotnet-build-development"], context))
                self.assertEqual(event == "pull_request" and requested and
                                 specs == "success" and gates,
                                 condition_allows(jobs["dotnet-build-development-requested"], context))

    def test_integration_requires_selected_output_but_allows_skipped_sibling(self):
        job = self.build["jobs"]["integration"]
        self.assertEqual({"dotnet-build", "specs", "dotnet-build-development",
                          "dotnet-build-development-requested"}, set(job["needs"]))
        states = ("success", "failure", "cancelled", "skipped")
        for event, requested, specs, development, release, cancelled in product(
                ("pull_request", "schedule", "workflow_dispatch"), (False, True),
                states, states, states, (False, True)):
            is_pr = event == "pull_request"
            results = {"dotnet-build": release, "specs": specs,
                       "dotnet-build-development": "skipped" if is_pr else development,
                       "dotnet-build-development-requested": development if is_pr else "skipped"}
            expected = (not cancelled and release == "success" and development == "success"
                        and (not is_pr or (requested and specs == "success")))
            with self.subTest(event=event, requested=requested, results=results, cancelled=cancelled):
                context = ConditionContext(event, requested, results, cancelled)
                self.assertEqual(expected, condition_allows(job, context))
        dependabot = ConditionContext("schedule", False, {name: "success" for name in job["needs"]},
                                      actor="dependabot[bot]")
        self.assertFalse(condition_allows(job, dependabot))

    def test_coverage_cannot_be_cached_if_any_integration_dependency_failed_or_was_skipped(self):
        coverage = self.build["jobs"]["coverage-merge-and-cache"]
        self.assertEqual({"specs", "integration", "integration-api", "mongodb"}, set(coverage["needs"]))
        self.assertIn("github.event_name != 'pull_request_target'", coverage["if"])
        self.assertIn("!cancelled()", coverage["if"])
        for dependency in coverage["needs"]:
            self.assertIn(f"needs.{dependency}.result == 'success'", coverage["if"])

    def test_coverage_allows_successful_paths_despite_skipped_development_ancestor(self):
        jobs = self.build["jobs"]
        coverage = jobs["coverage-merge-and-cache"]
        ancestors = dependency_ancestors(jobs, "coverage-merge-and-cache")
        self.assertIn("dotnet-build-development", ancestors)
        self.assertIn("dotnet-build-development-requested", ancestors)
        # Model transitive implicit-success propagation without claiming to emulate
        # GitHub's scheduler. Explicit status functions must bypass this restriction.
        modeled_job = dict(coverage, needs=sorted(ancestors))
        for event in ("pull_request", "schedule", "workflow_dispatch"):
            results = dict.fromkeys(ancestors, "success")
            sibling = ("dotnet-build-development" if event == "pull_request"
                       else "dotnet-build-development-requested")
            results[sibling] = "skipped"
            context = ConditionContext(event, True, results)
            with self.subTest(event=event):
                self.assertTrue(condition_allows(modeled_job, context))
                # Seed the old guard: this fixture must detect the skipped ancestor.
                old_guard = dict(modeled_job, **{"if": "github.event_name != 'pull_request_target'"})
                self.assertFalse(condition_allows(old_guard, context))

    def test_coverage_rejects_incomplete_cancelled_ordinary_and_dependabot_runs(self):
        coverage = self.build["jobs"]["coverage-merge-and-cache"]
        states = ("success", "failure", "cancelled", "skipped", "")
        for event, cancelled, results in product(
                ("pull_request", "schedule", "workflow_dispatch", "pull_request_target"),
                (False, True), product(states, repeat=len(coverage["needs"]))):
            prerequisites = dict(zip(coverage["needs"], results))
            context = ConditionContext(event, True, prerequisites, cancelled)
            expected = (event != "pull_request_target" and not cancelled
                        and all(result == "success" for result in results))
            with self.subTest(event=event, cancelled=cancelled, results=prerequisites):
                self.assertEqual(expected, condition_allows(coverage, context))
        # Ordinary PRs skip integration; Dependabot also skips the matrix. Keep
        # their existing coverage behavior through immediate prerequisite results.
        for actor in ("contributor", "dependabot[bot]"):
            results = dict.fromkeys(coverage["needs"], "success")
            results["integration"] = "skipped"
            context = ConditionContext("pull_request", False, results, actor=actor)
            self.assertFalse(condition_allows(coverage, context))


if __name__ == "__main__":
    unittest.main()
