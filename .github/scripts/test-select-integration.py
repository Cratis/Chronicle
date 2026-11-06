# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Specifications for the integration selection: when a push runs everything, some lanes, or nothing."""
import importlib.util
import os
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
        defaults = dict(entries=ENTRIES, base="B", head="H", touched=False, green_head=None)
        defaults.update(kwargs)
        return select.decide(files_between=self.files(mapping), **defaults)

    def test_an_unreadable_diff_runs_everything(self):
        self.assertEqual("full", self.decide({})[0])

    def test_a_hot_core_change_runs_everything_even_when_the_files_look_ignorable(self):
        mode, _, _ = self.decide({("B", "H"): ["Documentation/a.md"]}, touched=True)
        self.assertEqual("full", mode)

    def test_only_ignorable_files_since_the_last_green_run_skips_the_matrix(self):
        mapping = {("G", "H"): ["Documentation/a.md", "Source/Kernel/Core.Specs/a.cs"], ("B", "H"): ["Source/Kernel/Core/Observation/a.cs"]}
        mode, _, reasons = self.decide(mapping, touched=True, green_head="G")
        self.assertEqual("skip", mode)
        self.assertIn("G", reasons[0])

    def test_a_relevant_file_since_the_last_green_run_does_not_skip(self):
        mapping = {("G", "H"): ["Documentation/a.md", "Source/Kernel/Core/a.cs"], ("B", "H"): ["Source/Kernel/Core/a.cs"]}
        mode, _, _ = self.decide(mapping, touched=True, green_head="G")
        self.assertEqual("full", mode)

    def test_an_unreadable_diff_since_the_last_green_run_does_not_skip(self):
        mode, _, _ = self.decide({("B", "H"): ["Documentation/a.md", "Integration/Client/for_Webhooks/a.cs"]}, green_head="G")
        self.assertEqual("selected", mode)

    def test_a_whole_pull_request_of_ignorable_files_skips_without_any_earlier_run(self):
        self.assertEqual("skip", self.decide({("B", "H"): ["Documentation/a.md"]})[0])


class LastGreenSpecs(unittest.TestCase):
    RUNS = "\n".join(["aaa\t2026-01-01T00:00:00Z\t7", "bbb\t2026-01-02T00:00:00Z\t7", "ccc\t2026-01-03T00:00:00Z\t8", "hhh\t2026-01-04T00:00:00Z\t7"])

    def fake_run(self, ancestors, present=("aaa", "bbb", "ccc")):
        def run(*arguments):
            if arguments[0] == "gh":
                return 0, self.RUNS, ""
            if arguments[1] == "cat-file":
                return (0 if arguments[3].split("^")[0] in present else 1), "", ""
            return (0 if arguments[3] in ancestors else 1), "", ""
        return run

    def test_takes_the_newest_green_head_of_this_pull_request_that_is_an_ancestor(self):
        with patch.object(select, "run", self.fake_run(ancestors={"aaa", "bbb"})):
            self.assertEqual("bbb", select.last_green_head("o/r", 7, "hhh"))

    def test_ignores_other_pull_requests_and_the_current_head(self):
        with patch.object(select, "run", self.fake_run(ancestors={"ccc", "hhh"})):
            self.assertIsNone(select.last_green_head("o/r", 7, "hhh"))

    def test_a_force_pushed_head_that_is_no_ancestor_is_not_reused(self):
        with patch.object(select, "run", self.fake_run(ancestors=set())):
            self.assertIsNone(select.last_green_head("o/r", 7, "hhh"))

    def test_a_head_missing_from_the_clone_is_not_reused(self):
        with patch.object(select, "run", self.fake_run(ancestors={"aaa", "bbb"}, present=())):
            self.assertIsNone(select.last_green_head("o/r", 7, "hhh"))

    def test_an_api_failure_reuses_nothing(self):
        with patch.object(select, "run", lambda *a: (1, "", "boom")):
            self.assertIsNone(select.last_green_head("o/r", 7, "hhh"))


if __name__ == "__main__":
    unittest.main()
