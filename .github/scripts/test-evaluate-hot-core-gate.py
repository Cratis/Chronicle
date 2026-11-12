# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise gate/refresh decisions, API boundaries, and the CLI with recorded inputs."""

import argparse
from contextlib import redirect_stdout
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import integration_lane_exclusions as exclusions

SCRIPT = Path(__file__).with_name("evaluate-hot-core-gate.py")
spec = importlib.util.spec_from_file_location("evaluate_hot_core_gate", SCRIPT)
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
LATEST_REQUESTED = gate.latest_requested

# Recorded read-only API fields from the plan's green and skipped-label runs.
SHA = "647523d239226eec17b4718855f2f0d3930bc0ae"
SKIPPED_SHA = "33d501177e26a905871de5e390aeba188b9510ed"
GREEN = {"id": 39822452184, "head_sha": SHA, "event": "pull_request", "conclusion": "success",
         "html_url": "https://github.com/Cratis/Chronicle/actions/runs/39822452184",
         "jobs": [{"name": gate.EVIDENCE_JOB, "conclusion": "success"}]}
SKIPPED = {"id": 39837419547, "head_sha": SKIPPED_SHA, "event": "pull_request", "conclusion": "skipped",
           "jobs": [{"name": "hot-core-gate-requested-${{ github.event.label.name }}", "conclusion": "skipped"}]}
FAILED_GATE = {"id": 42, "head_sha": SHA, "event": "pull_request", "status": "completed",
               "jobs": [{"name": gate.GATE_JOB, "conclusion": "failure"}]}
BUILD_GREEN = {"id": 70, "head_sha": SHA, "event": "pull_request", "status": "completed",
               "conclusion": "success", "html_url": "https://github.com/Cratis/Chronicle/actions/runs/70",
               "jobs": [{"name": "specs", "conclusion": "success"}]}
NESTED_JOB = "requested / " + gate.EVIDENCE_JOB
ENV = {"DETECT": "success", "TOUCHED": "true", "DISPATCH": "false", "FORK": "false",
       "REPO": "Cratis/Chronicle", "HEAD_SHA": SHA, "BUILD": "skipped", "MATRIX": "skipped",
       "RUN_INTEGRATION": "false"}


def arguments(**overrides):
    return argparse.Namespace(runs_json=None, build_runs_json=None, gate_runs_json=None, pr_json=None,
                              dry_run=True, **overrides)


