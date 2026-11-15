# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Specifications for the integration selection: when a push runs everything, some lanes, or nothing."""
import importlib.util
import os
import subprocess
import tempfile
from pathlib import Path
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("select_integration", Path(__file__).with_name("select-integration.py"))
select = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(select)
ENTRIES = select.load_entries(str(ROOT / select.CONTRACT))


def classify(*files):
    return select.classify(files, ENTRIES)


class ContractSpecs(unittest.TestCase):
    def test_every_named_client_namespace_exists_so_a_typo_cannot_select_nothing(self):
        directories = {entry.name for entry in (ROOT / "Integration" / "Client").iterdir() if entry.is_dir()}
        named = {name for entry in ENTRIES for name in entry.get("client", []) if "{ns}" not in name}
        self.assertEqual(set(), named - directories)

    def test_every_non_wildcard_directory_prefix_exists(self):
        for entry in ENTRIES:
            for pattern in entry["paths"]:
                prefix = pattern.split("*")[0].split("{")[0].rstrip("/")
                if prefix and "." not in Path(prefix).name:
                    self.assertTrue((ROOT / prefix).exists(), f"{pattern} names a directory that does not exist")

    def test_the_hot_core_is_never_ignorable(self):
        contract = (ROOT / ".github" / "hot-core-paths.txt").read_text(encoding="utf-8").splitlines()
        patterns = [line.strip() for line in contract if line.strip() and not line.startswith("#")]
        for pattern in patterns:
            sample = pattern.replace("**", "x/y").replace("*", "z")
            mode, _, _ = classify(sample)
            self.assertNotEqual("skip", mode, f"hot-core path {sample} would be ignored")

    def test_an_entry_that_selects_nothing_is_rejected(self):
        with self.assertRaises(SystemExit):
            path = ROOT / ".ai-work-select-test.json"
            path.write_text('{"entries":[{"paths":["a/**"],"action":"select"}]}', encoding="utf-8")
            try:
                select.load_entries(str(path))
            finally:
                path.unlink()


class ClassificationSpecs(unittest.TestCase):
    def test_documentation_and_workbench_changes_need_no_integration(self):
        mode, selection, _ = classify("Documentation/a/b.mdx", "README.md", "Source/Workbench/Features/X/y.tsx")
        self.assertEqual(("skip", None), (mode, selection))

    def test_specification_projects_need_no_integration(self):
        mode, _, _ = classify("Source/Kernel/Core.Specs/Observation/a.cs", "Source/Clients/DotNET.Specs/b.cs", "Source/Infrastructure.Specs/c.cs")
        self.assertEqual("skip", mode)

    def test_the_storage_contract_specs_run_inside_integration_wrappers_so_they_force_everything(self):
        mode, _, _ = classify("Source/Kernel/Storage.Specs/EventSequences/a.cs")
        self.assertEqual("full", mode)

    def test_an_integration_spec_change_selects_its_own_namespace(self):
        mode, selection, _ = classify("Integration/Client/for_Reducers/when_x/a.cs", "Integration/Client/Projections/b.cs")
        self.assertEqual("selected", mode)
        self.assertEqual(["Projections", "for_Reducers"], selection["client"])
        self.assertFalse(selection["kernel"] or selection["sql"])

    def test_a_shared_integration_fixture_forces_everything(self):
        for path in ("Integration/Client/ChronicleCollection.cs", "Integration/Directory.Build.props", "Integration/Kernel/Kernel.csproj"):
            self.assertEqual("full", classify(path)[0], path)

    def test_a_kernel_integration_lane_has_no_mapping_so_it_forces_everything(self):
        self.assertEqual("full", classify("Integration/Kernel/Jobs/a.cs")[0])

    def test_a_client_area_change_selects_its_lanes(self):
        mode, selection, _ = classify("Source/Clients/DotNET/Reducers/a.cs")
        self.assertEqual("selected", mode)
        self.assertEqual(["for_ReadModels", "for_Reducers"], selection["client"])

    def test_one_unmapped_path_forces_everything_even_beside_mapped_ones(self):
        mode, selection, reasons = classify("Source/Clients/DotNET/Reducers/a.cs", "Source/Kernel/Server/Program.cs")
        self.assertEqual(("full", None), (mode, selection))
        self.assertTrue(any("Server/Program.cs" in reason for reason in reasons))

    def test_workflow_and_selection_changes_force_everything(self):
        for path in (".github/workflows/integration.yml", ".github/integration-selection.json", ".github/scripts/select-integration.py"):
            self.assertEqual("full", classify(path)[0], path)

    def test_ignorable_files_do_not_narrow_a_selection_into_a_skip(self):
        mode, selection, _ = classify("Documentation/a.md", "Integration/Client/for_Webhooks/a.cs")
        self.assertEqual(("selected", ["for_Webhooks"]), (mode, selection["client"]))


