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


class IntegrationBundleHandoff(unittest.TestCase):
    def test_every_development_producer_uploads_bundles_alongside_the_full_archive(self):
        for filename, names, prefix in (
            ("dotnet-build.yml", ("dotnet-build-development", "dotnet-build-development-requested"),
             "build-output-development"),
            ("hot-core-gate.yml", ("build",), "hot-core-build-output"),
        ):
            jobs = yaml.safe_load((ROOT / ".github/workflows" / filename).read_text())["jobs"]
            for name in names:
                with self.subTest(workflow=filename, job=name):
                    steps = jobs[name]["steps"]
                    bundles = next(step for step in steps
                                   if step.get("uses") == "./.github/actions/upload-integration-bundles")
                    self.assertEqual({"prefix": prefix}, bundles["with"])
                    full = next(step for step in steps if step.get("uses") == "actions/upload-artifact@v4"
                                and step["with"]["name"] == prefix)
                    self.assertEqual(prefix + ".tar.zst", full["with"]["path"])
                    build = next(step for step in steps if step.get("name") == "Build with DEVELOPMENT symbol")
                    self.assertLess(steps.index(build), steps.index(bundles))

    def test_bundle_uploads_are_required_and_uncompressed(self):
        action = yaml.safe_load((ROOT / ".github/actions/upload-integration-bundles/action.yml").read_text())
        steps = action["runs"]["steps"]
        pack = steps[0]
        self.assertEqual("${{ inputs.prefix }}", pack["env"]["BUNDLE_PREFIX"])
        self.assertIn('pack --prefix "$BUNDLE_PREFIX" --out .integration-bundles', pack["run"])
        uploads = [step for step in steps if step.get("uses") == "actions/upload-artifact@v4"]
        self.assertEqual(3, len(uploads))
        for bundle, upload in zip(("client", "kernel", "clustering"), uploads):
            with self.subTest(bundle=bundle):
                self.assertIn("--bundle " + bundle, pack["run"])
                self.assertEqual({"name": "${{ inputs.prefix }}-bundle-" + bundle,
                                  "path": ".integration-bundles/integration-bundle-" + bundle + ".tar.zst",
                                  "retention-days": 1, "compression-level": 0, "if-no-files-found": "error"},
                                 upload["with"])


class IntegrationBundleConsumer(unittest.TestCase):
    def test_each_main_lane_consumes_its_project_bundle_before_execution(self):
        jobs = yaml.safe_load((ROOT / ".github/workflows/integration.yml").read_text())["jobs"]
        for lane, project in (("client", "Client"), ("kernel", "Kernel"), ("clustering", "Clustering")):
            with self.subTest(lane=lane):
                self.assert_bundle_input(lane, project, jobs[f"{lane}-integration-specs"]["steps"])

    def assert_bundle_input(self, lane, project, steps):
        consume = next(step for step in steps if step.get("uses") == "./.github/actions/consume-integration-bundle")
        expected = {"artifact-prefix": "${{ inputs.build-output-artifact }}",
                    "project": f"Integration/{project}/{project}.csproj",
                    "filter": "${{ matrix.filter }}" if lane == "client" else "FullyQualifiedName~${{ matrix.namespace }}"}
        if lane == "kernel":
            expected["namespaces"] = "${{ matrix.namespace }}"
        self.assertEqual(expected, consume["with"])
        execution = next(step for step in steps if step.get("name", "").startswith(f"Run {project} integration tests"))
        self.assertLess(steps.index(consume), steps.index(execution))
        self.assertFalse(any(step.get("uses") == "actions/download-artifact@v4" for step in steps))
        for step in steps:
            self.assertNotRegex(step.get("run", ""), r"dotnet\s+restore\b|tar\s+-I")

    def test_only_restores_the_coverage_tool(self):
        action = yaml.safe_load((ROOT / ".github/actions/consume-integration-bundle/action.yml").read_text())
        steps = {step["name"]: step for step in action["runs"]["steps"]}
        self.assertEqual({"dotnet-version": "10.0.x"}, steps["Setup .NET"]["with"])
        self.assertEqual("dotnet tool restore", steps["Restore coverage tool"]["run"])
        self.assertEqual({"name": "${{ inputs.artifact-prefix }}-bundle-${{ steps.bundle.outputs.name }}",
                          "path": ".integration-bundles"}, steps["Download integration bundle"]["with"])
        verify = steps["Verify integration bundle"]
        self.assertIn('node .github/scripts/integration-bundles.mjs "${args[@]}"', verify["run"])
        self.assertIn('args=(verify --bundle "$BUNDLE_NAME"', verify["run"])
        self.assertIn('--filter "$BUNDLE_FILTER"', verify["run"])
        self.assertIn('--namespaces "$BUNDLE_NAMESPACES"', verify["run"])
        names = list(steps)
        self.assertLess(names.index("Restore coverage tool"), names.index("Verify integration bundle"))
        self.assertLess(names.index("Download integration bundle"), names.index("Verify integration bundle"))
        for step in steps.values():
            self.assertNotRegex(step.get("run", ""), r"dotnet\s+(restore|build|publish|run|msbuild)\b")
            self.assertNotIn("cache", step.get("uses", ""))


if __name__ == "__main__":
    unittest.main(verbosity=2)