class GateDecisions(unittest.TestCase):
    def setUp(self):
        latest = patch.object(gate, "latest_requested", return_value=None)
        self.latest = latest.start()
        self.addCleanup(latest.stop)
        build = patch.object(gate, "latest_build", return_value=None)
        build.start()
        self.addCleanup(build.stop)

    def test_message_tells_missing_running_and_failed_integration_apart(self):
        url = "https://github.com/Cratis/Chronicle/actions/runs/7"
        cases = [(None, "No Requested Integration run has started"),
                 ({"id": 7, "status": "in_progress", "conclusion": None, "html_url": url}, "still running"),
                 ({"id": 7, "status": "completed", "conclusion": "failure", "html_url": url},
                  "ended with failure"),
                 ({"id": 7, "status": "completed", "conclusion": "success", "html_url": url},
                  "requested gate did not pass")]
        for env in (ENV, dict(ENV, TOUCHED="false", RUN_INTEGRATION="true", PR_NUMBER="7")):
            for latest, text in cases:
                with self.subTest(touched=env["TOUCHED"], latest=latest), patch.dict(os.environ, env, clear=True), \
                        patch.object(gate, "api", return_value={"head": {"sha": SHA},
                                                                "labels": [{"name": "run-integration"}]}), \
                        patch.object(gate, "evidence_runs", return_value=[]):
                    self.latest.return_value = latest
                    ok, message = gate.gate(arguments())
                    self.assertFalse(ok)
                    self.assertIn(text, message)
                    self.assertIn(SHA, message)
                    if latest:
                        self.assertIn(url, message)
        self.latest.assert_called_with("Cratis/Chronicle", SHA, None)

    def test_latest_requested_picks_newest_pull_request_run_for_the_head(self):
        older = dict(GREEN, id=1, status="completed", conclusion="failure")
        newer = dict(GREEN, id=2, status="in_progress", conclusion=None)
        other = dict(GREEN, id=3, head_sha="new-push")
        with patch.object(gate, "workflow_runs", return_value=[older, newer, other]) as runs:
            self.assertEqual(newer, LATEST_REQUESTED("Cratis/Chronicle", SHA, None))
        runs.assert_called_once_with("Cratis/Chronicle", gate.EVIDENCE_WORKFLOW, SHA)

    def test_evidence_is_exact_sha_event_and_job(self):
        cases = [([], False), ([GREEN], True), ([SKIPPED], False)]
        for field, value in [("head_sha", "new-push"), ("event", "workflow_dispatch")]:
            cases.append(([dict(GREEN, **{field: value})], False))
        for conclusion in ("failure", "cancelled", "skipped", None):
            cases.append(([dict(GREEN, jobs=[{"name": gate.EVIDENCE_JOB, "conclusion": conclusion}])], False))
        cases.append(([dict(GREEN, jobs=[{"name": "hot-core-gate-requested-ci-skip", "conclusion": "success"}])], False))
        cases.append(([dict(GREEN, jobs=[]), GREEN], True))
        for runs, expected in cases:
            with self.subTest(runs=runs):
                self.assertEqual(expected, gate.has_green_evidence(runs, SHA))

    def test_gate_decisions_and_lazy_evidence(self):
        cases = [({"TOUCHED": "false"}, True, "not required"),
                 ({"DETECT": "failure"}, False, "Failing closed"),
                 ({"FORK": "true"}, False, "trusted branch"),
                 ({"DISPATCH": "true", "BUILD": "success", "MATRIX": "success"}, True, "green"),
                 ({"DISPATCH": "true", "BUILD": "failure"}, False, "did not pass"),
                 ({"DISPATCH": "true", "BUILD": "success", "MATRIX": "cancelled"}, False, "did not pass")]
        for overrides, expected, text in cases:
            with self.subTest(overrides=overrides), patch.dict(os.environ, dict(ENV, **overrides), clear=True), \
                    patch.object(gate, "evidence_runs", side_effect=AssertionError("must not fetch")):
                ok, message = gate.gate(arguments())
                self.assertEqual(expected, ok)
                self.assertIn(text, message)

    def test_evidence_passes_even_after_label_removal_and_old_failure(self):
        failed = dict(GREEN, id=GREEN["id"] - 1, conclusion="failure", jobs=[])
        with patch.dict(os.environ, ENV, clear=True), patch.object(gate, "evidence_runs", return_value=[failed, GREEN]):
            ok, message = gate.gate(arguments())
        self.assertTrue(ok)
        self.assertIn(GREEN["html_url"], message)
        self.assertIn(SHA, message)

    def test_excluded_lane_is_reported_not_treated_as_missing_evidence(self):
        reason = ("Reactors miss acknowledged events: https://github.com/Cratis/Chronicle/issues/5300 "
                  "and https://github.com/Cratis/Chronicle/issues/5350")
        with patch.dict(exclusions.EXCLUDED_LANES,
                        {"RollingUpgrade": reason}, clear=True):
            for env in (ENV, dict(ENV, DISPATCH="true", BUILD="success", MATRIX="success")):
                with self.subTest(env=env), patch.dict(os.environ, env, clear=True), \
                        patch.object(gate, "evidence_runs", return_value=[GREEN]):
                    ok, message = gate.gate(arguments())
                self.assertTrue(ok)
                self.assertIn("Skipped integration lane RollingUpgrade (explicitly excluded)", message)
                self.assertIn(reason, message)
            # A declaration is not evidence and cannot waive any failed/missing matrix.
            for env in (ENV, dict(ENV, DISPATCH="true", BUILD="success", MATRIX="failure")):
                with self.subTest(env=env), patch.dict(os.environ, env, clear=True), \
                        patch.object(gate, "evidence_runs", return_value=[]):
                    ok, _ = gate.gate(arguments())
                self.assertFalse(ok)

    def test_no_evidence_without_label_fails_with_request_and_sha(self):
        with patch.dict(os.environ, ENV, clear=True), patch.object(gate, "evidence_runs", return_value=[]):
            ok, message = gate.gate(arguments())
        self.assertFalse(ok)
        self.assertIn("Add the `run-integration` label", message)
        self.assertIn(SHA, message)
        self.assertNotIn("re-add", message)

    def test_no_evidence_with_label_explains_automatic_push_runs_and_sha(self):
        with patch.dict(os.environ, dict(ENV, RUN_INTEGRATION="true"), clear=True), \
                patch.object(gate, "evidence_runs", return_value=[]):
            ok, message = gate.gate(arguments())
        self.assertFalse(ok)
        self.assertIn("automatically on each push", message)
        self.assertIn("no need to remove and re-add", message)
        self.assertIn("turns green automatically when that run passes", message)
        self.assertIn(SHA, message)
        self.assertNotIn("Add the", message)

    def labelled_not_touched(self, pr, runs):
        env = dict(ENV, TOUCHED="false", RUN_INTEGRATION="true", PR_NUMBER="7")
        with patch.dict(os.environ, env, clear=True), patch.object(gate, "api", return_value=pr) as api, \
                patch.object(gate, "evidence_runs", return_value=runs) as evidence:
            result = gate.gate(arguments())
        api.assert_called_once_with("repos/Cratis/Chronicle/pulls/7")
        return result, evidence

    def test_labelled_pr_without_hot_core_needs_evidence_for_its_head(self):
        labelled = {"head": {"sha": SHA}, "labels": [{"name": "run-integration"}]}
        (ok, message), _ = self.labelled_not_touched(labelled, [])
        self.assertFalse(ok)
        self.assertIn("title=Integration requested", message)
        self.assertIn(SHA, message)
        self.assertIn("remove the label and re-run this check", message)
        (ok, message), _ = self.labelled_not_touched(labelled, [GREEN])
        self.assertTrue(ok)
        self.assertIn(GREEN["html_url"], message)

    def test_labelled_pr_keeps_old_behaviour_when_label_removed_or_head_moved(self):
        for pr in ({"head": {"sha": SHA}, "labels": []},
                   {"head": {"sha": "new-push"}, "labels": [{"name": "run-integration"}]}):
            with self.subTest(pr=pr):
                (ok, message), evidence = self.labelled_not_touched(pr, [])
                self.assertTrue(ok)
                self.assertIn("not required", message)
                evidence.assert_not_called()

    def test_labelled_pr_api_error_fails_closed_in_cli(self):
        output = io.StringIO()
        env = dict(ENV, TOUCHED="false", RUN_INTEGRATION="true", PR_NUMBER="7")
        with patch.dict(os.environ, env, clear=True), patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                patch.object(gate, "api", side_effect=subprocess.CalledProcessError(1, "gh", stderr="HTTP 500")), \
                redirect_stdout(output):
            self.assertEqual(1, gate.main())
        self.assertIn("failing closed: HTTP 500", output.getvalue())

    def test_unlabelled_untouched_pr_never_reads_the_current_pr(self):
        with patch.dict(os.environ, dict(ENV, TOUCHED="false"), clear=True), \
                patch.object(gate, "api", side_effect=AssertionError("must not call")):
            self.assertTrue(gate.gate(arguments())[0])

    def test_evidence_lookup_uses_pr_head_not_merge_commit(self):
        with patch.dict(os.environ, dict(ENV, GITHUB_SHA="merge-commit", RUN_INTEGRATION="true"), clear=True), \
                patch.object(gate, "collection", return_value=[GREEN]) as collect, \
                patch.object(gate, "with_jobs", return_value=GREEN):
            self.assertTrue(gate.gate(arguments())[0])
        collect.assert_any_call(
            f"repos/Cratis/Chronicle/actions/workflows/requested-integration.yml/runs"
            f"?head_sha={SHA}&event=pull_request&per_page=100", "workflow_runs")

    SAME_RUN = {"REQUESTED": "success", "REQUESTED_PROVEN": "true", "REQUESTED_TOUCHED": "true",
                "REQUESTED_ALL_PROVIDERS": "true", "PR_NUMBER": "7", "GITHUB_RUN_ID": "555",
                "GITHUB_SERVER_URL": "https://github.com"}
    LABELLED = {"head": {"sha": SHA}, "labels": [{"name": "run-integration"}]}

    def same_run(self, pr=None, evidence=None, **overrides):
        env = dict(ENV, RUN_INTEGRATION="true", **{**self.SAME_RUN, **overrides})
        lookup = patch.object(gate, "evidence_runs", return_value=evidence or [])
        with patch.dict(os.environ, env, clear=True), patch.object(gate, "api", return_value=pr or self.LABELLED), \
                lookup:
            return gate.gate(arguments())

    def test_same_run_proof_turns_a_touched_gate_green_without_looking_up_runs(self):
        ok, message = self.same_run()
        self.assertTrue(ok)
        self.assertIn("https://github.com/Cratis/Chronicle/actions/runs/555", message)
        ok, _ = self.same_run(TOUCHED="false", REQUESTED_TOUCHED="false", REQUESTED_ALL_PROVIDERS="false")
        self.assertTrue(ok)

    def test_same_run_proof_accepts_a_reused_result_only_when_the_callee_reports_it(self):
        self.assertTrue(self.same_run(REQUESTED_PROVEN="skip", REQUESTED_ALL_PROVIDERS="")[0])

    def test_same_run_proof_is_never_a_verdict_unless_complete_and_every_gap_stays_red(self):
        for overrides in ({"REQUESTED": "failure"}, {"REQUESTED": "cancelled"}, {"REQUESTED": "skipped"},
                          {"REQUESTED": ""}, {"REQUESTED_PROVEN": ""}, {"REQUESTED_PROVEN": "false"},
                          {"REQUESTED_ALL_PROVIDERS": ""}, {"REQUESTED_ALL_PROVIDERS": "false"},
                          {"REQUESTED_TOUCHED": "true", "TOUCHED": "false", "REQUESTED_ALL_PROVIDERS": "false"},
                          {"GITHUB_RUN_ID": ""}):
            with self.subTest(overrides=overrides):
                ok, message = self.same_run(**overrides)
                self.assertFalse(ok)

    def test_same_run_proof_needs_the_current_label_and_head_never_the_payload(self):
        for pr in ({"head": {"sha": SHA}, "labels": []},
                   {"head": {"sha": "new-push"}, "labels": [{"name": "run-integration"}]}):
            with self.subTest(pr=pr):
                ok, message = self.same_run(pr=pr)
                self.assertFalse(ok)
                self.assertIn("Add the `run-integration` label", message)
        # Label removed but untouched stays green as before.
        self.assertTrue(self.same_run(pr={"head": {"sha": SHA}, "labels": []}, TOUCHED="false",
                                      REQUESTED_TOUCHED="false")[0])

    def test_same_run_proof_never_applies_to_forks_dispatch_or_failed_detection(self):
        for overrides in ({"FORK": "true"}, {"DETECT": "failure"}, {"DISPATCH": "true", "BUILD": "failure"}):
            with self.subTest(overrides=overrides):
                self.assertFalse(self.same_run(**overrides)[0])

    def test_same_run_label_api_error_fails_closed_in_cli(self):
        output = io.StringIO()
        env = dict(ENV, RUN_INTEGRATION="true", **self.SAME_RUN)
        with patch.dict(os.environ, env, clear=True), patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                patch.object(gate, "api", side_effect=subprocess.CalledProcessError(1, "gh", stderr="HTTP 403")), \
                redirect_stdout(output):
            self.assertEqual(1, gate.main())
        self.assertIn("failing closed: HTTP 403", output.getvalue())

    def test_api_error_fails_closed_in_cli(self):
        output = io.StringIO()
        with patch.dict(os.environ, ENV, clear=True), patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                patch.object(gate, "api", side_effect=subprocess.CalledProcessError(1, "gh", stderr="HTTP 403")), \
                redirect_stdout(output):
            self.assertEqual(1, gate.main())
        self.assertIn("failing closed: HTTP 403", output.getvalue())

    def test_only_successful_runs_load_latest_jobs(self):
        def runs(repo, workflow, head_sha):
            return [SKIPPED, GREEN] if workflow == gate.EVIDENCE_WORKFLOW else []
        with patch.object(gate, "workflow_runs", side_effect=runs), \
                patch.object(gate, "with_jobs", return_value=GREEN) as load:
            self.assertEqual([GREEN], gate.evidence_runs("Cratis/Chronicle", SHA, None))
        load.assert_called_once_with("Cratis/Chronicle", GREEN)

    def test_same_run_workflow_runs_load_jobs_when_completed_even_if_the_gate_failed(self):
        # The nested requested job can pass while the ordinary gate fails on specs; that run is still evidence.
        failed_gate_run = {"id": 90, "head_sha": SHA, "event": "pull_request", "status": "completed",
                           "conclusion": "failure"}
        running = dict(failed_gate_run, id=91, status="in_progress", conclusion=None)
        cancelled = dict(failed_gate_run, id=92, conclusion="cancelled")
        loaded = dict(failed_gate_run, jobs=[{"name": NESTED_JOB, "conclusion": "success"}])

        def runs(repo, workflow, head_sha):
            return [failed_gate_run, running, cancelled] if workflow == gate.GATE_WORKFLOW else []
        with patch.object(gate, "workflow_runs", side_effect=runs), \
                patch.object(gate, "with_jobs", return_value=loaded) as load:
            self.assertEqual([loaded], gate.evidence_runs("Cratis/Chronicle", SHA, None))
        load.assert_called_once_with("Cratis/Chronicle", failed_gate_run)

    def test_nested_requested_job_in_a_gate_run_is_evidence_but_a_run_without_it_is_not(self):
        same_run = dict(GREEN, jobs=[{"name": NESTED_JOB, "conclusion": "success"}])
        self.assertTrue(gate.has_green_evidence([same_run], SHA))
        for jobs in ([], [{"name": "hot-core-gate", "conclusion": "success"}],
                     [{"name": NESTED_JOB, "conclusion": "failure"}],
                     [{"name": NESTED_JOB, "conclusion": "skipped"}],
                     [{"name": "requested / hot-core-gate-requested-patch", "conclusion": "success"}],
                     [{"name": "xhot-core-gate-requested-run-integration", "conclusion": "success"}],
                     [{"name": "requested / hot-core-gate-requested-run-integration-x", "conclusion": "success"}]):
            with self.subTest(jobs=jobs):
                self.assertFalse(gate.has_green_evidence([dict(GREEN, jobs=jobs)], SHA))
        # The standalone requested workflow keeps its exact job name.
        self.assertTrue(gate.has_green_evidence([GREEN], SHA))

    def test_api_collection_keeps_all_pages(self):
        with patch.object(gate, "api", return_value=[{"jobs": [{"name": "other"}]}, {"jobs": GREEN["jobs"]}]):
            self.assertEqual(2, len(gate.collection("jobs", "jobs")))

    def test_jobs_are_listed_with_a_small_page_size(self):
        with patch.object(gate, "collection", return_value=[]) as collect:
            gate.with_jobs("Cratis/Chronicle", {"id": 7})
        collect.assert_called_once_with("repos/Cratis/Chronicle/actions/runs/7/jobs?filter=latest&per_page=30", "jobs")

    def test_api_retries_server_errors_then_succeeds(self):
        server_error = subprocess.CalledProcessError(1, "gh", stderr="gh: Server Error (HTTP 502)")
        ok = subprocess.CompletedProcess(["gh"], 0, stdout='{"ok": true}', stderr="")
        with patch.object(gate.subprocess, "run", side_effect=[server_error, server_error, ok]) as run, \
                patch.object(gate.time, "sleep") as sleep:
            self.assertEqual({"ok": True}, gate.api("anything"))
        self.assertEqual(3, run.call_count)
        self.assertEqual([((5,),), ((15,),)], sleep.call_args_list)

    def test_api_gives_up_after_bounded_server_errors(self):
        server_error = subprocess.CalledProcessError(1, "gh", stderr="gh: Server Error (HTTP 502)")
        with patch.object(gate.subprocess, "run", side_effect=[server_error] * 3) as run, \
                patch.object(gate.time, "sleep"), self.assertRaises(subprocess.CalledProcessError):
            gate.api("anything")
        self.assertEqual(3, run.call_count)

    def test_api_retries_timeouts(self):
        ok = subprocess.CompletedProcess(["gh"], 0, stdout="", stderr="")
        with patch.object(gate.subprocess, "run", side_effect=[subprocess.TimeoutExpired("gh", 30), ok]) as run, \
                patch.object(gate.time, "sleep"):
            self.assertIsNone(gate.api("anything"))
        self.assertEqual(2, run.call_count)

    def test_api_never_retries_client_errors(self):
        forbidden = subprocess.CalledProcessError(1, "gh", stderr="gh: Forbidden (HTTP 403)")
        with patch.object(gate.subprocess, "run", side_effect=[forbidden]) as run, \
                patch.object(gate.time, "sleep") as sleep, self.assertRaises(subprocess.CalledProcessError):
            gate.api("anything")
        self.assertEqual(1, run.call_count)
        sleep.assert_not_called()


