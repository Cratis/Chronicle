# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Recorded benchmark shapes and launcher-only checks; never runs dotnet."""

import argparse
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("paired-benchmark.py")
spec = importlib.util.spec_from_file_location("paired_benchmark", SCRIPT)
bench = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bench)
ROOT = SCRIPT.resolve().parents[2]


def setUpModule():
    (ROOT / ".ai-work").mkdir(exist_ok=True)


def rebuild(rate=100, activations=100):
    return {
        "Settings": {"Scenario": "S2", "Mode": "live", "Count": 10000, "Topology": "split-jobs"},
        "Verdict": "passed", "Differences": [],
        "Series": [{"Name": "Rebuild/S2/mongodb/split-jobs/live/events-per-second", "Value": rate},
                   {"Name": "Rebuild/S2/mongodb/split-jobs/live/request-to-swap-seconds", "Value": 2}],
        "StartupFenceCoverageSeconds": 3, "PeakWorkingSetBytes": 500,
        "AllocatedBytes": 1000, "ServedList": {"P99Ms": 10}, "KernelFindByKey": {"P99Ms": 20},
        "Environment": {"Silos": [{"Silo": "Silo_0", "Observers": True},
                                    {"Silo": "Silo_1", "Observers": False}]},
        "GrainActivations": [
            {"Silo": "Silo_0", "ActivationsByGrainType": {"Cratis.ProjectionObserverSubscriber": activations}},
            {"Silo": "Silo_1", "ActivationsByGrainType": {"Cratis.ProjectionObserverSubscriber": 999}}]}


def replay():
    return {"Workload": {"Events": 50000, "Roots": 2000}, "ReplaySeconds": 10,
            "EventsPerSecond": 5000, "MongoCommandsPerEvent": 2, "GrainCallsPerEvent": 3,
            "Before": {"Count": 2000, "Hash": "abc"}, "After": {"Count": 2000, "Hash": "abc"},
            "MongoFindCommandsPerEventByTag": {"find-by-key": 1}}


class Planning(unittest.TestCase):
    def test_alternating_pairs(self):
        self.assertEqual(bench.order(3), ["before", "after", "after", "before", "before", "after"])
        self.assertEqual(bench.order(1), ["before", "after"])

    def test_matrix(self):
        cells = bench.matrix("rebuild", "S2,S3", "two,split-jobs")
        self.assertEqual([cell["cell"] for cell in cells],
                         ["rebuild-S2-two", "rebuild-S2-split-jobs", "rebuild-S3-two", "rebuild-S3-split-jobs"])
        self.assertEqual(bench.matrix("replay-shape", "S5", "foo"),
                         [{"cell": "replay-shape", "scenario": "", "topology": ""}])

    def test_invalid_matrix(self):
        for scenarios, topologies, message in [("S5", "two", "scenario"), ("S2", "foo", "topology"),
                                               ("", "two", "scenario"), ("S1,S2,S3", "single,two,three", "8 cells")]:
            with self.subTest(scenarios=scenarios, topologies=topologies), self.assertRaisesRegex(ValueError, message):
                bench.matrix("rebuild", scenarios, topologies)

    def test_invalid_dispatch(self):
        for repetitions, before, after, message in [(0, "a", "b", "1-10"), (11, "a", "b", "1-10"),
                                                    (1, " ", "b", "before"), (1, "a", "", "after")]:
            with self.subTest(repetitions=repetitions, before=before, after=after):
                with self.assertRaisesRegex(ValueError, message):
                    bench.validate_inputs(repetitions, before, after)

    def test_last_object(self):
        self.assertEqual(bench.extract_record('log\n{"a": 1}\n[]\n{"b": 2}\nteardown'), {"b": 2})
        self.assertIsNone(bench.extract_record('log\n[]\ninvalid {'))

    def test_classifications(self):
        cases = [("rebuild", 0, rebuild(), "", False, "passed"),
                 ("rebuild", 0, {"Verdict": "failed", "Differences": ["hash"]}, "", False, "mismatch"),
                 ("rebuild", 0, {"Verdict": "failed", "Error": "broken"}, "", False, "failed"),
                 ("rebuild", 2, rebuild(), "", False, "failed"),
                 ("rebuild", 0, None, "", False, "no-record"),
                 ("rebuild", -9, None, "", True, "timed-out"),
                 ("rebuild", 1, None, "split-jobs needs the cluster Jobs role", False, "product-lacks-jobs-role"),
                 ("rebuild", 0, {"Error": "split-jobs needs the cluster Jobs role"}, "", False, "product-lacks-jobs-role"),
                 ("replay-shape", 0, replay(), "", False, "passed"),
                 ("replay-shape", 0, {"Before": 1, "After": 2}, "", False, "mismatch"),
                 ("replay-shape", 0, {}, "", False, "failed")]
        for *inputs, expected in cases:
            with self.subTest(expected=expected, inputs=inputs):
                self.assertEqual(bench.classify(*inputs), expected)

    def test_harness_flags(self):
        with tempfile.TemporaryDirectory(dir=ROOT / ".ai-work") as temp:
            program = Path(temp) / bench.PROJECT / "Program.cs"
            program.parent.mkdir(parents=True)
            program.write_text('args.Contains("--replay-shape")')
            self.assertFalse(bench.harness_supports(Path(temp), "rebuild"))
            self.assertTrue(bench.harness_supports(Path(temp), "replay-shape"))