class DecisionSpecs(unittest.TestCase):
    @staticmethod
    def files(mapping):
        return lambda base, head: mapping.get((base, head))

    def decide(self, mapping, **kwargs):
        defaults = dict(entries=ENTRIES, base="B", head="H", touched=False, green=None)
        defaults.update(kwargs)
        return select.decide(files_between=self.files(mapping), **defaults)

    def test_an_unreadable_diff_runs_everything(self):
        self.assertEqual("full", self.decide({})[0])

    def test_a_hot_core_change_runs_everything_even_when_the_files_look_ignorable(self):
        mode, _, _ = self.decide({("B", "H"): ["Documentation/a.md"]}, touched=True)
        self.assertEqual("full", mode)

    def test_only_ignorable_files_since_the_last_green_run_skips_the_matrix(self):
        mapping = {("G", "H"): ["Documentation/a.md", "Source/Kernel/Core.Specs/a.cs"], ("B", "H"): ["Source/Kernel/Core/Observation/a.cs"]}
        mode, _, reasons = self.decide(mapping, touched=True, green=dict(head_sha="G", run_id=123, fingerprint="config"))
        self.assertEqual("skip", mode)
        for evidence in ("Reused", "123", "G", "config"):
            self.assertIn(evidence, reasons[0])

    def test_a_relevant_file_since_the_last_green_run_does_not_skip(self):
        mapping = {("G", "H"): ["Documentation/a.md", "Source/Kernel/Core/a.cs"], ("B", "H"): ["Source/Kernel/Core/a.cs"]}
        mode, _, _ = self.decide(mapping, touched=True, green=dict(head_sha="G", run_id=123, fingerprint="config"))
        self.assertEqual("full", mode)

    def test_an_unreadable_diff_since_the_last_green_run_does_not_skip(self):
        mode, _, _ = self.decide({("B", "H"): ["Documentation/a.md", "Integration/Client/for_Webhooks/a.cs"]}, green=dict(head_sha="G", run_id=123, fingerprint="config"))
        self.assertEqual("selected", mode)

    def test_a_whole_pull_request_of_ignorable_files_skips_without_any_earlier_run(self):
        self.assertEqual("skip", self.decide({("B", "H"): ["Documentation/a.md"]})[0])

    def test_an_ignorable_pull_request_does_not_report_an_unproven_green_run_as_reused(self):
        mapping = {("G", "H"): ["Source/Kernel/Core/a.cs"], ("B", "H"): ["Documentation/a.md"]}
        green = dict(head_sha="G", run_id=123, fingerprint="config")
        mode, _, reasons = self.decide(mapping, green=green)
        self.assertEqual("skip", mode)
        self.assertNotIn("Reused", reasons[0])
        self.assertIsNone(select.proven_reuse(green, "H", ENTRIES, self.files(mapping)))