class BuildPathDetection(unittest.TestCase):
    def test_repository_build_paths(self):
        root = SCRIPT.resolve().parents[2]
        for paths, expected in [(["Documentation/concepts/events.mdx"], False),
                                (["Source/Kernel/Core/Events.cs"], True),
                                ([".github/workflows/hot-core-gate.yml"], True), ([], False)]:
            with self.subTest(paths=paths):
                self.assertEqual(expected, gate.build_triggered(paths, root))

    def test_exclusions_and_reinclusions_reuse_the_contract_globs(self):
        work = SCRIPT.resolve().parents[2] / ".ai-work" / "required-gate"
        work.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=work) as temporary:
            workflow = Path(temporary) / ".github/workflows/dotnet-build.yml"
            workflow.parent.mkdir(parents=True)
            workflow.write_text('on:\n  pull_request:\n    paths:\n      - "Source/**"\n'
                                '      - "!Source/skip/**"\n      - "Source/skip/keep.cs"\n'
                                '    paths-ignore:\n      - "Source/ignored/**"\n', encoding="utf-8")
            for path, expected in [("Source/a.cs", True), ("Source/skip/a.cs", False),
                                   ("Source/skip/keep.cs", True), ("Source/ignored/a.cs", False)]:
                with self.subTest(path=path):
                    self.assertEqual(expected, gate.build_triggered([path], temporary))

    def test_detect_uses_three_dot_diff_and_dispatch_never_reads_git(self):
        with patch.dict(os.environ, {}, clear=True), patch.object(gate.subprocess, "run") as run, \
                patch.object(gate, "build_triggered", return_value=True) as match:
            self.assertEqual((True, "build-triggered=false"), gate.detect_build(argparse.Namespace(base="")))
            run.assert_not_called()
            match.assert_not_called()
            run.return_value = subprocess.CompletedProcess([], 0, stdout="Source/tést.cs\0Docs/old name.md\0")
            self.assertEqual((True, "build-triggered=true"),
                             gate.detect_build(argparse.Namespace(base="base", head="pr-head")))
            run.assert_called_once_with(["git", "diff", "--no-renames", "--name-only", "-z", "base...pr-head"],
                                        capture_output=True, text=True, check=True)
            match.assert_called_once_with(["Source/tést.cs", "Docs/old name.md"], ".")

    def test_detect_errors_fail_closed_without_emitting_an_output(self):
        for error in (subprocess.CalledProcessError(1, "git", stderr="bad diff"), ValueError("bad filter")):
            with self.subTest(error=error), patch.dict(os.environ, {}, clear=True), \
                    patch.object(sys, "argv", [str(SCRIPT), "detect-build", "--base", "base"]), \
                    patch.object(gate.subprocess, "run", side_effect=error), redirect_stdout(io.StringIO()) as output:
                self.assertEqual(1, gate.main())
                self.assertIn("failing closed", output.getvalue())
                self.assertNotIn("build-triggered=", output.getvalue())


