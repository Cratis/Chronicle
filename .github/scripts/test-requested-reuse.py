#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Fail-closed specs for requested matrix reuse."""
import importlib.util
import pathlib
import subprocess
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location("requested_reuse", pathlib.Path(__file__).with_name("requested_reuse.py"))
reuse = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(reuse)
HEAD = "a" * 40
OLD = "b" * 40
BASE = "c" * 40


def current(**changes):
    return dict(repo="o/r", head_repo="o/r", pull_request=7, head=HEAD, base=BASE,
                event="pull_request", **changes)


def source():
    return dict(id=123, head_sha=OLD, event="pull_request", conclusion="success",
                path=".github/workflows/hot-core-gate.yml", head_repository={"full_name": "o/r"},
                display_title=f"Hot Core Gate - PR #7 @ {OLD} base {BASE}", has_matrix_job=True)


def jobs(prefix="requested / "):
    return [dict(name=prefix + "hot-core-gate-requested-run-integration", conclusion="success"),
            dict(name=prefix + "requested-integration-run-integration / integration", conclusion="success"),
            dict(name=prefix + "requested-integration-run-integration / neo4j (${{ matrix.lane }})", conclusion="skipped")]


class EligibilitySpecs(unittest.TestCase):
    def test_accepts_recorded_tested_base_without_mutable_pull_requests_metadata(self):
        self.assertIsNone(reuse.source_run_refusal(source(), current()))

    def test_missing_fields_are_not_evidence(self):
        for field in source():
            candidate = source()
            del candidate[field]
            with self.subTest(field=field):
                self.assertIsNotNone(reuse.source_run_refusal(candidate, current()))

    def test_refuses_wrong_source_or_missing_immutable_base(self):
        changes = [dict(event="push"), dict(conclusion="failure"), dict(head_sha=HEAD),
                   dict(head_repository={"full_name": "fork/r"}), dict(path="other.yml"),
                   dict(has_matrix_job=False), dict(display_title="Hot Core Gate"),
                   dict(display_title=f"Hot Core Gate - PR #8 @ {OLD} base {BASE}"),
                   dict(display_title=f"Hot Core Gate - PR #7 @ {OLD} base {'d' * 40}"),
                   dict(display_title=f"Hot Core Gate - PR #7 @ {HEAD} base {BASE}"),
                   dict(display_title=f"Hot Core Gate - PR #7 @ {OLD} base {BASE}\nextra")]
        for change in changes:
            with self.subTest(change=change):
                self.assertIsNotNone(reuse.source_run_refusal(source() | change, current()))

    def test_refuses_non_pr_unknown_base_and_current_fork(self):
        for change in [dict(event="push"), dict(event=""), dict(base=""), dict(head_repo="fork/r"), dict(head_repo="")]:
            with self.subTest(change=change):
                self.assertIsNotNone(reuse.source_run_refusal(source(), current() | change))

    def test_legacy_run_requires_the_same_strict_recorded_metadata(self):
        candidate = source() | dict(path=".github/workflows/requested-integration.yml")
        self.assertIsNotNone(reuse.source_run_refusal(candidate, current()))
        candidate["display_title"] = f"Requested Integration - PR #7 @ {OLD} base {BASE}"
        self.assertIsNone(reuse.source_run_refusal(candidate, current()))


class MatrixSpecs(unittest.TestCase):
    def test_real_nested_and_standalone_names_prove_executed_lanes(self):
        for prefix in ("", "requested / "):
            self.assertTrue(reuse.matrix_executed(jobs(prefix)))

    def test_skipped_matrix_cannot_chain(self):
        self.assertFalse(reuse.matrix_executed([jobs()[0]]))
        self.assertFalse(reuse.matrix_executed([jobs()[0], jobs()[2]]))
        self.assertFalse(reuse.matrix_executed([]))

    def test_bad_gate_or_lane_or_unknown_status_is_not_proof(self):
        for index in (0, 1):
            for status in ("failure", "cancelled", "skipped", None):
                candidate = jobs()
                candidate[index]["conclusion"] = status
                with self.subTest(index=index, status=status):
                    self.assertFalse(reuse.matrix_executed(candidate))
        self.assertFalse(reuse.matrix_executed(jobs() + [dict(name="requested / requested-integration-run-integration / specs", conclusion="failure")]))

    def test_non_lane_success_does_not_prove_the_matrix_ran(self):
        candidate = jobs()
        candidate[1]["name"] = "requested / requested-integration-run-integration / dotnet-build"
        self.assertFalse(reuse.matrix_executed(candidate))


