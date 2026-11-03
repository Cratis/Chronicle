# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Offline endpoint-shaped admission fixtures; unexpected endpoints are always errors."""
import copy
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import stat
from unittest.mock import patch
import tempfile
import unittest
import zipfile

SPEC = importlib.util.spec_from_file_location("feed", Path(__file__).with_name("nightly-coverage-feed.py"))
feed = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(feed)
NOW = 1793546946000
SHA = "a" * 40
REPORT = json.dumps({"coverage": {"assemblies": [{"name": "Cratis.Chronicle", "coverage": 72.5}]}}).encode()


class FakeGitHub:
    def __init__(self):
        self.repo = {"id": 11, "full_name": "Cratis/Chronicle", "default_branch": "main"}
        self.workflow = {"id": 22, "path": ".github/workflows/dotnet-build.yml", "state": "active"}
        self.run = {"id": 33, "run_attempt": 2, "workflow_id": 22,
                    "path": ".github/workflows/dotnet-build.yml", "event": "schedule", "head_branch": "main",
                    "head_sha": SHA, "status": "completed", "conclusion": "success",
                    "created_at": "2026-11-01T14:00:00Z", "updated_at": "2026-11-01T15:30:00Z",
                    "repository": {"id": 11}, "head_repository": {"id": 11}}
        self.artifact = {"id": 44, "name": "coverage-nightly-v1-33-2", "size_in_bytes": 1,
                         "digest": "", "expired": False, "created_at": "2026-11-01T15:30:00Z",
                         "expires_at": "2026-11-29T15:30:00Z", "workflow_run": {
                             "id": 33, "repository_id": 11, "head_repository_id": 11,
                             "head_branch": "main", "head_sha": SHA}}
        self.runs = [self.run]
        self.artifacts = [self.artifact]
        self.calls = []
        self.changes = {}
        self.repack()

    def repack(self, report=REPORT, manifest=None, members=None):
        digest = hashlib.sha256(report).hexdigest()
        provenance = {"schemaVersion": 1, "repository": "Cratis/Chronicle", "repositoryId": 11,
                      "workflowPath": ".github/workflows/dotnet-build.yml", "workflowId": 22,
                      "event": "schedule", "ref": "refs/heads/main", "runId": 33, "runAttempt": 2,
                      "headSha": SHA, "measuredAt": NOW, "summarySha256": digest,
                      "scope": "dotnet-build/coverage-merge-and-cache-v1"}
        provenance.update(manifest or {})
        files = {"Summary.json": report, "SHA256SUMS": f"{digest}  Summary.json\n".encode(),
                 "provenance.json": json.dumps(provenance).encode()}
        if members is not None:
            files = members
        stream = io.BytesIO()
        with zipfile.ZipFile(stream, "w", zipfile.ZIP_DEFLATED) as archive:
            for name, data in files.items():
                archive.writestr(name, data)
        self.zip = stream.getvalue()
        self.artifact.update(size_in_bytes=len(self.zip), digest="sha256:" + hashlib.sha256(self.zip).hexdigest())

    def json(self, path):
        self.calls.append(path)
        values = {
            feed.REPO: self.repo, feed.WORKFLOW: self.workflow,
            feed.RUNS: {"total_count": len(self.runs), "workflow_runs": self.runs},
            feed.REPO + "/actions/runs/33": self.run,
            feed.REPO + "/actions/runs/33/artifacts?per_page=100": {"total_count": len(self.artifacts), "artifacts": self.artifacts},
            feed.REPO + "/actions/artifacts/44": self.artifact,
        }
        if path in self.changes:
            return self.changes[path](copy.deepcopy(values[path]), self.calls.count(path))
        if path not in values:
            raise AssertionError("Unexpected endpoint: " + path)
        return copy.deepcopy(values[path])

    def archive(self, path):
        self.calls.append(path)
        if path != feed.REPO + "/actions/artifacts/44/zip":
            raise AssertionError(path)
        return self.zip