class Comparison(unittest.TestCase):
    def test_medians_delta_direction_and_noise(self):
        rows = bench.compare_records("rebuild", [rebuild(100), rebuild(120), rebuild(110)],
                                     [rebuild(110), rebuild(121), rebuild(120)])
        row = rows["events-per-second"]
        self.assertEqual(row["before"]["median"], 110)
        self.assertEqual(row["after"]["median"], 120)
        self.assertAlmostEqual(row["deltaPercent"], 100 / 11)
        self.assertEqual(row["direction"], "better")
        self.assertTrue(row["withinNoise"])
        self.assertAlmostEqual(row["before"]["spreadPercent"], 200 / 11)

    def test_missing_metric_and_no_spread(self):
        after = rebuild()
        del after["AllocatedBytes"]
        rows = bench.compare_records("rebuild", [rebuild()], [after])
        self.assertIsNone(rows["AllocatedBytes"]["after"])
        self.assertIsNone(rows["AllocatedBytes"]["deltaPercent"])
        self.assertIsNone(rows["events-per-second"]["before"]["spreadPercent"])

    def test_lower_latency_is_better_and_higher_is_worse(self):
        after = rebuild(90)
        after["ServedList"]["P99Ms"] = 5
        rows = bench.compare_records("rebuild", [rebuild()], [after])
        self.assertEqual(rows["ServedList.P99Ms"]["direction"], "better")
        self.assertEqual(rows["events-per-second"]["direction"], "worse")

    def test_settings_must_match_including_within_a_side(self):
        after = rebuild()
        after["Settings"]["Mode"] = "cold"
        with self.assertRaisesRegex(ValueError, "Settings differ"):
            bench.compare_records("rebuild", [rebuild()], [after])
        with self.assertRaisesRegex(ValueError, "Settings differ"):
            bench.compare_records("rebuild", [rebuild(), after], [rebuild()])

    def test_dynamic_replay_metrics_and_workload(self):
        after = replay()
        after["MongoFindCommandsPerEventByTag"]["new-tag"] = 2
        rows = bench.compare_records("replay-shape", [replay()], [after])
        self.assertIsNone(rows["MongoFindCommandsPerEventByTag.new-tag"]["before"])
        self.assertEqual(rows["MongoFindCommandsPerEventByTag.find-by-key"]["after"]["median"], 1)
        after["Workload"]["Events"] = 100
        with self.assertRaisesRegex(ValueError, "Settings differ"):
            bench.compare_records("replay-shape", [replay()], [after])

    def test_activation_flag(self):
        self.assertEqual(bench.observer_activations(rebuild()), 100)
        self.assertEqual(bench.activation_flag([rebuild()], [rebuild(100, 125)]), "flagged")
        self.assertEqual(bench.activation_flag([rebuild()], [rebuild(105, 125)]), "not flagged")
        missing = rebuild()
        missing["GrainActivations"][0]["ActivationsByGrainType"] = {"other": 5}
        self.assertEqual(bench.activation_flag([rebuild(), missing], [rebuild(100, 125)]), "inconclusive")
        two = rebuild()
        two["Environment"]["Silos"][1]["Observers"] = True
        self.assertEqual(bench.observer_activations(two), 1099)


