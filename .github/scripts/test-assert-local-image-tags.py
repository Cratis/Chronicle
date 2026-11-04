# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Plant local image tag defects so the checkout-only guard cannot silently stop checking."""

import contextlib
import importlib.util
import io
from pathlib import Path
import re
import tempfile
import unittest

SCRIPT = Path(__file__).with_name("assert-local-image-tags.py")
spec = importlib.util.spec_from_file_location("assert_local_image_tags", SCRIPT)
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)
ROOT = SCRIPT.parents[2]
LOCAL = "cratis/chronicle:local-development-slim"


def project(reference):
    return (
        '<Project><PropertyGroup><LocalImageName Condition="\'$(LocalImageName)\' == \'\'">'
        f'{reference}</LocalImageName></PropertyGroup><Target Name="BuildDocker">'
        '<Exec Command="docker build -t $(LocalImageName) -f X ." /></Target></Project>'
    )


def runtime(reference):
    return f'Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "{reference}";'


class LocalImageTags(unittest.TestCase):
    def run_check(self, root):
        output, errors = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(output), contextlib.redirect_stderr(errors):
            result = checker.main([str(SCRIPT), str(root)])
        return result, output.getvalue(), errors.getvalue()

    def tree_check(self, files):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name, content in files.items():
                path = root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content, encoding="utf-8")
            return self.run_check(root)

    def test_committed_repository_passes(self):
        result, output, errors = self.run_check(ROOT)
        self.assertEqual(0, result, errors)
        counts = re.search(r"(\d+) docker build producers .*?, (\d+) CRATIS_CHRONICLE_LOCAL_IMAGE defaults", output)
        self.assertIsNotNone(counts, output)
        self.assertGreaterEqual(int(counts[1]), 4)
        self.assertGreaterEqual(int(counts[2]), 4)
        self.assertIn("0 claim a published tag.", output)

    def test_msbuild_published_defaults_fail(self):
        for reference in ("cratis/chronicle:latest-development", "cratis/chronicle:1.2.3-development-slim"):
            with self.subTest(reference=reference):
                result, _, errors = self.tree_check({"x.csproj": project(reference), "x.cs": runtime(LOCAL)})
                self.assertEqual(1, result, errors)
                self.assertIn("x.csproj", errors)
                self.assertIn(reference, errors)

    def test_implicit_latest_shell_build_fails_and_comments_are_ignored(self):
        result, _, errors = self.tree_check({
            "x.sh": "# docker build -t cratis/chronicle -f X .\ndocker build -t cratis/chronicle -f X .\n",
            "x.cs": runtime(LOCAL),
        })
        self.assertEqual(1, result, errors)
        self.assertEqual(1, errors.count("x.sh:"), errors)
        self.assertIn("cratis/chronicle", errors)

    def test_docker_hub_prefixes_are_normalised(self):
        for prefix in ("docker.io/", "index.docker.io/"):
            with self.subTest(prefix=prefix):
                result, _, errors = self.tree_check({
                    "x.sh": f"docker build -t {prefix}cratis/chronicle:latest -f X .",
                    "x.cs": runtime(LOCAL),
                })
                self.assertEqual(1, result, errors)
                self.assertIn(f"x.sh: {prefix}cratis/chronicle:latest", errors)

    def test_unresolvable_property_fails(self):
        result, _, errors = self.tree_check({
            "x.proj": '<Exec Command="docker build -t $(SomethingElse) -f X ." />',
            "x.cs": runtime(LOCAL),
        })
        self.assertEqual(1, result, errors)
        self.assertIn("$(SomethingElse)", errors)
        self.assertIn("unresolvable", errors)

    def test_published_runtime_default_fails(self):
        reference = "cratis/chronicle:latest-development"
        result, _, errors = self.tree_check({"x.csproj": project(LOCAL), "x.cs": runtime(reference)})
        self.assertEqual(1, result, errors)
        self.assertIn(f"x.cs: {reference}", errors)

    def test_runtime_default_without_a_producer_fails(self):
        result, _, errors = self.tree_check({"x.csproj": project(LOCAL), "x.cs": runtime("cratis/chronicle:local-other")})
        self.assertEqual(1, result, errors)
        self.assertIn("no local build produces it", errors)

    def test_clean_tree_passes(self):
        result, output, errors = self.tree_check({"x.csproj": project(LOCAL), "x.cs": runtime(LOCAL)})
        self.assertEqual(0, result, errors)
        self.assertIn("1 docker build producers (1 MSBuild, 0 shell), 1 CRATIS_CHRONICLE_LOCAL_IMAGE defaults", output)

    def test_empty_tree_cannot_vouch(self):
        result, _, errors = self.tree_check({})
        self.assertEqual(2, result, errors)
        self.assertIn("cannot vouch", errors)

    def test_producers_without_runtime_defaults_cannot_vouch(self):
        result, _, errors = self.tree_check({"x.csproj": project(LOCAL)})
        self.assertEqual(2, result, errors)
        self.assertIn("cannot vouch", errors)

    def test_long_tag_option_and_buildx_are_checked(self):
        for command in ("docker build --tag", "docker buildx build --tag"):
            with self.subTest(command=command):
                result, _, errors = self.tree_check({
                    "x.sh": f'{command} "cratis/chronicle:latest" -f X .',
                    "x.cs": runtime(LOCAL),
                })
                self.assertEqual(1, result, errors)
                self.assertIn("x.sh: cratis/chronicle:latest", errors)
        result, _, errors = self.tree_check({
            "x.targets": project("cratis/chronicle:latest").replace(" -t ", " --tag "),
            "x.cs": runtime(LOCAL),
        })
        self.assertEqual(1, result, errors)
        self.assertIn("x.targets: cratis/chronicle:latest", errors)

    def test_other_repositories_and_local_tag_are_allowed(self):
        for reference in ("example.invalid/chronicle:override", "cratis/chronicle:local"):
            with self.subTest(reference=reference):
                result, _, errors = self.tree_check({"x.props": project(reference), "x.cs": runtime(reference)})
                self.assertEqual(0, result, errors)

    def test_generated_and_private_directories_are_pruned(self):
        files = {"x.csproj": project(LOCAL), "x.cs": runtime(LOCAL)}
        for directory in (".git", "node_modules", "bin", "obj", ".ai-work"):
            files[f"{directory}/bad.sh"] = "docker build -t cratis/chronicle ."
        result, output, errors = self.tree_check(files)
        self.assertEqual(0, result, errors)
        self.assertIn("Checked 2 files:", output)


if __name__ == "__main__":
    unittest.main()