class Admission(unittest.TestCase):
    def setUp(self):
        scratch = Path(__file__).resolve().parents[2] / ".ai-work"
        scratch.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=scratch)
        self.addCleanup(self.temp.cleanup)
        self.output = Path(self.temp.name) / "feed"
        self.api = FakeGitHub()

    def consume(self, observer=None):
        return feed.consume(self.api, self.output, NOW, observer if observer is not None else {
            "runId": 99, "runAttempt": 1, "executionSha": "b" * 40, "requestedVersion": "19.116.0"})

    def test_valid_without_release_or_tag_evidence(self):
        self.assertEqual(0, self.consume())
        result = json.loads((self.output / "result.json").read_text())
        self.assertEqual("measured", result["state"])
        self.assertEqual(SHA, result["source"]["headSha"])
        self.assertEqual(NOW, result["source"]["measuredAt"])
        self.assertNotIn("version", result["source"])
        self.assertEqual("unverified", result["observer"]["releaseContext"])
        self.assertEqual(REPORT, (self.output / "Summary.json").read_bytes())
        self.assertFalse(any("releases" in call or "tags" in call for call in self.api.calls))

    def test_equal_sha_never_infers_measured_version_and_bad_observer_does_not_reject(self):
        for observer in ({"executionSha": SHA, "requestedVersion": "missing-release"}, {"runId": False}, {}):
            self.assertEqual(0, self.consume(observer))
            result = json.loads((self.output / "result.json").read_text())
            self.assertNotIn("version", result["source"])
            self.assertEqual("unverified", result["observer"]["releaseContext"])

    def test_untrusted_runs_are_filtered_before_archive_access(self):
        changes = {"event": "pull_request", "head_branch": "other", "workflow_id": 9,
                   "path": "other.yml", "head_sha": "bad", "run_attempt": True,
                   "status": "in_progress", "conclusion": "failure", "repository": {"id": 9},
                   "head_repository": {"id": 9}}
        for key, value in changes.items():
            with self.subTest(key=key):
                self.api = FakeGitHub()
                self.api.run[key] = value
                self.assertEqual(1, self.consume())
                self.assertFalse(any(call.endswith("/zip") for call in self.api.calls))

    def test_canonical_repository_and_workflow_required_fields(self):
        for obj, key, value in (("repo", "default_branch", "other"), ("repo", "full_name", "fork/Chronicle"),
                                ("repo", "id", True), ("workflow", "state", "disabled_manually"),
                                ("workflow", "path", "other.yml"), ("workflow", "id", None)):
            self.api = FakeGitHub()
            getattr(self.api, obj)[key] = value
            self.assertEqual(1, self.consume())

    def test_selected_invalid_artifact_never_falls_back(self):
        for key, value in {"expired": True, "digest": None, "id": False, "size_in_bytes": 17 * 1024**2,
                           "workflow_run": {"id": 33}, "expires_at": "2026-10-01T00:00:00Z"}.items():
            with self.subTest(key=key):
                self.api = FakeGitHub()
                self.api.artifact[key] = value
                self.assertEqual(1, self.consume())
                self.assertNotIn("source", json.loads((self.output / "result.json").read_text()))
        self.api = FakeGitHub()
        self.api.artifacts.append(copy.deepcopy(self.api.artifact))
        self.assertEqual(1, self.consume())

    def test_empty_candidates_missing_and_skipped_newer_run_continues(self):
        self.api.runs = []
        self.assertEqual(1, self.consume())
        self.assertEqual("missing", json.loads((self.output / "result.json").read_text())["state"])
        self.api = FakeGitHub()
        newer = dict(self.api.run, id=34, created_at="2026-11-01T15:00:00Z")
        self.api.runs = [self.api.run, newer]
        original = self.api.json
        self.api.json = lambda path: ({"total_count": 0, "artifacts": []}
                                    if path.endswith("/34/artifacts?per_page=100") else original(path))
        self.assertEqual(0, self.consume())
        self.assertLess(self.api.calls.index(feed.REPO + "/actions/runs/33/artifacts?per_page=100"),
                        self.api.calls.index(feed.REPO + "/actions/artifacts/44/zip"))

    def test_rechecks_reject_changed_run_or_artifact(self):
        for endpoint, field, value in (("runs/33", "run_attempt", 3), ("runs/33", "head_sha", "c" * 40),
                                       ("runs/33", "status", "in_progress"), ("artifacts/44", "digest", "sha256:" + "0" * 64)):
            self.api = FakeGitHub()
            path = feed.REPO + "/actions/" + endpoint
            self.api.changes[path] = lambda obj, count, f=field, v=value: dict(obj, **{f: v}) if count > 1 else obj
            self.assertEqual(1, self.consume())
            self.assertFalse((self.output / "Summary.json").exists())

    def test_manifest_schema_identity_digest_and_time_violations(self):
        changes = {"schemaVersion": 2, "repositoryId": 12, "workflowId": 23, "workflowPath": "wrong",
                   "event": "workflow_dispatch", "ref": "main", "runId": 34, "runAttempt": 1,
                   "headSha": "b" * 40, "summarySha256": "0" * 64, "scope": "wrong",
                   "measuredAt": NOW + 600000}
        for key, value in changes.items():
            with self.subTest(key=key):
                self.api = FakeGitHub()
                self.api.repack(manifest={key: value})
                self.assertEqual(1, self.consume())

    def test_zip_report_and_checksum_violations(self):
        for members in ({"../Summary.json": REPORT}, {"Summary.json": REPORT},
                        {"Summary.json": REPORT, "SHA256SUMS": b"wrong", "provenance.json": b"{}"},
                        {"Summary.json": REPORT, "SHA256SUMS": b"", "provenance.json": b"{}", "tool.py": b"exit()"}):
            self.api = FakeGitHub()
            self.api.repack(members=members)
            self.assertEqual(1, self.consume())
        for report in (b"{}", b"[]", b'{"coverage":{"assemblies":[{"name":"A.Specs","coverage":20}]}}',
                       b'{"coverage":{"assemblies":[{"name":"A","coverage":NaN}]}}',
                       b'{"coverage":{"assemblies":[{"name":"A","coverage":101}]}}'):
            self.api = FakeGitHub()
            self.api.repack(report=report)
            self.assertEqual(1, self.consume())
        self.api = FakeGitHub()
        self.api.zip += b"tampered"
        self.assertEqual(1, self.consume())

    def test_stale_handoff_removed_before_unavailable_and_bounds_are_not_empty_success(self):
        self.assertEqual(0, self.consume())
        def unavailable(path):
            raise feed.Unavailable("API unavailable")
        self.api.json = unavailable
        self.assertEqual(2, self.consume())
        self.assertFalse((self.output / "Summary.json").exists())
        self.assertEqual("incomplete", json.loads((self.output / "result.json").read_text())["state"])
        self.api = FakeGitHub()
        self.api.artifacts *= 101
        self.assertEqual(1, self.consume())

    def test_packaging_uses_own_api_identity_and_exact_bytes(self):
        report = Path(self.temp.name) / "Summary.json"
        report.write_bytes(REPORT)
        target = Path(self.temp.name) / "producer"
        feed.produce(self.api, report, target, NOW, 33, 2, SHA)
        self.assertEqual({"Summary.json", "SHA256SUMS", "provenance.json"}, {p.name for p in target.iterdir()})
        self.assertEqual(REPORT, (target / "Summary.json").read_bytes())
        self.assertEqual(NOW, json.loads((target / "provenance.json").read_text())["measuredAt"])

    def test_no_fallback_after_a_selected_payload_fails(self):
        older = dict(self.api.run, id=32, created_at='2026-10-31T14:00:00Z')
        self.api.runs.append(older)
        self.api.repack(manifest={'runAttempt': 1})
        self.assertEqual(1, self.consume())
        self.assertFalse(any('/runs/32' in call for call in self.api.calls))
        self.assertFalse((self.output / 'Summary.json').exists())

    def test_zip_duplicate_link_encryption_and_decompression_bounds(self):
        cases = []
        duplicate = io.BytesIO()
        with zipfile.ZipFile(duplicate, 'w') as archive:
            archive.writestr('Summary.json', REPORT)
            archive.writestr('Summary.json', REPORT)
            archive.writestr('provenance.json', '{}')
        cases.append(duplicate.getvalue())
        link = io.BytesIO()
        with zipfile.ZipFile(link, 'w') as archive:
            info = zipfile.ZipInfo('Summary.json')
            info.create_system = 3
            info.external_attr = (stat.S_IFLNK | 0o777) << 16
            archive.writestr(info, 'target')
            archive.writestr('SHA256SUMS', '')
            archive.writestr('provenance.json', '{}')
        cases.append(link.getvalue())
        self.api.repack()
        encrypted = bytearray(self.api.zip)
        # Set the encrypted flag in each central-directory entry, without extracting.
        index = encrypted.find(b'PK\x01\x02')
        encrypted[index + 8] |= 1
        cases.append(bytes(encrypted))
        oversized = io.BytesIO()
        with zipfile.ZipFile(oversized, 'w', zipfile.ZIP_DEFLATED) as archive:
            archive.writestr('Summary.json', b' ' * (feed.SUMMARY_LIMIT + 1))
            archive.writestr('SHA256SUMS', '')
            archive.writestr('provenance.json', '{}')
        cases.extend((oversized.getvalue(), b'not a zip'))
        for payload in cases:
            with self.subTest(size=len(payload)):
                self.api = FakeGitHub()
                self.api.zip = payload
                self.api.artifact.update(size_in_bytes=len(payload), digest='sha256:' + hashlib.sha256(payload).hexdigest())
                self.assertEqual(1, self.consume())
                self.assertFalse((self.output / 'Summary.json').exists())

    def test_listing_bounds_stale_runs_and_required_metadata(self):
        self.api.runs *= 31
        self.assertEqual(1, self.consume())
        self.api = FakeGitHub()
        self.api.run['created_at'] = '2026-09-01T00:00:00Z'
        self.assertEqual(1, self.consume())
        self.assertFalse(any(call.endswith('/zip') for call in self.api.calls))
        for field in ('run_attempt', 'repository', 'updated_at', 'workflow_id'):
            self.api = FakeGitHub()
            del self.api.run[field]
            self.assertEqual(1, self.consume())
        self.api = FakeGitHub()
        self.api.artifact['workflow_run']['head_sha'] = 'c' * 40
        self.assertEqual(1, self.consume())

    def test_cli_entrypoint_has_injected_api_clock_and_no_publication_lookup(self):
        with patch.dict('os.environ', {'GITHUB_RUN_ID': '99', 'GITHUB_RUN_ATTEMPT': '1',
                                      'GITHUB_SHA': SHA, 'COVERAGE_REQUESTED_VERSION': 'not-published'}):
            self.assertEqual(0, feed.main(['consume', '--output', str(self.output)], self.api, lambda: NOW))
        self.assertFalse(any('release' in call or '/tags' in call for call in self.api.calls))

    def test_producer_rejects_identity_and_bad_report_without_creating_feed(self):
        report = Path(self.temp.name) / 'Summary.json'
        report.write_bytes(REPORT)
        for attempt, sha in ((1, SHA), (2, 'c' * 40)):
            with self.assertRaises(feed.Rejected):
                feed.produce(self.api, report, self.output, NOW, 33, attempt, sha)
            self.assertFalse(self.output.exists())
        report.write_text('{}')
        with self.assertRaises(feed.Rejected):
            feed.produce(self.api, report, self.output, NOW, 33, 2, SHA)
        self.assertFalse(self.output.exists())

    def test_transport_subprocess_is_bounded_and_nonzero_is_unavailable(self):
        for script, limit, timeout in (("process.stdout.write('x'.repeat(10000))", 64, 5),
                                       ("setInterval(() => {}, 1000)", 64, 0.05),
                                       ("process.exit(4)", 64, 5)):
            with self.assertRaises(feed.Unavailable):
                feed.bounded_command(['node', '-e', script], limit, timeout)
        self.assertEqual(b'{}', feed.bounded_command(['node', '-e', "process.stdout.write('{}')"], 64, 5))

    def test_adapter_uses_fixed_get_paths_and_reports_transport_errors(self):
        calls = []
        def runner(command, limit, timeout):
            calls.append((command, limit, timeout))
            return b'{"id":11}'
        api = feed.GitHub(runner)
        self.assertEqual({"id": 11}, api.json(feed.REPO))
        self.assertEqual(["gh", "api", "--method", "GET", feed.REPO], calls[0][0])
        self.assertEqual(60, calls[0][2])
        for payload in (b"not json", b"{} {}", b"[]"):
            with self.assertRaises(feed.Unavailable):
                feed.GitHub(lambda *args: payload).json(feed.REPO)
        with self.assertRaises(feed.Unavailable):
            feed.GitHub(lambda *args: b' ' * (feed.JSON_LIMIT + 1)).json(feed.REPO)
        for error in (subprocess.TimeoutExpired("gh", 60), OSError("not installed")):
            with self.assertRaises(feed.Unavailable):
                feed.GitHub(lambda *args, e=error: (_ for _ in ()).throw(e)).json(feed.REPO)


if __name__ == "__main__":
    unittest.main()
