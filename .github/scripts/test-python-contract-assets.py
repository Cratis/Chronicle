#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Tests for python-contract-assets.py. Run: python3 .github/scripts/test-python-contract-assets.py"""

from __future__ import annotations

import hashlib
import importlib.util
import tarfile
import tempfile
import unittest
import zipfile
from pathlib import Path

spec = importlib.util.spec_from_file_location("assets", Path(__file__).with_name("python-contract-assets.py"))
assets = importlib.util.module_from_spec(spec)
spec.loader.exec_module(assets)


def make_dist(directory: Path, version: str, wheel_version: str | None = None, omit: str | None = None) -> None:
    wheel, sdist = assets.expected_files(version)
    paths = [p for p in assets.REQUIRED_PACKAGE_PATHS if p != omit]
    with zipfile.ZipFile(directory / wheel, "w") as archive:
        for path in paths:
            archive.writestr(f"{assets.DISTRIBUTION_NAME}/{path}", "")
        archive.writestr(
            f"{assets.DISTRIBUTION_NAME}-{version}.dist-info/METADATA",
            f"Metadata-Version: 2.4\nName: cratis-chronicle-contracts\nVersion: {wheel_version or version}\n\nbody\n",
        )
    with tarfile.open(directory / sdist, "w:gz") as archive:
        with tempfile.TemporaryDirectory() as scratch:
            member = Path(scratch) / "member"
            member.write_text("")
            for path in paths:
                archive.add(member, f"{assets.DISTRIBUTION_NAME}-{version}/src/{assets.DISTRIBUTION_NAME}/{path}")


class TagValidation(unittest.TestCase):
    def test_accepts_exact_stable_tag(self) -> None:
        self.assertEqual(assets.parse_tag("v19.31.2"), "19.31.2")

    def test_rejects_everything_else(self) -> None:
        for bad in (
            "19.31.2",
            "main",
            "v19.31",
            "v19.31.2-rc.1",
            "v19.31.2 ",
            "v01.2.3",
            "v1.2.3\nv1.2.4",
            "v1.2.3; rm -rf /",
            "$(id)",
            "refs/heads/main",
            "v1.2.3/../x",
            "",
        ):
            with self.subTest(bad=bad), self.assertRaises(assets.CheckFailed):
                assets.parse_tag(bad)


class ReleaseChecks(unittest.TestCase):
    release = {"tagName": "v1.2.3", "isDraft": False, "isPrerelease": False, "publishedAt": "2026-01-01T00:00:00Z"}

    def test_passes_for_published_release_at_tag(self) -> None:
        assets.check_release(self.release, "v1.2.3", "abc", "abc")

    def test_rejects_wrong_commit_draft_prerelease_and_other_tag(self) -> None:
        cases = [
            (self.release, "abc", "def"),
            ({**self.release, "isDraft": True}, "abc", "abc"),
            ({**self.release, "isPrerelease": True}, "abc", "abc"),
            ({**self.release, "publishedAt": None}, "abc", "abc"),
            ({**self.release, "tagName": "v1.2.4"}, "abc", "abc"),
        ]
        for release, tag_commit, checkout in cases:
            with self.subTest(release=release, checkout=checkout), self.assertRaises(assets.CheckFailed):
                assets.check_release(release, "v1.2.3", tag_commit, checkout)


class Distributions(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.dist = Path(self.temporary.name)

    def tearDown(self) -> None:
        self.temporary.cleanup()

    def test_accepts_matching_distributions(self) -> None:
        make_dist(self.dist, "1.2.3")
        assets.verify_distributions(self.dist, "1.2.3")

    def test_rejects_version_mismatch(self) -> None:
        make_dist(self.dist, "1.2.3", wheel_version="0.0.0")
        with self.assertRaises(assets.CheckFailed):
            assets.verify_distributions(self.dist, "1.2.3")

    def test_rejects_missing_generated_contract(self) -> None:
        make_dist(self.dist, "1.2.3", omit="eventsources_pb2.py")
        with self.assertRaises(assets.CheckFailed):
            assets.verify_distributions(self.dist, "1.2.3")

    def test_rejects_extra_files(self) -> None:
        make_dist(self.dist, "1.2.3")
        (self.dist / "extra.txt").write_text("x")
        with self.assertRaises(assets.CheckFailed):
            assets.verify_distributions(self.dist, "1.2.3")

    def test_manifest_is_deterministic_and_verifiable(self) -> None:
        make_dist(self.dist, "1.2.3")
        protos = self.dist.parent / f"{self.dist.name}-protos"
        protos.mkdir()
        (protos / "chronicle.desc").write_bytes(b"d")
        first = assets.build_manifest(self.dist, "1.2.3", "v1.2.3", "abc", protos)
        second = assets.build_manifest(self.dist, "1.2.3", "v1.2.3", "abc", protos)
        self.assertEqual(first, second)
        (self.dist / assets.CHECKSUMS_FILE).write_text(first[0])
        assets.verify_checksums(self.dist, "1.2.3")
        (self.dist / assets.expected_files("1.2.3")[0]).write_bytes(b"tampered")
        with self.assertRaises(assets.CheckFailed):
            assets.verify_checksums(self.dist, "1.2.3")

    def test_plan_never_overwrites_a_differing_asset(self) -> None:
        make_dist(self.dist, "1.2.3")
        for name in assets.asset_names("1.2.3")[2:]:
            (self.dist / name).write_text(name)
        digests = {
            n: "sha256:" + hashlib.sha256((self.dist / n).read_bytes()).hexdigest() for n in assets.asset_names("1.2.3")
        }
        wheel = assets.asset_names("1.2.3")[0]

        empty = assets.plan_upload({"assets": []}, self.dist, "1.2.3")
        self.assertEqual(set(empty.values()), {"upload"})

        partial = assets.plan_upload({"assets": [{"name": wheel, "digest": digests[wheel]}]}, self.dist, "1.2.3")
        self.assertEqual(partial[wheel], "skip")

        with self.assertRaises(assets.CheckFailed):
            assets.plan_upload({"assets": [{"name": wheel, "digest": "sha256:0"}]}, self.dist, "1.2.3")
        with self.assertRaises(assets.CheckFailed):
            assets.plan_upload(
                {"assets": [{"name": "cratis_chronicle_contracts-9.9.9.tar.gz", "digest": "x"}]}, self.dist, "1.2.3"
            )


if __name__ == "__main__":
    unittest.main()
