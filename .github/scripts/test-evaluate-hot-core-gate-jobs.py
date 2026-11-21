# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise requested job evidence, including copied jobs and incomplete rerun attempts."""

import argparse
from contextlib import redirect_stdout
import importlib.util
import io
import os
from pathlib import Path
import subprocess
import sys
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("evaluate-hot-core-gate.py")
spec = importlib.util.spec_from_file_location("evaluate_hot_core_gate", SCRIPT)
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)

SHA = "synthetic-head"
GREEN = {"id": 555, "head_sha": SHA, "event": "pull_request", "status": "completed", "conclusion": "success",
         "jobs": [{"id": 1, "name": gate.EVIDENCE_JOB, "status": "completed", "conclusion": "success"}]}
ENV = {"DETECT": "success", "TOUCHED": "true", "DISPATCH": "false", "FORK": "false",
       "REPO": "Cratis/Chronicle", "HEAD_SHA": SHA, "BUILD": "skipped", "MATRIX": "skipped",
       "RUN_INTEGRATION": "false"}
SAME_RUN = dict(ENV, RUN_INTEGRATION="true", REQUESTED="success", REQUESTED_PROVEN="true",
                REQUESTED_TOUCHED="true", REQUESTED_ALL_PROVIDERS="true", PR_NUMBER="7", GITHUB_RUN_ID="555")
LABELLED = {"head": {"sha": SHA}, "labels": [{"name": "run-integration"}]}
NESTED_AGGREGATE = dict(GREEN["jobs"][0], name="requested / " + gate.EVIDENCE_JOB)


def arguments():
    return argparse.Namespace(runs_json=None, build_runs_json=None, gate_runs_json=None, pr_json=None, dry_run=True)


def same_run(jobs):
    with patch.dict(os.environ, SAME_RUN, clear=True), patch.object(gate, "api", return_value=LABELLED), \
            patch.object(gate, "with_jobs", return_value={"jobs": jobs}), \
            patch.object(gate, "evidence_runs", return_value=[]), \
            patch.object(gate, "latest_requested", return_value=None), \
            patch.object(gate, "latest_build", return_value=None):
        return gate.gate(arguments())


class JobEvidence(unittest.TestCase):
    def test_latest_jobs_keep_copied_results_and_passing_rerun(self):
        copied = {"id": 2, "name": "integration", "status": "completed", "conclusion": "success"}
        rerun = dict(copied, id=3)
        jobs = GREEN["jobs"] + [copied, rerun]
        with patch.object(gate, "api", return_value=[{"jobs": jobs}]) as api:
            latest = gate.with_jobs("Cratis/Chronicle", dict(GREEN, run_attempt=2))
        api.assert_called_once_with(
            f"repos/Cratis/Chronicle/actions/runs/{GREEN['id']}/jobs?filter=latest&per_page=30", paginate=True)
        self.assertTrue(gate.has_green_evidence([latest], SHA))
        for conclusion in ("skipped", "neutral"):
            with self.subTest(conclusion=conclusion):
                self.assertTrue(gate.has_green_evidence(
                    [dict(GREEN, jobs=jobs + [dict(copied, id=4, conclusion=conclusion)])], SHA))

    def test_duplicate_display_names_do_not_hide_a_failing_job_id(self):
        failed = {"id": 2, "name": "integration", "status": "completed", "conclusion": "failure"}
        passed = dict(failed, id=3, conclusion="success")
        for jobs in ([failed, passed], [passed, failed]):
            with self.subTest(jobs=jobs):
                self.assertFalse(gate.has_green_evidence([dict(GREEN, jobs=GREEN["jobs"] + jobs)], SHA))

    def test_missing_job_list_or_job_fields_never_supply_evidence(self):
        for run in ({key: value for key, value in GREEN.items() if key != "jobs"}, dict(GREEN, jobs=[]),
                    dict(GREEN, jobs=[{"name": gate.EVIDENCE_JOB, "conclusion": "success"}])):
            with self.subTest(run=run), patch.dict(os.environ, ENV, clear=True), \
                    patch.object(gate, "evidence_runs", return_value=[run]), \
                    patch.object(gate, "latest_requested", return_value=None), \
                    patch.object(gate, "latest_build", return_value=None):
                self.assertFalse(gate.gate(arguments())[0])


