# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Fails a pull request whose body would publish badly as release notes.

This repository publishes the pull request body verbatim as the GitHub release body, so the body is
not internal correspondence - it is the release note a consumer reads. Nothing checked that until now:
the only gate on a pull request was the release-intent label, and the result is visible on the
releases page. Measured over the last 400 published releases: 44 carry a heading that is not a
release-note section, 24 end in an agent-tool footer, 4 contain a closing keyword, and 10 declare a
section and then say nothing under it.

The heading rule is an allowlist, never a denylist. A denylist only ever learns the last mistake -
`## Test plan` would have been banned and `## Root cause`, `## Verified`, `## Suggested regression
test` and `## Files affected` would all still have shipped. The allowlist is the Keep a Changelog set
the repository already writes, so anything else has to be deliberate.

Every failure names the offending text and says what to do about it, because this check runs against
prose a human wrote and a message like "invalid body" would just teach people to work around it.
"""

import json
import os
import re
import sys

# The Keep a Changelog sections named by `.ai/rules/pull-requests.md`, plus `Summary`, which the same
# rule sanctions ("include a summary only if there is a cohesive theme across the changes") and which
# 108 of the last 400 releases use - 101 of them alongside a changelog section. The allowlist encodes
# the rule as written; it is not an opportunity to narrow it.
ALLOWED_HEADINGS = ("Summary", "Added", "Changed", "Fixed", "Removed", "Deprecated", "Security")

# Only the changelog sections are required to carry content. `Summary` is prose whose emptiness the
# heading rule already covers, and listing it here would be the same complaint twice.
SECTIONS_REQUIRING_CONTENT = ("Added", "Changed", "Fixed", "Removed", "Deprecated", "Security")

# Markers of an agent transcript or tool footer. These are matched anywhere in the body, unlike the
# heading rule, because they are never legitimate in a release note in any position.
TRANSCRIPT_MARKERS = (
    (r"Generated with \[?Claude Code", "an agent tool footer"),
    (r"Co-Authored-By:\s*Claude", "an agent co-author trailer"),
    (r"\U0001F916", "an agent emoji footer"),
    (r"^\s*Original prompt\s*:?", "a pasted original prompt"),
    (r"<details>", "a collapsed transcript block"),
)

# GitHub closes an issue when the body of a merged pull request says so. In a release note the same
# words are read as a description of the change, and the issue closes as a side effect nobody chose.
CLOSING_KEYWORDS = r"\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s+(?:#\d+|https://github\.com/\S+/issues/\d+)"

HEADING = re.compile(r"^(#{1,6})\s*(.+?)\s*$", re.M)


def sections(body):
    """Yields (level, title, content) for every markdown heading in the body, content up to the next heading."""
    matches = list(HEADING.finditer(body))
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(body)
        yield len(match.group(1)), match.group(2), body[match.end():end]


def check_headings(body):
    """Reports every heading that is not a release-note section, and every section left empty."""
    failures = []
    for _, title, content in sections(body):
        # A heading carrying a link or inline code is prose formatting rather than a section title;
        # judging it against the allowlist would flag legitimate wording. Only bare titles are checked.
        if title not in ALLOWED_HEADINGS and not re.search(r"[`\[\]()]", title):
            failures.append(
                f"::error::The body has a `{title}` heading, which is not a release-note section. "
                f"This body publishes verbatim as the release note, so a reader sees it on the releases page. "
                f"Use one of {', '.join(ALLOWED_HEADINGS)}, or move the content into the pull request as a comment, "
                f"where internal detail belongs and is not published."
            )

        if title in SECTIONS_REQUIRING_CONTENT and not content.strip():
            failures.append(
                f"::error::The `{title}` section is declared but empty, so the release note shows a heading with "
                f"nothing under it. Write the entry or remove the heading."
            )

    return failures


def check_transcripts(body):
    """Reports agent transcript and tool-footer residue anywhere in the body."""
    failures = []
    for pattern, description in TRANSCRIPT_MARKERS:
        if re.search(pattern, body, re.M | re.I):
            failures.append(
                f"::error::The body contains {description}. It publishes verbatim as the release note, "
                f"so this reaches consumers. Remove it."
            )

    return failures


def check_closing_keywords(body):
    """Reports issue-closing keywords, which close issues as a side effect of publishing a note."""
    found = re.findall(CLOSING_KEYWORDS, body, re.I)
    if not found:
        return []

    return [
        f"::error::The body uses the issue-closing keyword `{found[0]}`. Merging would close that issue "
        f"as a side effect of the release note wording rather than as a decision. Reference the issue as "
        f"`(#123)` instead, which links it without closing it."
    ]


class UnreadableBody(Exception):
    """Raised when the body cannot be read at all, as opposed to reading badly."""


def read_body(source):
    """Returns the pull request body, from a GitHub event payload when given one and a plain file otherwise.

    A source that cannot be read is an error in its own right and never an empty body: treating an
    unreadable event payload as "no body" would report a missing description for a pull request that
    has one, and a check whose failure text is wrong is worse than no check.
    """
    try:
        with open(source, encoding="utf-8") as file:
            text = file.read()
    except OSError as error:
        raise UnreadableBody(f"`{source}` could not be read ({error}).") from error

    if not source.endswith(".json"):
        return text

    try:
        payload = json.loads(text)
    except json.JSONDecodeError as error:
        raise UnreadableBody(f"`{source}` is not valid JSON ({error}).") from error

    if not isinstance(payload, dict):
        raise UnreadableBody(f"`{source}` is not a GitHub event payload - its top level is not an object.")

    pull_request = payload.get("pull_request")
    if not isinstance(pull_request, dict):
        raise UnreadableBody(
            f"`{source}` carries no `pull_request` object, so it is not a pull request event. "
            f"This check must run on a `pull_request` trigger."
        )

    return pull_request.get("body") or ""


def main():
    """Reports every way the pull request body would publish badly as a release note."""
    source = sys.argv[1] if len(sys.argv) > 1 else os.environ.get("GITHUB_EVENT_PATH")
    if not source:
        print("::error::No pull request body to check. Pass a file, or run where GITHUB_EVENT_PATH is set.")
        return 1

    try:
        body = read_body(source)
    except UnreadableBody as error:
        print(f"::error::{error}")
        return 1

    if not body.strip():
        print(
            "::error::The pull request body is empty. It publishes verbatim as the release note, so an empty "
            "body ships a release nobody can read. Describe the change under one of "
            f"{', '.join(ALLOWED_HEADINGS)}."
        )
        return 1

    failures = check_headings(body) + check_transcripts(body) + check_closing_keywords(body)
    for failure in failures:
        print(failure)

    if failures:
        print(f"{len(failures)} problem(s) would publish to the release notes. See .ai/rules/pull-requests.md.")
        return 1

    print("Pull request body reads as a release note.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
