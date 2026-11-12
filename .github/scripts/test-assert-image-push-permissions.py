# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Plant image-push permission defects so the checkout-only guard stays effective (#5854)."""

import contextlib
import importlib.util
import io
from pathlib import Path
import tempfile
import unittest

import yaml

SCRIPT = Path(__file__).with_name("assert-image-push-permissions.py")
spec = importlib.util.spec_from_file_location("assert_image_push_permissions", SCRIPT)
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)
PUSH = {"uses": "docker/build-push-action@v6", "with": {"push": True}}


class ImagePushPermissions(unittest.TestCase):
    def run_check(self, root):
        output, errors = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(errors):
            result = checker.main([str(SCRIPT), str(root)])
        return result, output.getvalue(), errors.getvalue()

    def tree_check(self, workflow):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            if workflow is not None:
                path = root / ".github/workflows/image.yml"
                path.parent.mkdir(parents=True)
                path.write_text(yaml.safe_dump(workflow), encoding="utf-8")
            return self.run_check(root)

    def test_job_permission_violations(self):
        for permission in ({"contents": "read"}, {"packages": "read"}, {"packages": "none"}, "read-all", None):
            with self.subTest(permission=permission):
                result, _, errors = self.tree_check({"jobs": {"publish": {"permissions": permission, "steps": [PUSH]}}})
                self.assertEqual(1, result, errors)
                self.assertIn("image.yml: job publish pushes an image but packages permission is ", errors)

    def test_workflow_permission_fallback_passes(self):
        result, output, errors = self.tree_check({
            "permissions": {"packages": "write"}, "jobs": {"publish": {"steps": [PUSH, PUSH]}},
        })
        self.assertEqual(0, result, errors)
        self.assertIn("1 workflow files: 1 pushing jobs", output)

    def test_job_override_does_not_merge_workflow_permissions(self):
        for permission in ({"packages": "read"}, {"contents": "read"}, None):
            with self.subTest(permission=permission):
                result, _, errors = self.tree_check({
                    "permissions": {"packages": "write"},
                    "jobs": {"publish": {"permissions": permission, "steps": [PUSH]}},
                })
                self.assertEqual(1, result, errors)

    def test_write_all_passes_at_either_level(self):
        for workflow in (
            {"permissions": "write-all", "jobs": {"publish": {"steps": [PUSH]}}},
            {"jobs": {"publish": {"permissions": "write-all", "steps": [PUSH]}}},
        ):
            with self.subTest(workflow=workflow):
                result, _, errors = self.tree_check(workflow)
                self.assertEqual(0, result, errors)

    def test_job_write_overrides_workflow_read(self):
        result, _, errors = self.tree_check({
            "permissions": "read-all",
            "jobs": {"publish": {"permissions": {"packages": "write"}, "steps": [PUSH]}},
        })
        self.assertEqual(0, result, errors)

    def test_build_push_expressions_and_strings_are_checked(self):
        for push in (True, "true", "${{ github.ref == 'refs/heads/main' }}", "${{ false }}"):
            with self.subTest(push=push):
                step = {"uses": "docker/build-push-action@v6", "with": {"push": push}}
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [step]}}})
                self.assertEqual(1, result, errors)
                self.assertIn("permission is missing", errors)

    def test_non_pushing_steps_are_ignored(self):
        ignored = [
            {"uses": "docker/build-push-action@v6", "with": {"push": False}},
            {"uses": "docker/build-push-action@v6", "with": {"push": "false"}},
            {"uses": "docker/build-push-action@v6"},
            {"uses": "docker/login-action@v3", "with": {"registry": "ghcr.io"}},
            {"run": "docker buildx build --load .\n# docker push example.invalid/image"},
        ]
        result, output, errors = self.tree_check({"jobs": {
            "publish": {"permissions": {"packages": "write"}, "steps": [PUSH]},
            "build": {"steps": ignored},
            "reusable": {"uses": "./.github/workflows/other.yml"},
        }})
        self.assertEqual(0, result, errors)
        self.assertIn("1 pushing jobs", output)

    def test_docker_shell_push_forms_are_detected(self):
        for command in (
            "docker push example.invalid/image",
            "docker image push example.invalid/image",
            "docker buildx build --platform linux/amd64 --push .",
            "docker buildx build \\\n  --tag example.invalid/image \\\n  --push .",
        ):
            with self.subTest(command=command):
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [{"run": command}]}}})
                self.assertEqual(1, result, errors)

    def test_zero_files_cannot_vouch(self):
        result, _, errors = self.tree_check(None)
        self.assertEqual(2, result, errors)
        self.assertIn("0 workflow files", errors)

    def test_zero_pushing_jobs_cannot_vouch(self):
        result, _, errors = self.tree_check({"jobs": {"login": {"steps": [{"uses": "docker/login-action@v3"}]}}})
        self.assertEqual(2, result, errors)
        self.assertIn("0 pushing jobs", errors)

    def test_each_violating_job_is_reported(self):
        result, _, errors = self.tree_check({"jobs": {"one": {"steps": [PUSH, PUSH]}, "two": {"steps": [PUSH]}}})
        self.assertEqual(1, result, errors)
        self.assertEqual(2, len(errors.splitlines()), errors)
        self.assertIn("job one", errors)
        self.assertIn("job two", errors)

    def test_committed_repository_passes(self):
        result, output, errors = self.run_check(SCRIPT.parents[2])
        self.assertEqual(0, result, errors)
        self.assertIn("all grant packages: write", output)


if __name__ == "__main__":
    unittest.main()
