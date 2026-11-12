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
LOGIN = {"uses": "docker/login-action@v3", "with": {"registry": "ghcr.io", "password": "${{ github.token }}"}}
PUSH_STEPS = [LOGIN, PUSH]


class ImagePushPermissions(unittest.TestCase):
    def run_check(self, root):
        output, errors = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(errors):
            result = checker.main([str(SCRIPT), str(root)])
        return result, output.getvalue(), errors.getvalue()

    def tree_check(self, workflow, name="image.yml"):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            if workflow is not None:
                path = root / ".github/workflows" / name
                path.parent.mkdir(parents=True)
                path.write_text(yaml.safe_dump(workflow), encoding="utf-8")
            return self.run_check(root)

    def test_job_permission_violations(self):
        for permission in ({"contents": "read"}, {"packages": "read"}, {"packages": "none"}, "read-all", None):
            with self.subTest(permission=permission):
                result, _, errors = self.tree_check({"jobs": {"publish": {"permissions": permission, "steps": PUSH_STEPS}}})
                self.assertEqual(1, result, errors)
                self.assertIn("image.yml: job publish pushes an image but packages permission is ", errors)

    def test_workflow_permission_fallback_passes(self):
        result, output, errors = self.tree_check({
            "permissions": {"packages": "write"}, "jobs": {"publish": {"steps": [*PUSH_STEPS, PUSH]}},
        })
        self.assertEqual(0, result, errors)
        self.assertIn("1 workflow files: 1 in-scope pushing jobs", output)

    def test_job_override_does_not_merge_workflow_permissions(self):
        for permission in ({"packages": "read"}, {"contents": "read"}, None):
            with self.subTest(permission=permission):
                result, _, errors = self.tree_check({
                    "permissions": {"packages": "write"},
                    "jobs": {"publish": {"permissions": permission, "steps": PUSH_STEPS}},
                })
                self.assertEqual(1, result, errors)

    def test_write_all_passes_at_either_level(self):
        for workflow in (
            {"permissions": "write-all", "jobs": {"publish": {"steps": PUSH_STEPS}}},
            {"jobs": {"publish": {"permissions": "write-all", "steps": PUSH_STEPS}}},
        ):
            with self.subTest(workflow=workflow):
                result, _, errors = self.tree_check(workflow)
                self.assertEqual(0, result, errors)

    def test_job_write_overrides_workflow_read(self):
        result, _, errors = self.tree_check({
            "permissions": "read-all",
            "jobs": {"publish": {"permissions": {"packages": "write"}, "steps": PUSH_STEPS}},
        })
        self.assertEqual(0, result, errors)

    def test_build_push_expressions_and_strings_are_checked(self):
        for push in (True, "true", "${{ github.ref == 'refs/heads/main' }}", "${{ false }}"):
            with self.subTest(push=push):
                step = {"uses": "docker/build-push-action@v6", "with": {"push": push}}
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [LOGIN, step]}}})
                self.assertEqual(1, result, errors)
                self.assertIn("permission is missing", errors)

    def test_non_pushing_steps_are_ignored(self):
        ignored = [
            LOGIN,
            {"uses": "docker/build-push-action@v6", "with": {"push": False}},
            {"uses": "docker/build-push-action@v6", "with": {"push": "false"}},
            {"uses": "docker/build-push-action@v6"},
            {"run": "docker buildx build --load .\n# docker push example.invalid/image"},
        ]
        result, output, errors = self.tree_check({"jobs": {
            "publish": {"permissions": {"packages": "write"}, "steps": PUSH_STEPS},
            "build": {"steps": ignored},
            "reusable": {"uses": "./.github/workflows/other.yml"},
        }})
        self.assertEqual(0, result, errors)
        self.assertIn("1 in-scope pushing jobs", output)

    def test_docker_shell_push_forms_are_detected(self):
        for command in (
            "docker push example.invalid/image",
            "docker image push example.invalid/image",
            "docker buildx build --platform linux/amd64 --push .",
            "docker buildx build \\\n  --tag example.invalid/image \\\n  --push .",
        ):
            with self.subTest(command=command):
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [LOGIN, {"run": command}]}}})
                self.assertEqual(1, result, errors)

    def test_other_credentials_and_registries_are_ignored(self):
        for inputs in (
            {"username": "${{ secrets.DOCKER_USERNAME }}", "password": "${{ secrets.DOCKER_PASSWORD }}"},
            {"registry": "ghcr.io", "password": "${{ secrets.GHCR_PAT }}"},
            {"registry": "docker.io", "password": "${{ github.token }}"},
        ):
            with self.subTest(inputs=inputs):
                login = {"uses": "docker/login-action@v3", "with": inputs}
                result, output, errors = self.tree_check({"jobs": {
                    "publish": {"permissions": {"packages": "write"}, "steps": PUSH_STEPS},
                    "other": {"steps": [login, PUSH]},
                }})
                self.assertEqual(0, result, errors)
                self.assertIn("1 in-scope pushing jobs", output)

    def test_ghcr_workflow_token_login_requires_package_write(self):
        for token in ("${{ github.token }}", "${{ secrets.GITHUB_TOKEN }}"):
            with self.subTest(token=token):
                login = {"uses": "docker/login-action@v3", "with": {"registry": "GHCR.IO", "password": token}}
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [login, PUSH]}}})
                self.assertEqual(1, result, errors)
                self.assertIn("permission is missing", errors)

    def test_shell_ghcr_workflow_token_login_is_detected(self):
        for token in ('"$GITHUB_TOKEN"', '"${{ github.token }}"', '"${{ secrets.GITHUB_TOKEN }}"'):
            with self.subTest(token=token):
                login = {"run": f'echo {token} | docker login GHCR.IO -u example --password-stdin'}
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [login, PUSH]}}})
                self.assertEqual(1, result, errors)

    def test_shell_ghcr_login_argument_orders_are_detected(self):
        for command in (
            'echo "$GITHUB_TOKEN" | docker login -u example --password-stdin ghcr.io',
            'echo "$GITHUB_TOKEN" | docker login --username example --password-stdin https://ghcr.io',
            'docker login -u example -p "${{ github.token }}" ghcr.io/cratis',
        ):
            with self.subTest(command=command):
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [{"run": command}, PUSH]}}})
                self.assertEqual(1, result, errors)

    def test_yaml_extension_workflows_are_scanned(self):
        result, _, errors = self.tree_check({"jobs": {"publish": {"steps": PUSH_STEPS}}}, name="image.yaml")
        self.assertEqual(1, result, errors)
        self.assertIn("image.yaml: job publish pushes an image", errors)

    def test_shell_other_credentials_and_registries_are_ignored(self):
        for command in (
            'echo "$GHCR_PAT" | docker login ghcr.io -u example --password-stdin',
            'echo "$GITHUB_TOKEN" | docker login docker.io -u example --password-stdin',
            '# echo "$GITHUB_TOKEN" | docker login ghcr.io -u example --password-stdin',
        ):
            with self.subTest(command=command):
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": [{"run": command}, PUSH]}}})
                self.assertEqual(2, result, errors)
                self.assertIn("0 in-scope pushing jobs", errors)

    def test_zero_files_cannot_vouch(self):
        result, _, errors = self.tree_check(None)
        self.assertEqual(2, result, errors)
        self.assertIn("0 workflow files", errors)

    def test_zero_in_scope_jobs_cannot_vouch(self):
        for steps in ([LOGIN], [PUSH]):
            with self.subTest(steps=steps):
                result, _, errors = self.tree_check({"jobs": {"publish": {"steps": steps}}})
                self.assertEqual(2, result, errors)
                self.assertIn("0 in-scope pushing jobs", errors)

    def test_each_violating_job_is_reported(self):
        result, _, errors = self.tree_check({"jobs": {
            "one": {"steps": [*PUSH_STEPS, PUSH]}, "two": {"steps": PUSH_STEPS},
        }})
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