class SpecsDecisions(unittest.TestCase):
    def evaluate(self, runs, **overrides):
        with patch.dict(os.environ, dict(ENV, TOUCHED="false", **overrides), clear=True), \
                patch.object(gate, "workflow_runs", return_value=runs) as lookup, \
                patch.object(gate, "with_jobs", side_effect=lambda repo, run: run), \
                patch.object(gate, "evidence_runs", return_value=[]):
            result = gate.gate(arguments())
        lookup.assert_called_once_with("Cratis/Chronicle", gate.BUILD_WORKFLOW, SHA)
        return result

    def test_not_triggered_and_no_run_keeps_both_hot_core_outcomes(self):
        self.assertTrue(self.evaluate([])[0])
        with patch.dict(os.environ, ENV, clear=True), patch.object(gate, "workflow_runs", return_value=[]), \
                patch.object(gate, "evidence_runs", return_value=[GREEN]):
            self.assertTrue(gate.gate(arguments())[0])

    def test_triggered_build_must_start(self):
        ok, message = self.evaluate([], BUILD_TRIGGERED="true")
        self.assertFalse(ok)
        self.assertIn("has not started", message)
        self.assertIn("title=Specs required", message)
        self.assertIn(SHA, message)

    def test_running_build_fails_without_loading_jobs(self):
        with patch.object(gate, "with_jobs", side_effect=AssertionError("must not fetch")):
            latest = dict(BUILD_GREEN, status="in_progress", conclusion=None)
            with patch.object(gate, "workflow_runs", return_value=[latest]):
                self.assertEqual(latest, gate.latest_build("Cratis/Chronicle", SHA, None))
        ok, message = self.evaluate([latest])
        self.assertFalse(ok)
        self.assertIn("still running", message)
        self.assertIn("turns green automatically when it passes", message)
        self.assertIn(BUILD_GREEN["html_url"], message)

    def test_completed_build_requires_exact_specs_success(self):
        for conclusion in ("failure", "cancelled", "skipped", None, "missing"):
            jobs = [] if conclusion == "missing" else [{"name": "specs", "conclusion": conclusion}]
            with self.subTest(conclusion=conclusion):
                ok, message = self.evaluate([dict(BUILD_GREEN, jobs=jobs)])
                self.assertFalse(ok)
                self.assertIn(f"specs conclusion: {conclusion}", message)
                self.assertIn(BUILD_GREEN["html_url"], message)
        self.assertFalse(self.evaluate([dict(BUILD_GREEN, jobs=[{"name": "not-specs", "conclusion": "success"}])])[0])
        self.assertTrue(self.evaluate([BUILD_GREEN])[0])
        self.assertTrue(self.evaluate([BUILD_GREEN], BUILD_TRIGGERED="true")[0])

    def test_latest_build_wins_and_other_heads_or_events_cannot_supply_evidence(self):
        failed = dict(BUILD_GREEN, id=71, jobs=[])
        other_head = dict(BUILD_GREEN, id=72, head_sha="new-push")
        dispatch = dict(BUILD_GREEN, id=73, event="workflow_dispatch")
        with patch.object(gate, "workflow_runs", return_value=[other_head, dispatch, BUILD_GREEN, failed]), \
                patch.object(gate, "with_jobs", return_value=failed) as load:
            self.assertEqual(failed, gate.latest_build("Cratis/Chronicle", SHA, None))
        load.assert_called_once_with("Cratis/Chronicle", failed)
        self.assertFalse(self.evaluate([other_head, dispatch], BUILD_TRIGGERED="true")[0])

    def test_specs_success_cannot_waive_hot_core_failure_and_both_messages_remain(self):
        with patch.dict(os.environ, ENV, clear=True), patch.object(gate, "latest_build", return_value=BUILD_GREEN), \
                patch.object(gate, "evidence_runs", return_value=[]), patch.object(gate, "latest_requested", return_value=None):
            ok, message = gate.gate(arguments())
        self.assertFalse(ok)
        self.assertIn("Hot core needs integration", message)
        self.assertIn("specs passed", message)

    def test_forks_require_specs_but_explain_manual_rerun(self):
        ok, message = self.evaluate([dict(BUILD_GREEN, status="in_progress")], FORK="true")
        self.assertFalse(ok)
        self.assertIn("Re-run this check", message)
        self.assertIn("fork PRs do not refresh automatically", message)
        self.assertTrue(self.evaluate([BUILD_GREEN], FORK="true")[0])

    def test_dispatch_and_failed_detection_never_query_build_runs(self):
        for overrides in ({"DISPATCH": "true", "BUILD": "success", "MATRIX": "success"}, {"DETECT": "failure"}):
            with self.subTest(overrides=overrides), patch.dict(os.environ, dict(ENV, **overrides), clear=True), \
                    patch.object(gate, "latest_build", side_effect=AssertionError("must not fetch")):
                ok, _ = gate.gate(arguments())
                self.assertEqual(overrides.get("DETECT") != "failure", ok)

    def test_recorded_inputs_through_the_cli_without_api_calls(self):
        recorded_checks()

    def test_build_api_and_parse_errors_fail_closed(self):
        for error in (subprocess.CalledProcessError(1, "gh", stderr="HTTP 403"), ValueError("invalid JSON")):
            with self.subTest(error=error), patch.dict(os.environ, dict(ENV, TOUCHED="false"), clear=True), \
                    patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                    patch.object(gate, "api", side_effect=error), redirect_stdout(io.StringIO()) as output:
                self.assertEqual(1, gate.main())
                self.assertIn("failing closed", output.getvalue())


