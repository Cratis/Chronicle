#!/usr/bin/env python3
"""Specs for the shared SQL Server image pin."""
import importlib.util
from pathlib import Path
import subprocess
import tempfile
import unittest

SCRIPT = Path(__file__).with_name("sql-server-image-pin.py")
SPEC = importlib.util.spec_from_file_location("pin", SCRIPT)
pin = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(pin)
ROOT = SCRIPT.resolve().parents[2]
IMAGE = "mcr.microsoft.com/mssql/server:2025-CU10-ubuntu-24.04"
CONSTANT = "Integration/Client/SqlServerContainerImage.cs"
WORKFLOW = ".github/workflows/integration.yml"


class when_checking_the_repository(unittest.TestCase):
    def test_the_pin_is_explicit_and_current(self):
        self.assertTrue(pin.validate_pin(pin.pinned_image(ROOT)))

    def test_all_references_follow_the_pin(self):
        self.assertEqual([], pin.violations(ROOT))


class when_checking_references(unittest.TestCase):
    def setUp(self):
        self.tree = tempfile.TemporaryDirectory()
        self.addCleanup(self.tree.cleanup)
        self.root = Path(self.tree.name)
        subprocess.run(["git", "init", "-q", self.root], check=True)
        self.write(CONSTANT, f'public const string Name = "{IMAGE}";\n')
        self.write(WORKFLOW, f"docker pull {IMAGE}\ndocker pull {IMAGE}\n")

    def write(self, path, text):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text)
        subprocess.run(["git", "-C", self.root, "add", path], check=True)

    def test_rejects_floating_tags(self):
        for tag in ("latest", "2025-latest"):
            self.assertFalse(pin.validate_pin(f"mcr.microsoft.com/mssql/server:{tag}"))

    def test_rejects_an_older_major(self):
        self.assertFalse(pin.validate_pin("mcr.microsoft.com/mssql/server:2022-CU20-ubuntu-22.04"))

    def test_rejects_rtm(self):
        self.assertFalse(pin.validate_pin("mcr.microsoft.com/mssql/server:2025-RTM-ubuntu-24.04"))

    def test_reports_a_mismatched_workflow_with_its_location(self):
        self.write(WORKFLOW, f"docker pull {IMAGE}\ndocker pull mcr.microsoft.com/mssql/server:2025-CU9-ubuntu-24.04\n")
        self.assertTrue(any(path == WORKFLOW and line == 2 and "does not match" in message
                            for path, line, message in pin.violations(self.root)))

    def test_reports_a_csharp_literal_outside_the_constant(self):
        self.write("Integration/Other.cs", f'var image = "{IMAGE}";\n')
        self.assertTrue(any(path == "Integration/Other.cs" and "constant" in message
                            for path, _, message in pin.violations(self.root)))

    def test_ignores_documentation(self):
        self.write("Documentation/example.md", "mcr.microsoft.com/mssql/server:2025-latest\n")
        self.assertEqual([], pin.violations(self.root))

    def test_reports_a_missing_clustering_pre_pull(self):
        self.write(WORKFLOW, f"docker pull {IMAGE}\n")
        self.assertTrue(any(path == WORKFLOW and "two" in message
                            for path, _, message in pin.violations(self.root)))


if __name__ == "__main__":
    unittest.main()
