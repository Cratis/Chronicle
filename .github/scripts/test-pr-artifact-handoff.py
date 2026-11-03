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


class NightlyFeedHandoff(unittest.TestCase):
    def setUp(self):
        (ROOT / '.ai-work').mkdir(exist_ok=True)
        self.build = yaml.load((ROOT / '.github/workflows/dotnet-build.yml').read_text(), Loader=yaml.BaseLoader)['jobs']
        self.publish = yaml.load((ROOT / '.github/workflows/publish.yml').read_text(), Loader=yaml.BaseLoader)['jobs']['coverage-statistics']
        self.steps = {step.get('name'): step for step in self.publish['steps']}

    def test_producer_and_consumer_permissions_and_transport(self):
        producer = self.build['coverage-merge-and-cache']
        self.assertEqual({'contents': 'read', 'actions': 'read'}, producer['permissions'])
        self.assertEqual({'contents': 'read', 'actions': 'read'}, self.publish['permissions'])
        upload = next(step for step in producer['steps'] if step.get('name') == 'Upload nightly coverage feed')
        self.assertRegex(upload['uses'], r'^actions/upload-artifact@[0-9a-f]{40}$')
        self.assertEqual('coverage-nightly-v1-${{ github.run_id }}-${{ github.run_attempt }}', upload['with']['name'])
        self.assertEqual('error', upload['with']['if-no-files-found'])
        self.assertIn("github.event_name == 'schedule'", upload['if'])
        self.assertIn("github.repository == 'Cratis/Chronicle'", upload['if'])
        self.assertIn("github.ref == 'refs/heads/main'", upload['if'])
        self.assertFalse(any('cache@' in step.get('uses', '') for step in self.publish['steps']))
        checkout = self.steps['Checkout code']
        self.assertRegex(checkout['uses'], r'^actions/checkout@[0-9a-f]{40}$')
        self.assertEqual('${{ github.sha }}', checkout['with']['ref'])
        self.assertEqual('${{ secrets.PAT_WORKFLOWS }}', checkout['with']['token'])
        admit = self.steps['Admit scheduled main coverage']
        self.assertEqual('${{ github.token }}', admit['env']['GH_TOKEN'])
        self.assertEqual('Cratis/Chronicle', admit['env']['GH_REPO'])
        self.assertNotIn('continue-on-error', admit)
        self.assertLess(list(self.steps).index('Verify frozen main checkout'), list(self.steps).index('Admit scheduled main coverage'))
        for step in self.publish['steps']:
            self.assertNotIn('PAT_WORKFLOWS', str(step.get('env', {})))
        self.assertIn('python3 -B .github/scripts/test-nightly-coverage-feed.py', str(self.build))
        self.assertEqual('[wire-compatibility, grain-compatibility, release]', str(self.publish['needs']).replace("'", ''))

    def test_report_time_is_captured_immediately_after_reportgenerator(self):
        steps = self.build['coverage-merge-and-cache']['steps']
        generate = next(step for step in steps if step.get('name') == 'Generate merged coverage report')
        self.assertIn('COVERAGE_MEASURED_AT_MS', generate['run'])
        before, after = generate['run'].split('COVERAGE_MEASURED_AT_MS', 1)
        self.assertNotIn('ls -la', before)
        self.assertIn('GITHUB_ENV', after)

    def test_detached_writer_and_main_guard_with_local_git_only(self):
        import os
        with tempfile.TemporaryDirectory(dir=ROOT / '.ai-work') as directory:
            root = Path(directory)
            remote, checkout, other = (root / name for name in ('remote.git', 'checkout', 'other'))
            def git(cwd, *args, ok=True):
                result = subprocess.run(['git', '-C', str(cwd), *args], capture_output=True, text=True)
                if ok:
                    self.assertEqual(0, result.returncode, result.stderr)
                return result
            git(root, 'init', '--bare', '--initial-branch=main', str(remote))
            git(root, 'clone', str(remote), str(checkout))
            for config in (('user.name', 'fixture'), ('user.email', 'fixture@example.invalid')):
                git(checkout, 'config', *config)
            (checkout / 'initial').write_text('main')
            git(checkout, 'add', 'initial')
            git(checkout, 'commit', '-m', 'initial')
            git(checkout, 'push', 'origin', 'HEAD:refs/heads/main')
            sha = git(checkout, 'rev-parse', 'HEAD').stdout.strip()
            git(checkout, 'checkout', '--detach', sha)
            git(root, 'clone', str(remote), str(other))
            for config in (('user.name', 'fixture'), ('user.email', 'fixture@example.invalid')):
                git(other, 'config', *config)
            # A URL rewrite keeps the actual workflow origin checks intact, with no network.
            canonical = 'https://github.com/Cratis/Chronicle'
            git(checkout, 'remote', 'set-url', 'origin', canonical)
            git(checkout, 'config', 'url.' + str(remote) + '.insteadOf', canonical)
            environment = {**os.environ, 'EXPECTED_SHA': sha, 'EXECUTION_REF': 'refs/heads/main',
                           'EXECUTION_REPOSITORY': 'Cratis/Chronicle'}
            def shell(script, env=environment):
                return subprocess.run(['bash', '-euo', 'pipefail', '-c', script], cwd=checkout,
                                      env=env, capture_output=True, text=True)
            guard = self.steps['Verify frozen main checkout']['run']
            self.assertEqual(0, shell(guard).returncode)
            self.assertNotEqual(0, shell(guard, {**environment, 'EXECUTION_REF': 'refs/heads/other'}).returncode)
            self.assertNotEqual(0, shell(guard, {**environment, 'EXPECTED_SHA': '0' * 40}).returncode)
            self.assertNotEqual(0, shell(guard, {**environment, 'EXECUTION_REPOSITORY': 'fork/Chronicle'}).returncode)
            git(checkout, 'remote', 'set-url', 'origin', 'https://example.invalid/fork')
            self.assertNotEqual(0, shell(guard).returncode)
            git(checkout, 'remote', 'set-url', 'origin', canonical)
            stats = checkout / 'Documentation/statistics/coverage-data.js'
            stats.parent.mkdir(parents=True)
            stats.write_text('statistics')
            git(checkout, 'add', 'Documentation/statistics/coverage-data.js')
            git(checkout, 'commit', '-m', 'statistics')
            (other / 'advance').write_text('main advanced')
            git(other, 'add', 'advance')
            git(other, 'commit', '-m', 'advance')
            git(other, 'push', 'origin', 'HEAD:refs/heads/main')
            writer = self.steps['Commit coverage statistics']['run']
            self.assertIn('git fetch origin main', writer)
            self.assertIn('git rebase refs/remotes/origin/main', writer)
            self.assertIn('git push origin HEAD:refs/heads/main', writer)
            self.assertNotIn('--force', writer)
            transport = writer[writer.index('git fetch origin main'):writer.index('git push origin HEAD:refs/heads/main')]
            self.assertEqual(0, shell(transport).returncode)
            self.assertEqual(0, shell('git push origin HEAD:refs/heads/main').returncode)
            git(other, 'pull', '--ff-only')
            (other / 'race').write_text('remote advance after rebase')
            git(other, 'add', 'race')
            git(other, 'commit', '-m', 'race')
            git(other, 'push', 'origin', 'HEAD:refs/heads/main')
            # Make divergent local data: an obsolete push must fail, not silently reset main.
            stats.write_text('new local statistics')
            git(checkout, 'add', 'Documentation/statistics/coverage-data.js')
            git(checkout, 'commit', '-m', 'new statistics')
            self.assertNotEqual(0, shell('git push origin HEAD:refs/heads/main').returncode)
            (other / 'Documentation/statistics/coverage-data.js').write_text('conflicting remote statistics')
            git(other, 'add', 'Documentation/statistics/coverage-data.js')
            git(other, 'commit', '-m', 'conflicting remote statistics')
            git(other, 'push', 'origin', 'HEAD:refs/heads/main')
            remote_head = git(remote, 'rev-parse', 'refs/heads/main').stdout
            conflict = shell(transport)
            self.assertNotEqual(0, conflict.returncode)
            self.assertIn('CONFLICT', conflict.stdout)
            self.assertEqual(remote_head, git(remote, 'rev-parse', 'refs/heads/main').stdout)
            self.assertEqual('', git(checkout, 'branch', '--show-current').stdout.strip())


if __name__ == "__main__":
    unittest.main(verbosity=2)