class RefreshDecisions(unittest.TestCase):
    def test_rerun_target_uses_latest_completed_failed_gate_only(self):
        passed = dict(FAILED_GATE, id=43, jobs=[{"name": gate.GATE_JOB, "conclusion": "success"}])
        cases = [([], SHA, None), ([FAILED_GATE], "new-push", None), ([FAILED_GATE], SHA, 42),
                 ([FAILED_GATE, passed], SHA, None), ([passed, FAILED_GATE], SHA, None),
                 ([FAILED_GATE, dict(FAILED_GATE, id=44)], SHA, 44),
                 ([FAILED_GATE, dict(FAILED_GATE, id=44, status="in_progress")], SHA, None),
                 ([dict(FAILED_GATE, event="workflow_dispatch")], SHA, None),
                 ([dict(FAILED_GATE, jobs=[])], SHA, None)]
        for runs, current, expected in cases:
            with self.subTest(runs=runs, current=current):
                self.assertEqual(expected, gate.rerun_target(runs, SHA, current))

    def test_refresh_returns_without_polling_while_the_latest_gate_is_running(self):
        for status in ("queued", "in_progress"):
            with self.subTest(status=status), patch.dict(os.environ, ENV, clear=True), \
                    patch.object(gate, "current_pr", return_value={"head": {"sha": SHA}}), \
                    patch.object(gate, "evidence_runs", return_value=[GREEN]), \
                    patch.object(gate, "gate_runs", return_value=[dict(FAILED_GATE, status=status)]) as runs, \
                    patch.object(gate.time, "sleep") as sleep, patch.object(gate, "api") as api:
                ok, message = gate.refresh(arguments())
                self.assertTrue(ok)
                self.assertIn("still running", message)
                runs.assert_called_once()
                sleep.assert_not_called()
                api.assert_not_called()

    def test_refresh_rechecks_head_and_never_posts_stale(self):
        with patch.dict(os.environ, ENV, clear=True), \
                patch.object(gate, "current_pr", side_effect=[{"head": {"sha": SHA}}, {"head": {"sha": "new"}}]), \
                patch.object(gate, "evidence_runs", return_value=[GREEN]), \
                patch.object(gate, "gate_runs", return_value=[FAILED_GATE]), \
                patch.object(gate.time, "sleep") as sleep, patch.object(gate, "api") as api:
            self.assertIn("Nothing to do", gate.refresh(arguments())[1])
        sleep.assert_not_called()
        api.assert_not_called()

    def test_refresh_with_no_gate_run_posts_nothing(self):
        with patch.dict(os.environ, ENV, clear=True), \
                patch.object(gate, "current_pr", return_value={"head": {"sha": SHA}}), \
                patch.object(gate, "evidence_runs", return_value=[GREEN]), \
                patch.object(gate, "gate_runs", return_value=[]), patch.object(gate, "api") as api:
            self.assertTrue(gate.refresh(arguments())[0])
        api.assert_not_called()

    def test_refresh_posts_only_for_green_evidence_and_failed_gate(self):
        for evidence, expected_calls in [([GREEN], 1), ([SKIPPED], 0)]:
            with self.subTest(evidence=evidence), patch.dict(os.environ, ENV, clear=True), \
                    patch.object(gate, "current_pr", return_value={"head": {"sha": SHA}}), \
                    patch.object(gate, "evidence_runs", return_value=evidence), \
                    patch.object(gate, "gate_runs", return_value=[FAILED_GATE]), patch.object(gate, "api") as api:
                args = arguments()
                args.dry_run = False
                self.assertTrue(gate.refresh(args)[0])
                self.assertEqual(expected_calls, api.call_count)
                if expected_calls:
                    api.assert_called_once_with("repos/Cratis/Chronicle/actions/runs/42/rerun-failed-jobs", method="POST")

    def test_build_source_refresh_requires_latest_green_specs(self):
        for latest, expected_calls in [(BUILD_GREEN, 1), (None, 0),
                                       (dict(BUILD_GREEN, jobs=[]), 0),
                                       (dict(BUILD_GREEN, status="in_progress"), 0)]:
            with self.subTest(latest=latest), \
                    patch.dict(os.environ, dict(ENV, SOURCE_WORKFLOW=".github/workflows/dotnet-build.yml"), clear=True), \
                    patch.object(gate, "current_pr", return_value={"head": {"sha": SHA}}), \
                    patch.object(gate, "latest_build", return_value=latest) as build, \
                    patch.object(gate, "evidence_runs", side_effect=AssertionError("wrong workflow")), \
                    patch.object(gate, "gate_runs", return_value=[FAILED_GATE]), patch.object(gate, "api") as api:
                args = arguments()
                args.dry_run = False
                self.assertTrue(gate.refresh(args)[0])
                build.assert_called_once_with("Cratis/Chronicle", SHA, None)
                self.assertEqual(expected_calls, api.call_count)
                if expected_calls:
                    api.assert_called_once_with("repos/Cratis/Chronicle/actions/runs/42/rerun-failed-jobs", method="POST")

    def test_build_refresh_never_reruns_a_moved_head(self):
        for heads in (["new"], [SHA, "new"]):
            with self.subTest(heads=heads), \
                    patch.dict(os.environ, dict(ENV, SOURCE_WORKFLOW=".github/workflows/dotnet-build.yml"), clear=True), \
                    patch.object(gate, "current_pr", side_effect=[{"head": {"sha": sha}} for sha in heads]), \
                    patch.object(gate, "latest_build", return_value=BUILD_GREEN), \
                    patch.object(gate, "gate_runs", return_value=[FAILED_GATE]), patch.object(gate, "api") as api:
                self.assertIn("Nothing to do", gate.refresh(arguments())[1])
                api.assert_not_called()

    def test_empty_payload_pr_number_falls_back_to_commit_pulls(self):
        pr = {"state": "open", "head": {"sha": SHA, "repo": {"full_name": "Cratis/Chronicle"}}}
        with patch.dict(os.environ, ENV, clear=True), patch.object(gate, "api", return_value=[pr]) as api:
            self.assertEqual(pr, gate.current_pr("Cratis/Chronicle", SHA, None))
        api.assert_called_once_with(f"repos/Cratis/Chronicle/commits/{SHA}/pulls")


