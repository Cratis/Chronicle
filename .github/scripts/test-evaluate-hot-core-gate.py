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
ENV = {"DETECT": "success", "TOUCHED": "true", "DISPATCH": "false", "FORK": "false",
       "REPO": "Cratis/Chronicle", "HEAD_SHA": SHA, "BUILD": "skipped", "MATRIX": "skipped",
       "RUN_INTEGRATION": "false"}


def arguments(**overrides):
    return argparse.Namespace(runs_json=None, gate_runs_json=None, pr_json=None, dry_run=True, **overrides)


class GateDecisions(unittest.TestCase):
    def setUp(self):
        latest = patch.object(gate, "latest_requested", return_value=None)
        self.latest = latest.start()
        self.addCleanup(latest.stop)

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
        collect.assert_called_once_with(
            f"repos/Cratis/Chronicle/actions/workflows/requested-integration.yml/runs"
            f"?head_sha={SHA}&event=pull_request&per_page=100", "workflow_runs")

    def test_api_error_fails_closed_in_cli(self):
        output = io.StringIO()
        with patch.dict(os.environ, ENV, clear=True), patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                patch.object(gate, "api", side_effect=subprocess.CalledProcessError(1, "gh", stderr="HTTP 403")), \
                redirect_stdout(output):
            self.assertEqual(1, gate.main())
        self.assertIn("failing closed: HTTP 403", output.getvalue())

    def test_only_successful_runs_load_latest_jobs(self):
        with patch.object(gate, "workflow_runs", return_value=[SKIPPED, GREEN]), \
                patch.object(gate, "with_jobs", return_value=GREEN) as load:
            self.assertEqual([GREEN], gate.evidence_runs("Cratis/Chronicle", SHA, None))
        load.assert_called_once_with("Cratis/Chronicle", GREEN)

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

    def test_refresh_rechecks_head_after_polling_and_never_posts_stale(self):
        with patch.dict(os.environ, ENV, clear=True), \
                patch.object(gate, "current_pr", side_effect=[{"head": {"sha": SHA}}, {"head": {"sha": "new"}}]), \
                patch.object(gate, "evidence_runs", return_value=[GREEN]), \
                patch.object(gate, "gate_runs", side_effect=[[dict(FAILED_GATE, status="in_progress")], [FAILED_GATE]]), \
                patch.object(gate.time, "sleep") as sleep, patch.object(gate, "api") as api:
            self.assertIn("Nothing to do", gate.refresh(arguments())[1])
        sleep.assert_called_once_with(30)
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
        for name, args, overrides, expected, text in cases:
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
