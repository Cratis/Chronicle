#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Specs for assert-pull-request-body.py.

The body check decides what reaches the public releases page, so a silent change in what it accepts
is a silent change in what consumers read. These specs pin both directions, because a gate is only
proven by failing when it should: every case below that ends in a failure is drawn from a real
published release, and every case that passes is drawn from the shape the rules ask for.

Run with: python3 .github/scripts/test-assert-pull-request-body.py
"""

import importlib.util
import json
import os
import sys
import tempfile
import unittest

SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(SCRIPT_DIRECTORY, "assert-pull-request-body.py")


def _load():
    """Imports the check by path, since its filename is not an importable module name."""
    spec = importlib.util.spec_from_file_location("assert_pull_request_body", SCRIPT)
    if spec is None or spec.loader is None:
        raise ImportError(f"Could not load the check from `{SCRIPT}` - the specs cannot run without it.")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


check = _load()


def run(body):
    """Runs the check over a body and returns its exit code."""
    with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False, encoding="utf-8") as file:
        file.write(body)
        path = file.name

    try:
        return _main_with(path)
    finally:
        os.unlink(path)


def _main_with(path):
    """Invokes main with argv pointing at the body file."""
    original = sys.argv
    sys.argv = ["assert-pull-request-body.py", path]
    try:
        return check.main()
    finally:
        sys.argv = original


class when_the_body_reads_as_a_release_note(unittest.TestCase):
    """The shapes the rules ask for have to pass, or the check just teaches people to route around it."""

    def test_changelog_sections_pass(self):
        self.assertEqual(run("## Fixed\n\n- Something a consumer sees (#1234)\n"), 0)

    def test_a_summary_before_the_sections_passes(self):
        # pull-requests.md sanctions a summary when there is a cohesive theme, and 108 of the last
        # 400 releases use one. Rejecting it would contradict the rule the check exists to enforce.
        body = "## Summary\n\nOne theme across the change.\n\n## Changed\n\n- A behavior change (#1)\n"
        self.assertEqual(run(body), 0)

    def test_a_bare_issue_reference_passes(self):
        self.assertEqual(run("## Fixed\n\n- A defect, referenced not closed (#3671)\n"), 0)

    def test_prose_headings_carrying_code_or_links_pass(self):
        self.assertEqual(run("## Added\n\n- A thing\n\n### Example \u2014 `.csproj`\n\nDetail.\n"), 0)


class when_the_body_would_publish_internal_detail(unittest.TestCase):
    """Each case is a heading that actually reached the public releases page."""

    def test_a_test_plan_heading_fails(self):
        self.assertEqual(run("## Fixed\n\n- A fix (#1)\n\n## Test plan\n\n- [x] specs pass\n"), 1)

    def test_a_root_cause_heading_fails(self):
        self.assertEqual(run("## Fixed\n\n- A fix (#1)\n\n## Root cause\n\nThe converter.\n"), 1)

    def test_a_files_affected_heading_fails(self):
        self.assertEqual(run("## Changed\n\n- A change (#1)\n\n## Files affected\n\n- a.cs\n"), 1)

    def test_a_near_miss_of_a_real_section_fails(self):
        # `Fixes` and `Fix` both shipped; they are not `Fixed`, and an allowlist is what catches that.
        self.assertEqual(run("## Fixes\n\n- A fix (#1)\n"), 1)


class when_the_body_carries_agent_residue(unittest.TestCase):
    """24 of the last 400 releases end in a tool footer or a transcript block."""

    def test_a_tool_footer_fails(self):
        body = "## Fixed\n\n- A fix (#1)\n\n\U0001F916 Generated with [Claude Code](https://claude.com/claude-code)\n"
        self.assertEqual(run(body), 1)

    def test_a_co_author_trailer_fails(self):
        self.assertEqual(run("## Fixed\n\n- A fix (#1)\n\nCo-Authored-By: Claude <noreply@anthropic.com>\n"), 1)

    def test_a_collapsed_transcript_block_fails(self):
        self.assertEqual(run("## Fixed\n\n- A fix (#1)\n\n<details>\n<summary>prompt</summary>\n</details>\n"), 1)

    def test_an_original_prompt_block_fails(self):
        self.assertEqual(run("## Fixed\n\n- A fix (#1)\n\nOriginal prompt: make the thing work\n"), 1)


class when_the_body_would_close_an_issue(unittest.TestCase):
    """A closing keyword in a release note closes an issue as a side effect of the wording."""

    def test_fixes_hash_fails(self):
        self.assertEqual(run("## Fixed\n\n- Fixes #3671 by correcting the converter\n"), 1)

    def test_closes_hash_fails(self):
        self.assertEqual(run("## Changed\n\n- Closes #123\n"), 1)

    def test_resolves_a_full_issue_url_fails(self):
        body = "## Fixed\n\n- Resolves https://github.com/Cratis/Chronicle/issues/123\n"
        self.assertEqual(run(body), 1)


class when_a_section_is_declared_and_left_empty(unittest.TestCase):
    """pull-requests.md already says not to leave blank headings; 10 releases did anyway."""

    def test_an_empty_changelog_section_fails(self):
        self.assertEqual(run("## Fixed\n\n- A fix (#1)\n\n## Security\n"), 1)

    def test_an_empty_body_fails(self):
        self.assertEqual(run("   \n"), 1)


class when_the_body_cannot_be_read(unittest.TestCase):
    """An unreadable source is its own error and must never be reported as an empty body."""

    def test_a_missing_file_fails(self):
        self.assertEqual(_main_with(os.path.join(tempfile.gettempdir(), "no-such-body.md")), 1)

    def test_a_malformed_event_payload_fails(self):
        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as file:
            file.write("{not json")
            path = file.name

        try:
            self.assertEqual(_main_with(path), 1)
        finally:
            os.unlink(path)

    def test_an_event_payload_without_a_pull_request_fails(self):
        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as file:
            json.dump({"issue": {}}, file)
            path = file.name

        try:
            self.assertEqual(_main_with(path), 1)
        finally:
            os.unlink(path)

    def test_a_pull_request_event_payload_is_read(self):
        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as file:
            json.dump({"pull_request": {"body": "## Fixed\n\n- A fix (#1)\n"}}, file)
            path = file.name

        try:
            self.assertEqual(_main_with(path), 0)
        finally:
            os.unlink(path)


if __name__ == "__main__":
    unittest.main(verbosity=2)
