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

SCRIPT = Path(__file__).with_name("evaluate-hot-core-gate.py")
spec = importlib.util.spec_from_file_location("evaluate_hot_core_gate", SCRIPT)
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)

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
       "REPO": "Cratis/Chronicle", "HEAD_SHA": SHA, "BUILD": "skipped", "MATRIX": "skipped"}


def arguments(**overrides):
    return argparse.Namespace(runs_json=None, gate_runs_json=None, pr_json=None, dry_run=True, **overrides)


class GateDecisions(unittest.TestCase):
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

    def test_no_evidence_fails_with_request_and_sha(self):
        with patch.dict(os.environ, ENV, clear=True), patch.object(gate, "evidence_runs", return_value=[]):
            ok, message = gate.gate(arguments())
        self.assertFalse(ok)
        self.assertIn("run-integration", message)
        self.assertIn(SHA, message)

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
                 ("new-push", ["gate", "--runs-json", paths["green"]], {"HEAD_SHA": "new-push"}, 1, "new-push")]
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
