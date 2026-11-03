# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Exercise the prerelease artifact layout and the actual pre-publish shell steps, without caches or publishing.

Run with Python and PyYAML. YAML syntax/expression validation is also covered by actionlint.
"""

from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

import yaml

ROOT = Path(__file__).resolve().parents[2]
JOBS = yaml.safe_load((ROOT / ".github/workflows/pull-requests.yml").read_text())["jobs"]
SERVER_FILES = (
    "Cratis.Chronicle.Server",
    "Cratis.Chronicle.Server.dll",
    "Cratis.Chronicle.Server.deps.json",
    "Cratis.Chronicle.Server.runtimeconfig.json",
)


def action_steps(job, action):
    return [step for step in JOBS[job]["steps"] if step.get("uses", "").startswith(action + "@")]


class ArtifactHandoff(unittest.TestCase):
    def setUp(self):
        scratch = ROOT / ".ai-work"
        scratch.mkdir(exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=scratch)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.artifacts = {}
        self.producers = {}
        for job in ("workbench", "kernel-development"):
            for upload in action_steps(job, "actions/upload-artifact"):
                name = upload["with"]["name"]
                self.producers[name] = job
                # Model upload-artifact's directory-root layout, not a cache restore.
                output = self.root / "producer" / job / upload["with"]["path"]
                output.mkdir(parents=True)
                self.artifacts[name] = output
                if job == "workbench":
                    (output / "index.html").write_text("Workbench entry point")
                    (output / "assets").mkdir()
                    (output / "assets/app.js").write_text("Workbench bundle")
                else:
                    for architecture in ("x64", "arm64"):
                        (output / architecture).mkdir()
                        for filename in SERVER_FILES:
                            (output / architecture / filename).write_text("DEVELOPMENT kernel")

    def prepare(self, job):
        consumer = self.root / job
        consumer.mkdir()
        # Stop before setup/build/publish actions; execute the same validation/preparation
        # snippets CI runs after downloads. No Docker, package registry or remote effects.
        stop = "Set up Docker Buildx" if job == "publish-docker" else "Setup .Net"
        for step in JOBS[job]["steps"]:
            if step.get("name") == stop:
                break
            if step.get("uses", "").startswith("actions/download-artifact@"):
                artifact = self.artifacts[step["with"]["name"]]
                shutil.copytree(artifact, consumer / step["with"]["path"], dirs_exist_ok=True)
            if "run" in step:
                result = subprocess.run(
                    ["bash", "-euo", "pipefail", "-c", step["run"]],
                    cwd=consumer, capture_output=True, text=True, check=False,
                )
                if result.returncode:
                    return consumer, result
        return consumer, None

    def test_consumers_depend_on_the_exact_artifact_producers(self):
        for job in ("publish-docker", "publish-nuget-packages"):
            downloads = action_steps(job, "actions/download-artifact")
            self.assertTrue(downloads)
            for download in downloads:
                self.assertIn(self.producers[download["with"]["name"]], JOBS[job]["needs"])

    def test_missing_uploads_are_errors_not_warnings(self):
        for job in ("workbench", "kernel-development"):
            uploads = action_steps(job, "actions/upload-artifact")
            self.assertTrue(uploads)
            for upload in uploads:
                self.assertEqual("error", upload["with"].get("if-no-files-found"))

    def test_build_outputs_are_not_delivered_by_optional_caches(self):
        for job in JOBS:
            for cache in action_steps(job, "actions/cache"):
                path = cache["with"]["path"]
                self.assertNotIn("Server/out", path)
                self.assertNotIn("Workbench/wwwroot", path)

    def test_docker_receives_complete_development_and_workbench_outputs_without_caches(self):
        consumer, failure = self.prepare("publish-docker")
        self.assertIsNone(failure)
        for architecture in ("x64", "arm64"):
            for filename in SERVER_FILES:
                self.assertEqual("DEVELOPMENT kernel", (consumer / "Source/Kernel/Server/out" / architecture / filename).read_text())
        self.assertTrue((consumer / "Source/Workbench/wwwroot/assets/app.js").is_file())
        self.assertIn("-p:DefineConstants=DEVELOPMENT", str(JOBS["kernel-development"]["steps"]))

    def test_nuget_receives_workbench_without_caches(self):
        consumer, failure = self.prepare("publish-nuget-packages")
        self.assertIsNone(failure)
        self.assertTrue((consumer / "Source/Workbench/wwwroot/index.html").is_file())
        self.assertTrue((consumer / "Source/Workbench/wwwroot/assets/app.js").is_file())

    def test_both_publishers_reject_a_workbench_without_its_entry_point(self):
        (self.artifacts["workbench-out"] / "index.html").unlink()
        for job in ("publish-docker", "publish-nuget-packages"):
            with self.subTest(job=job):
                _, failure = self.prepare(job)
                self.assertIsNotNone(failure)
                self.assertNotEqual(0, failure.returncode)

    def test_docker_rejects_a_missing_development_binary(self):
        (self.artifacts["kernel-development-out"] / "x64/Cratis.Chronicle.Server.dll").unlink()
        _, failure = self.prepare("publish-docker")
        self.assertIsNotNone(failure)
        self.assertIn("Required prerelease output is missing or empty", failure.stdout)

    def test_docker_rejects_an_empty_runtime_configuration(self):
        (self.artifacts["kernel-development-out"] / "x64/Cratis.Chronicle.Server.runtimeconfig.json").write_text("")
        _, failure = self.prepare("publish-docker")
        self.assertIsNotNone(failure)
        self.assertIn("runtimeconfig.json", failure.stdout)


if __name__ == "__main__":
    unittest.main(verbosity=2)