class Execution(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir=ROOT / ".ai-work")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.results = self.root / "results"
        self.results.mkdir()
        sides = {}
        for side in ("before", "after"):
            directory = self.root / side
            directory.mkdir()
            sides[side] = {"directory": str(directory), "sha": side + "-sha", "harnessSha": "harness-sha", "overlaid": True}
        (self.results / "sides.json").write_text(json.dumps(sides))
        (self.results / "machine.json").write_text(json.dumps({"cpuModel": "test CPU", "cores": 4}))
        self.stub = self.root / "stub.py"
        self.stub.write_text('import json, os, sys, time\n'
                             'time.sleep(float(os.environ.get("STUB_SLEEP", "0")))\n'
                             'print("log line")\n'
                             'print(json.dumps({"Verdict": "passed", "Settings": {}, '
                             '"Commit": os.environ.get("COMMIT_SHA"), "github": os.environ.get("GITHUB_SHA"), '
                             '"size": os.environ.get("REBUILD_SIZE")}))\n'
                             'sys.exit(int(os.environ.get("STUB_EXIT", "0")) if os.environ.get("REBUILD_SIZE") != "1000" else 0)\n')
        self.args = argparse.Namespace(target="rebuild", cell="rebuild-S2-two", scenario="S2", topology="two",
                                       results=self.results, launcher=[sys.executable, str(self.stub)])
        self.environment = patch.dict(os.environ, {"REPETITIONS": "2", "SIZE": "small", "MODE": "live",
                                                   "TIMEOUT_SECONDS": "10", "RUN_BUDGET_SECONDS": "60",
                                                   "GITHUB_SHA": "workflow-sha", "STUB_EXIT": "0"})
        self.environment.start()
        self.addCleanup(self.environment.stop)

    def runs(self):
        return [json.loads(path.read_text()) for path in sorted((self.results / "runs").glob("*.json"))]

    def test_run_and_summary(self):
        self.assertEqual(bench.run(self.args), 0)
        runs = self.runs()
        self.assertEqual([run["side"] for run in runs], ["before", "after", "before", "after", "after", "before"])
        self.assertEqual([run["warmup"] for run in runs], [True, True, False, False, False, False])
        self.assertEqual(runs[0]["record"]["size"], "1000")
        self.assertEqual(runs[2]["record"]["size"], "small")
        for run in runs:
            self.assertEqual(run["record"]["Commit"], run["sha"])
            self.assertIsNone(run["record"]["github"])
        self.assertEqual(len(list((self.results / "runs").glob("*.stdout.log"))), 6)
        summary = self.root / "summary.md"
        self.assertEqual(bench.compare(argparse.Namespace(results=self.results, summary=summary)), 0)
        markdown = summary.read_text()
        self.assertIn("| Metric | Before median (spread) | After median (spread) |", markdown)
        self.assertIn("| Seq | Side | Status | Seconds |", markdown)
        self.assertIn("ProjectionObserverSubscriber activation flag: inconclusive", markdown)
        self.assertIn("harness-sha", markdown)
        self.assertIn("no spread", bench.format_stats({"median": 1, "spreadPercent": None}))

    def test_replay_environment_and_numeric_size(self):
        self.args.target = "replay-shape"
        self.args.cell = "replay-shape"
        self.stub.write_text('import json, os\n'
                             'print(json.dumps({"Before": {"Count": 1, "Hash": "abc"}, '
                             '"After": {"Count": 1, "Hash": "abc"}, '
                             '"roots": os.environ.get("REPLAY_ROOTS"), "events": os.environ.get("REPLAY_EVENTS")}))\n')
        with patch.dict(os.environ, SIZE="15000", REPLAY_ROOTS="2000"):
            self.assertEqual(bench.run(self.args), 0)
        runs = self.runs()
        self.assertEqual(runs[0]["record"]["roots"], "100")
        self.assertEqual(runs[0]["record"]["events"], "1000")
        self.assertEqual(runs[2]["record"]["roots"], "2000")
        self.assertEqual(runs[2]["record"]["events"], "15000")

    def test_failed_run_finishes_pair_and_stops(self):
        os.environ["STUB_EXIT"] = "2"
        self.assertEqual(bench.run(self.args), 1)
        self.assertEqual([run["status"] for run in self.runs()], ["passed", "passed", "failed", "failed"])

    def test_warmup_failure_fails_fast(self):
        self.stub.write_text('import sys\nsys.exit(1)\n')
        self.assertEqual(bench.run(self.args), 1)
        self.assertEqual(len(self.runs()), 1)

    def test_budget_marks_incomplete(self):
        os.environ["RUN_BUDGET_SECONDS"] = "0.000001"
        self.assertEqual(bench.run(self.args), 1)
        self.assertEqual(json.loads((self.results / "run.json").read_text())["status"], "INCOMPLETE")

    def test_wall_timeout_records_and_kills_process_group(self):
        with patch.object(bench, "wall_timeout", return_value=0.05), patch.dict(os.environ, {"STUB_SLEEP": "2"}):
            self.assertEqual(bench.run(self.args), 1)
        self.assertEqual(self.runs()[0]["status"], "timed-out")

    def test_cli_matrix_output(self):
        output = self.root / "output"
        env = dict(os.environ, TARGET="rebuild", SCENARIOS="S2", TOPOLOGIES="two", BEFORE_SHA="a", AFTER_SHA="b",
                   GITHUB_OUTPUT=str(output))
        result = subprocess.run([sys.executable, "-B", str(SCRIPT), "matrix"], env=env, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(json.loads(output.read_text().removeprefix("matrix="))["include"][0]["cell"], "rebuild-S2-two")

    def test_build_both_sides_and_overlay_only_benchmarks(self):
        harness = self.root / "harness"
        program = harness / bench.PROJECT / "Program.cs"
        program.parent.mkdir(parents=True)
        program.write_text('"--rebuild"')
        sides = json.loads((self.results / "sides.json").read_text())
        for side in sides.values():
            directory = Path(side["directory"])
            (directory / "Directory.Packages.props").write_text("unchanged")
        args = argparse.Namespace(side=[f"{name}:{side['directory']}" for name, side in sides.items()],
                                  harness=harness, target="rebuild", out=self.results)
        def git_value(directory, revision):
            return "harness-tree" if directory == harness else "product-tree"

        with patch.object(bench, "git_value", side_effect=git_value), patch.object(bench, "build_product", return_value=1) as build:
            self.assertEqual(bench.build(args), 1)
        self.assertEqual(build.call_count, 2)
        recorded = json.loads((self.results / "sides.json").read_text())
        self.assertTrue(recorded["before"]["overlaid"])
        self.assertEqual(recorded["after"]["harnessTree"], "harness-tree")
        for side in sides.values():
            self.assertEqual((Path(side["directory"]) / "Directory.Packages.props").read_text(), "unchanged")
            self.assertTrue((Path(side["directory"]) / bench.PROJECT / "Program.cs").exists())

    def test_missing_harness_diagnostic(self):
        args = argparse.Namespace(side=[f"before:{self.root / 'before'}"], harness=None,
                                  target="rebuild", out=self.results)
        summary = self.root / "summary"
        with patch.object(bench, "git_value", return_value="abc"), patch.dict(os.environ, GITHUB_STEP_SUMMARY=str(summary)):
            self.assertEqual(bench.build(args), 1)
        text = summary.read_text()
        for expected in ("before", "abc", "--rebuild", "harness_ref", "Product build failed"):
            self.assertIn(expected, text)


if __name__ == "__main__":
    unittest.main()
