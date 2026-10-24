#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for derive-build-version.py."""

import contextlib
import importlib.util
import io
import os
import sys
import unittest
from unittest import mock

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(SCRIPT_DIRECTORY, "derive-build-version.py")


def _load():
    """Import the script, whose filename is not a valid module name."""
    spec = importlib.util.spec_from_file_location("derive_build_version", SCRIPT)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Could not load module spec for {SCRIPT}")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


derive = _load()


def ref(tag):
    return f"0123456789abcdef0123456789abcdef01234567\trefs/tags/{tag}"


class for_next_version(unittest.TestCase):
    def test_highest_stable_tag_wins_numerically(self):
        refs = [ref("v19.9.0"), ref("v19.110.1"), ref("v19.108.1")]
        self.assertEqual(("19.111.0-ci.42", "v19.110.1"), derive.next_version(refs, "42"))

    def test_prerelease_and_odd_tags_are_ignored(self):
        refs = [ref("v19.110.1"), ref("v20.0.0-alpha.1"), ref("preserve/pr-4438"), ref("review-base-5184")]
        self.assertEqual("19.111.0-ci.7", derive.next_version(refs, "7")[0])

    def test_no_stable_tag_fails(self):
        with self.assertRaises(derive.DeriveError):
            derive.next_version([ref("v1.0.0-alpha.1")], "1")

    def test_missing_or_non_numeric_run_number_fails(self):
        for run in (None, "", "abc", "1.5", "-3"):
            with self.subTest(run=run), self.assertRaises(derive.DeriveError):
                derive.next_version([ref("v19.110.1")], run)


class for_main(unittest.TestCase):
    def test_prints_exactly_one_env_line(self):
        out, err = io.StringIO(), io.StringIO()
        completed = mock.Mock(stdout=ref("v19.110.1") + "\n")
        with mock.patch.dict(os.environ, {"GITHUB_RUN_NUMBER": "42"}), \
                mock.patch.object(derive.subprocess, "run", return_value=completed), \
                contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            code = derive.main()
        self.assertEqual(0, code)
        self.assertEqual("CRATIS_CHRONICLE_BUILD_VERSION=19.111.0-ci.42\n", out.getvalue())

    def test_failure_exits_non_zero_with_error_annotation(self):
        out, err = io.StringIO(), io.StringIO()
        completed = mock.Mock(stdout="")
        with mock.patch.dict(os.environ, {"GITHUB_RUN_NUMBER": "42"}), \
                mock.patch.object(derive.subprocess, "run", return_value=completed), \
                contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
            code = derive.main()
        self.assertEqual(1, code)
        self.assertEqual("", out.getvalue())
        self.assertIn("::error::", err.getvalue() + out.getvalue())


if __name__ == "__main__":
    sys.exit(unittest.main())