class PendingEvidence(unittest.TestCase):
    def test_in_flight_inspects_every_job_id_including_queued_rerun_copies(self):
        completed = {"id": 2, "name": "integration", "status": "completed", "conclusion": "success"}
        self.assertFalse(gate.in_flight(dict(GREEN, status="completed")))
        self.assertFalse(gate.in_flight([completed]))
        for status in ("queued", "in_progress", "waiting", "pending", None):
            with self.subTest(status=status):
                self.assertTrue(gate.in_flight(dict(GREEN, status=status)))
                self.assertTrue(gate.in_flight([completed, dict(completed, id=3, status=status)]))

    def test_missing_run_status_is_pending_and_never_green_evidence(self):
        run = {key: value for key, value in GREEN.items() if key != "status"}
        self.assertTrue(gate.in_flight(run))
        self.assertFalse(gate.has_green_evidence([run], SHA))

    def test_missing_job_status_is_pending_and_never_green_evidence(self):
        job = {key: value for key, value in GREEN["jobs"][0].items() if key != "status"}
        run = dict(GREEN, jobs=[job])
        self.assertTrue(gate.in_flight(run))
        self.assertFalse(gate.has_green_evidence([run], SHA))

    def test_same_run_nonfinal_result_is_pending_without_polling(self):
        for status in ("queued", "in_progress", "waiting"):
            with self.subTest(status=status), patch.dict(os.environ, dict(SAME_RUN, REQUESTED=status), clear=True), \
                    patch.object(gate, "evidence_runs", return_value=[]), \
                    patch.object(gate, "latest_requested", return_value=None), \
                    patch.object(gate.time, "sleep") as sleep:
                ok, message = gate.gate(arguments())
                self.assertIsNone(ok)
                self.assertIn("::notice title=Waiting for Requested Integration::", message)
                self.assertNotIn("::error", message)
                sleep.assert_not_called()

    def test_same_run_queued_job_is_pending_not_an_error(self):
        queued = {"id": 3, "name": "requested / integration", "status": "queued", "conclusion": None}
        ok, message = same_run([NESTED_AGGREGATE, queued])
        self.assertIsNone(ok)
        self.assertIn("::notice", message)
        self.assertNotIn("::error", message)

    def test_newest_requested_run_or_job_overrides_older_green_evidence(self):
        for latest in (dict(GREEN, id=99, status="in_progress"),
                       dict(GREEN, id=99, status="completed", jobs=GREEN["jobs"] + [
                           {"id": 3, "name": "integration", "status": "queued", "conclusion": None}])):
            with self.subTest(latest=latest), patch.dict(os.environ, ENV, clear=True), \
                    patch.object(gate, "evidence_runs", return_value=[GREEN]), \
                    patch.object(gate, "latest_requested", return_value=latest), \
                    patch.object(gate.time, "sleep") as sleep:
                ok, message = gate.gate(arguments())
                self.assertIsNone(ok)
                self.assertIn("::notice", message)
                self.assertNotIn("::error", message)
                sleep.assert_not_called()

    def test_pending_cli_exits_nonzero_with_notice(self):
        output = io.StringIO()
        with patch.dict(os.environ, ENV, clear=True), patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                patch.object(gate, "evidence_runs", return_value=[]), \
                patch.object(gate, "latest_requested", return_value=dict(GREEN, status="queued")), \
                redirect_stdout(output):
            self.assertEqual(1, gate.main())
        self.assertIn("::notice", output.getvalue())
        self.assertNotIn("::error", output.getvalue())


class CompletedGateEvidence(unittest.TestCase):
    def test_completed_run_with_unfinished_or_failing_job_never_passes(self):
        for status, conclusion in (("queued", None), ("in_progress", "success"), ("waiting", None),
                                   ("completed", "failure"), ("completed", "cancelled"),
                                   ("completed", "timed_out"), ("completed", None)):
            job = {"id": 2, "name": "integration", "status": status, "conclusion": conclusion}
            run = dict(GREEN, jobs=GREEN["jobs"] + [job])
            with self.subTest(status=status, conclusion=conclusion), patch.dict(os.environ, ENV, clear=True), \
                    patch.object(gate, "evidence_runs", return_value=[run]), \
                    patch.object(gate, "latest_requested", return_value=None), \
                    patch.object(gate, "latest_build", return_value=None):
                self.assertFalse(gate.gate(arguments())[0])

    def test_job_list_api_error_or_missing_list_fails_closed_in_cli(self):
        for response in (subprocess.CalledProcessError(1, "gh", stderr="HTTP 403"), [{}]):
            output = io.StringIO()
            with self.subTest(response=response), patch.dict(os.environ, ENV, clear=True), \
                    patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                    patch.object(gate, "workflow_runs", return_value=[GREEN]), \
                    patch.object(gate, "api", side_effect=[response]), redirect_stdout(output):
                self.assertEqual(1, gate.main())
            self.assertIn("failing closed", output.getvalue())


class SameRunEvidence(unittest.TestCase):
    def test_same_run_success_outputs_never_hide_queued_or_failing_requested_jobs(self):
        for status, conclusion in (("queued", None), ("completed", "failure")):
            job = {"id": 2, "name": "requested / integration", "status": status, "conclusion": conclusion}
            with self.subTest(status=status):
                self.assertFalse(same_run([NESTED_AGGREGATE, job])[0])
        self.assertFalse(same_run([])[0])

    def test_nested_requested_proof_does_not_require_unrelated_gate_jobs_to_pass(self):
        unrelated = {"id": 2, "name": gate.GATE_JOB, "status": "in_progress", "conclusion": None}
        for status, conclusion in (("in_progress", None), ("completed", "failure")):
            jobs = [NESTED_AGGREGATE, dict(unrelated, status=status, conclusion=conclusion)]
            with self.subTest(status=status):
                self.assertTrue(same_run(jobs)[0])
                self.assertTrue(gate.has_green_evidence([dict(GREEN, jobs=jobs)], SHA))

    def test_same_run_job_lookup_uses_the_current_run_and_fails_closed_on_api_error(self):
        output = io.StringIO()
        with patch.dict(os.environ, SAME_RUN, clear=True), patch.object(sys, "argv", [str(SCRIPT), "gate"]), \
                patch.object(gate, "api", return_value=LABELLED), \
                patch.object(gate, "with_jobs", side_effect=subprocess.CalledProcessError(
                    1, "gh", stderr="HTTP 403")) as jobs, redirect_stdout(output):
            self.assertEqual(1, gate.main())
        jobs.assert_called_once_with("Cratis/Chronicle", {"id": "555"})
        self.assertIn("failing closed: HTTP 403", output.getvalue())


if __name__ == "__main__":
    unittest.main()