class RenameSpecs(unittest.TestCase):
    def test_rename_from_exercised_source_into_docs_keeps_the_deleted_source_path(self):
        with tempfile.TemporaryDirectory() as directory:
            def run(*args):
                result = subprocess.run(args, cwd=directory, capture_output=True, text=True, check=False)
                return result.returncode, result.stdout, result.stderr
            root = Path(directory)
            run("git", "init", "-q")
            source = root / "Source/Clients/DotNET/Reducers/a.cs"
            source.parent.mkdir(parents=True)
            source.write_text("identical contents for git rename detection")
            run("git", "add", ".")
            old = run("git", "write-tree")[1].strip()
            docs = root / "Documentation/a.md"
            docs.parent.mkdir()
            source.rename(docs)
            run("git", "add", "-A")
            head = run("git", "write-tree")[1].strip()
            with patch.object(select, "run", side_effect=run):
                files = select.changed_files(old, head)
            self.assertIn("Source/Clients/DotNET/Reducers/a.cs", files)
            self.assertNotEqual("skip", select.classify(files, ENTRIES)[0])


class LastGreenSpecs(unittest.TestCase):
    HEAD, OLD, BASE = "a" * 40, "b" * 40, "c" * 40

    def candidate(self, **changes):
        return dict(id=123, head_sha=self.OLD, event="pull_request", conclusion="success",
                    path=".github/workflows/hot-core-gate.yml", head_repository={"full_name": "o/r"},
                    display_title=f"Hot Core Gate - PR #7 @ {self.OLD} base {self.BASE}") | changes

    def green(self, *, candidates=None, executed=True, fingerprint="config", ancestor=True):
        current = dict(repo="o/r", head_repo="o/r", pull_request="7", head=self.HEAD, base=self.BASE, event="pull_request")
        runs = [self.candidate()] if candidates is None else candidates
        with patch.object(select, "candidate_runs", side_effect=[[], runs]), \
             patch.object(select, "nested_pass", return_value=executed), \
             patch.object(select, "config_fingerprint", return_value=fingerprint), \
             patch.object(select, "changed_files", return_value=["Documentation/a.md"]), \
             patch.object(select, "is_ancestor", return_value=ancestor):
            return select.last_green_run(current, ENTRIES)

    def test_gate_nested_executed_pass_is_evidence(self):
        self.assertEqual(self.OLD, self.green()["head_sha"])

    def test_standalone_requires_metadata_and_execution_too(self):
        candidate = self.candidate(path=".github/workflows/requested-integration.yml",
                                   display_title=f"Requested Integration - PR #7 @ {self.OLD} base {self.BASE}")
        self.assertEqual(self.OLD, self.green(candidates=[candidate])["head_sha"])

    def test_gate_overall_green_without_matrix_is_not_evidence(self):
        self.assertIsNone(self.green(executed=False))

    def test_other_pr_current_head_and_legacy_unknown_base_are_not_evidence(self):
        for changes in [dict(head_sha=self.HEAD), dict(display_title="Hot Core Gate"),
                        dict(display_title=f"Hot Core Gate - PR #8 @ {self.OLD} base {self.BASE}")]:
            self.assertIsNone(self.green(candidates=[self.candidate(**changes)]))

    def test_non_ancestor_and_unknown_config_are_not_evidence(self):
        self.assertIsNone(self.green(ancestor=False))
        self.assertIsNone(self.green(fingerprint=None))

    def test_no_candidates_are_no_evidence(self):
        self.assertIsNone(self.green(candidates=[]))

    def test_api_errors_and_malformed_json_add_no_evidence(self):
        for response in [(1, "", "rate limited"), (0, "not JSON", ""), (0, "[{}]", "")]:
            with patch.object(select, "run", return_value=response):
                self.assertIsNone(select.candidate_runs("o/r", "hot-core-gate.yml"))
                self.assertFalse(select.nested_pass("o/r", 123))

    def test_non_pr_event_does_not_query_candidates(self):
        with patch.object(select, "candidate_runs") as query:
            self.assertIsNone(select.last_green_run(dict(event="push"), ENTRIES))
            query.assert_not_called()


if __name__ == "__main__":
    unittest.main()