class DecisionSpecs(unittest.TestCase):
    def decide(self, **changes):
        options = dict(fingerprint=lambda sha: "fingerprint", files_between=lambda a, b: ["Documentation/a.md"],
                       classify_delta=lambda files: "skip", ancestor=lambda a, b: True)
        options.update(changes)
        return reuse.reuse_decision(source(), current(), reuse.ReuseEvidence(**options))

    def test_reuses_only_proven_non_reaching_delta(self):
        result = self.decide()
        self.assertEqual("reused", result["state"])
        self.assertEqual((123, OLD, "fingerprint"), (result["run_id"], result["head_sha"], result["fingerprint"]))

    def test_unknown_file_reaching_file_or_unreadable_delta_refuses(self):
        self.assertEqual("refused", self.decide(classify_delta=lambda files: "full")["state"])
        self.assertEqual("refused", self.decide(classify_delta=lambda files: "selected")["state"])
        self.assertEqual("refused", self.decide(files_between=lambda a, b: None)["state"])

    def test_unreadable_changed_config_and_non_ancestor_refuse(self):
        for fingerprint in (lambda sha: None, lambda sha: sha):
            self.assertEqual("refused", self.decide(fingerprint=fingerprint)["state"])
        self.assertEqual("refused", self.decide(ancestor=lambda a, b: False)["state"])


class FingerprintSpecs(unittest.TestCase):
    def test_each_config_family_changes_the_fingerprint(self):
        paths = [".github/workflows/hot-core-gate.yml", ".github/scripts/select-integration.py",
                 "Directory.Packages.props", "global.json", ".editorconfig", ".globalconfig", "Chronicle.slnx",
                 "specs.runsettings", "Source/Directory.Build.props", "Integration/Directory.Build.targets",
                 "Samples/Chaos/Directory.Build.props", "Integration/Directory.Build.props", "Source/Clients/DotNET.CodeAnalysis/.editorconfig",
                 "Source/a.props", "Source/a.targets", "Source/NuGet.Config", "nuget.config", "a/other.globalconfig",
                 "Source/Workbench/yarn.lock", "Source/Kernel/Server/yarn.lock", "a/pnpm-lock.yaml", "a/package.json",
                 ".github/scripts/docs-verification/package-lock.json", "a/package-lock.json"]
        for path in paths:
            with self.subTest(path=path):
                runner = lambda *args: (0, f"100644 blob {args[-1]}\t{path}\0", "")
                self.assertNotEqual(reuse.config_fingerprint(OLD, runner), reuse.config_fingerprint(HEAD, runner))

    def test_non_config_content_is_not_fingerprinted(self):
        runner = lambda *args: (0, "100644 blob same\tglobal.json\0" + f"100644 blob {args[-1]}\tDocumentation/a.md\0", "")
        self.assertEqual(reuse.config_fingerprint(OLD, runner), reuse.config_fingerprint(HEAD, runner))

    def test_git_error_and_empty_config_refuse(self):
        self.assertIsNone(reuse.config_fingerprint(HEAD, lambda *args: (1, "", "bad commit")))
        self.assertIsNone(reuse.config_fingerprint(HEAD, lambda *args: (0, "", "")))

    def test_real_git_tree_objects_include_config_but_not_docs(self):
        with tempfile.TemporaryDirectory() as directory:
            def git(*args):
                result = subprocess.run(args, cwd=directory, capture_output=True, text=True, check=False)
                return result.returncode, result.stdout, result.stderr
            git("git", "init", "-q")
            root = pathlib.Path(directory)
            (root / "global.json").write_text("one")
            git("git", "add", ".")
            tree1 = git("git", "write-tree")[1].strip()
            (root / "note.md").write_text("docs")
            git("git", "add", ".")
            tree2 = git("git", "write-tree")[1].strip()
            self.assertEqual(reuse.config_fingerprint(tree1, git), reuse.config_fingerprint(tree2, git))
            (root / "global.json").write_text("two")
            git("git", "add", ".")
            tree3 = git("git", "write-tree")[1].strip()
            self.assertNotEqual(reuse.config_fingerprint(tree1, git), reuse.config_fingerprint(tree3, git))


if __name__ == "__main__":
    unittest.main()