def recorded_checks():
    """Feed serialized fixtures from this file through the real CLI, without GitHub calls."""
    work = SCRIPT.resolve().parents[2] / ".ai-work" / "5287"
    work.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=work) as temporary:
        paths = {}
        for name, data in {"green": [GREEN], "skipped": [SKIPPED], "gate": [FAILED_GATE],
                           "build": [BUILD_GREEN], "no-build": [],
                           "current": {"head": {"sha": SHA}}, "stale": {"head": {"sha": "new-push"}}}.items():
            paths[name] = str(Path(temporary) / (name + ".json"))
            Path(paths[name]).write_text(json.dumps(data), encoding="utf-8")
        cases = [("green", ["gate", "--runs-json", paths["green"]], {}, 0, SHA),
                 ("skipped-label", ["gate", "--runs-json", paths["skipped"]], {"HEAD_SHA": SKIPPED_SHA}, 1, "run-integration"),
                 ("new-push", ["gate", "--runs-json", paths["green"]], {"HEAD_SHA": "new-push"}, 1,
                  "Add the `run-integration` label"),
                 ("labeled-new-push", ["gate", "--runs-json", paths["green"]],
                  {"HEAD_SHA": "new-push", "RUN_INTEGRATION": "true"}, 1, "automatically on each push")]
        for name in ("current", "stale"):
            cases.append(("refresh-" + name, ["refresh", "--dry-run", "--runs-json", paths["green"],
                                             "--gate-runs-json", paths["gate"], "--pr-json", paths[name]],
                          {}, 0, "run 42" if name == "current" else "Nothing to do"))
        cases.extend([
            ("specs-green", ["gate", "--runs-json", paths["green"], "--build-runs-json", paths["build"]],
             {"BUILD_TRIGGERED": "true"}, 0, "specs passed"),
            ("specs-not-started", ["gate", "--runs-json", paths["green"], "--build-runs-json", paths["no-build"]],
             {"BUILD_TRIGGERED": "true"}, 1, "has not started"),
            ("refresh-build", ["refresh", "--dry-run", "--build-runs-json", paths["build"],
                               "--gate-runs-json", paths["gate"], "--pr-json", paths["current"]],
             {"SOURCE_WORKFLOW": ".github/workflows/dotnet-build.yml"}, 0, "run 42")])
        for name, args, overrides, expected, text in cases:
            if args[0] == "gate" and "--build-runs-json" not in args:
                args.extend(["--build-runs-json", paths["no-build"]])
            result = subprocess.run([sys.executable, str(SCRIPT), *args], env=dict(ENV, **overrides),
                                    capture_output=True, text=True, timeout=10)
            print(f"{name}: exit {result.returncode}; {result.stdout.strip()}")
            if result.returncode != expected or text not in result.stdout:
                raise AssertionError(f"{name}: expected exit {expected} containing {text!r}; {result.stderr}")


if __name__ == "__main__":
    if sys.argv[1:] == ["--recorded-checks"]:
        recorded_checks()
    else:
        unittest.main()
