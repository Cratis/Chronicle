# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Lock the analyzer ownership map: every configuration has one analyzer-owning build, the rest skip analyzers."""

from pathlib import Path
import re
import unittest

import yaml

ROOT = Path(__file__).resolve().parents[2]
WORKFLOWS = ROOT / ".github" / "workflows"
SKIP = "RunAnalyzers=false"


def workflow(filename):
    # PyYAML treats `on` as YAML 1.1's boolean true; BaseLoader keeps GitHub's key intact.
    with (WORKFLOWS / filename).open(encoding="utf-8") as file:
        return yaml.load(file, Loader=yaml.BaseLoader)


def step_run(wf, job, name):
    for step in wf["jobs"][job]["steps"]:
        if step.get("name") == name:
            return step["run"]
    raise AssertionError(f"{job} has no step named {name!r}")


def without_comments(script):
    return "\n".join(line for line in script.splitlines() if not line.lstrip().startswith("#"))


class AnalyzerOwnership(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build = workflow("dotnet-build.yml")
        cls.legacy = workflow("legacy-contracts.yml")

    def test_owners_keep_analyzers(self):
        owners = [
            ("dotnet-build", "Build"),
            ("dotnet-build", "Build client packages for every shipped framework"),
            ("dotnet-build-development", "Build with DEVELOPMENT symbol"),
            ("dotnet-build-development-requested", "Build with DEVELOPMENT symbol"),
        ]
        for job, name in owners:
            with self.subTest(job=job, step=name):
                run = step_run(self.build, job, name)
                self.assertIn("dotnet build", run)
                self.assertNotIn(SKIP, run)

    def test_debug_publish_skips_analyzers(self):
        run = step_run(self.build, "dotnet-build", "Build development Docker image")
        self.assertIn("dotnet publish", run)
        self.assertIn(f"-p:{SKIP}", run)

    def test_benchmark_builds_skip_analyzers(self):
        run = without_comments(step_run(self.build, "benchmarks-build", "Build benchmarks the way BenchmarkDotNet does"))
        self.assertEqual(2, len(re.findall(r"dotnet build", run)))
        self.assertEqual(2, run.count(f"/p:{SKIP}"))

    def test_legacy_contracts_build_skips_analyzers(self):
        run = step_run(self.legacy, "legacy-contracts", "Build the matrix evaluator")
        self.assertIn("dotnet build", run)
        self.assertIn(f"-p:{SKIP}", run)

    def test_packaging_build_skips_analyzers_only_for_the_owned_framework(self):
        run = without_comments(step_run(self.build, "dotnet-build", "Build client packages for every shipped framework"))
        self.assertEqual(2, run.count("dotnet build"))
        self.assertEqual(2, run.count("-p:SkipAnalyzersForTargetFramework=net10.0"))

    def test_framework_skip_is_scoped_to_that_framework(self):
        targets = (ROOT / "Directory.Build.targets").read_text(encoding="utf-8")
        match = re.search(r'<PropertyGroup Condition="([^"]*SkipAnalyzersForTargetFramework[^"]*)">\s*<RunAnalyzers>false</RunAnalyzers>', targets)
        self.assertIsNotNone(match)
        self.assertIn("'$(TargetFramework)' == '$(SkipAnalyzersForTargetFramework)'", match.group(1))
        self.assertEqual(1, targets.count("<RunAnalyzers>false</RunAnalyzers>"))

    def test_analyzers_are_never_removed(self):
        files = list(WORKFLOWS.glob("*.yml")) + [ROOT / "Directory.Build.props", ROOT / "Directory.Build.targets"]
        for path in files:
            text = path.read_text(encoding="utf-8")
            with self.subTest(file=path.name):
                self.assertIsNone(re.search(r"EnableNETAnalyzers[=>]\s*false", text, re.IGNORECASE))
                self.assertNotIn("RunAnalyzersDuringBuild=false", text)


if __name__ == "__main__":
    unittest.main()
