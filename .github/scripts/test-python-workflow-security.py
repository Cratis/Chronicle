#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Security guards for the two Python workflows, including in-memory mutation tests.

The reader deliberately accepts only the existing block-style security sections.
Unsupported layouts fail closed rather than silently skipping a policy check.
"""

import re
import unittest
from pathlib import Path

WORKFLOWS = Path(__file__).resolve().parents[1] / "workflows"
SHA = r"[0-9a-fA-F]{40}"


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def section(text, key, indent):
    """Read one uniquely named block mapping entry, excluding comments/blank lines."""
    lines = text.splitlines()
    pattern = re.compile(rf"^{' ' * indent}{re.escape(key)}:(.*)$")
    matches = [(i, match) for i, line in enumerate(lines) if (match := pattern.match(line))]
    require(len(matches) == 1, f"Expected exactly one {key} at indent {indent}")
    start, match = matches[0]
    result = [match.group(1).strip()]
    for line in lines[start + 1 :]:
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if len(line) - len(line.lstrip()) <= indent:
            break
        result.append(line)
    return "\n".join(result).strip("\n")


def mapping_keys(text, indent):
    return re.findall(rf"^{' ' * indent}([\w-]+):", text, re.MULTILINE)


def steps(job):
    block = section(job, "steps", 4)
    result = re.split(r"(?m)^      - ", block)
    require(not result[0].strip() and len(result) > 1, "Expected block-style steps")
    return ["        " + step for step in result[1:]]


def step_field(step, key):
    return section(step, key, 8)


def check_common(text, expected_jobs):
    require(section(text, "permissions", 0) == "{}", "Top-level permissions must be empty")
    jobs = section(text, "jobs", 0)
    require(mapping_keys("\n" + jobs, 2) == expected_jobs, "Unexpected or missing jobs")
    uses = re.findall(r"(?m)^\s*(?:- )?uses:\s*(\S+)", text)
    require(uses, "No actions found")
    require(all(re.fullmatch(rf"[\w./-]+@{SHA}", use) for use in uses), "Actions must be SHA-pinned")
    require(not re.search(r"(?m)^\s*(pull_request_target|workflow_run)\s*:", text), "Unsafe trigger")
    for name in expected_jobs:
        job = section(jobs, name, 2)
        require(re.fullmatch(r"[1-9][0-9]*", section(job, "timeout-minutes", 4)), "Missing timeout")
        steps(job)
    return jobs


def check_verify(text):
    jobs = check_common(text, ["verify"])
    verify = section(jobs, "verify", 2)
    require(section(verify, "permissions", 4).strip() == "contents: read", "Verify permissions changed")


def check_publish(text):
    jobs = check_common(text, ["build", "publish"])
    build = section(jobs, "build", 2)
    publish = section(jobs, "publish", 2)
    require(section(build, "permissions", 4).strip() == "contents: read", "Build permissions changed")
    require(not re.search(r"\b(id-token|environment|secrets)\b", build), "Build credential boundary changed")
    require(section(publish, "permissions", 4).strip() == "id-token: write", "Publish permissions changed")
    require(section(publish, "environment", 4) == "pypi-python-contracts", "Publish environment changed")
    require(section(publish, "needs", 4) == "build", "Publish must depend on build")
    publish_steps = steps(publish)
    require(len(publish_steps) == 2, "Expected exactly two publish steps")
    for step, action in zip(publish_steps, ["actions/download-artifact", "pypa/gh-action-pypi-publish"]):
        require(set(mapping_keys(step, 8)) <= {"name", "uses", "with"}, "Executable publish step")
        require(step_field(step, "uses").split("@", 1)[0] == action, "Unexpected publish action")
    build_steps = steps(build)
    require(step_field(build_steps[0], "name") == "Validate version format", "Validate version before checkout")
    require(step_field(build_steps[0], "env").strip() == "VERSION: ${{ inputs.version }}", "Version must come from env")
    require(any("actions/checkout@" in step for step in build_steps[1:]), "Missing checkout after validation")
    for step in build_steps + publish_steps:
        if "run" in mapping_keys(step, 8):
            script = step_field(step, "run")
            require(not re.search(r"\$\{\{\s*(inputs\.|github\.event\.inputs\.)", script), "Input interpolated into shell")
    script = step_field(build_steps[0], "run")
    matches = re.findall(r'if ! \[\[ "\$VERSION" =~ (\S+) \]\]; then', script)
    require(len(matches) == 1 and "exit 1" in script, "Missing rejecting version validation")
    return matches[0]


class WorkflowSecurity(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.verify = (WORKFLOWS / "python-contracts.yml").read_text()
        cls.publish = (WORKFLOWS / "publish-python-client.yml").read_text()

    def test_two_repository_workflows(self):
        check_verify(self.verify)
        check_publish(self.publish)
        print("Checked 2 Python workflows (verify, build, publish jobs)")

    def test_version_regex_behavior(self):
        pattern = check_publish(self.publish)
        for value in ("19.7.1", "9.8.0-beta.3", "1.0.0rc1"):
            with self.subTest(value=value):
                self.assertIsNotNone(re.fullmatch(pattern, value))
        for value in ("v19.7.1", "1.0", "1.0.0; rm -rf /", "$(id)", "1.0.0\n1.0.1", ""):
            with self.subTest(value=value):
                self.assertIsNone(re.fullmatch(pattern, value))

    def test_mutated_workflows_are_rejected(self):
        mutations = [
            (check_verify, self.verify, "permissions: {}", "permissions: read-all"),
            (check_verify, self.verify, "contents: read", "contents: write"),
            (check_verify, self.verify, "timeout-minutes: 30", "missing-timeout: 30"),
            (check_verify, self.verify, "pull_request:", "pull_request_target:"),
            (check_verify, self.verify, "pull_request:", "workflow_run:"),
            (check_verify, self.verify, "verify:", "other:"),
            (check_publish, self.publish, "permissions: {}", "permissions: write-all"),
            (check_publish, self.publish, "contents: read", "contents: read\n      id-token: write"),
            (check_publish, self.publish, "    steps:", "    environment: unsafe\n    steps:"),
            (check_publish, self.publish, "run: python build_package.py", "run: echo ${{ secrets.TOKEN }}"),
            (check_publish, self.publish, "id-token: write", "id-token: read"),
            (check_publish, self.publish, "environment: pypi-python-contracts", "environment: other"),
            (check_publish, self.publish, "needs: build", "needs: other"),
            (check_publish, self.publish, "timeout-minutes: 10", "missing-timeout: 10"),
            (check_publish, self.publish, "timeout-minutes: 30", "missing-timeout: 30"),
            (check_publish, self.publish, "name: Validate version format", "name: No validation"),
            (check_publish, self.publish, '"$VERSION" =~', '"literal" =~'),
            (check_publish, self.publish, "VERSION: ${{ inputs.version }}", "VERSION: other"),
            (check_publish, self.publish, "run: python build_package.py", "run: echo ${{ inputs.version }}"),
            (check_publish, self.publish, "run: python build_package.py", "run: echo ${{ github.event.inputs.version }}"),
            (check_publish, self.publish, "name: Publish distributions to PyPI", "run: echo unsafe\n        name: Publish distributions to PyPI"),
            (check_publish, self.publish, "pypa/gh-action-pypi-publish@", "actions/checkout@"),
        ]
        for check, original in ((check_verify, self.verify), (check_publish, self.publish)):
            checkout = re.search(rf"actions/checkout@{SHA}", original)
            self.assertIsNotNone(checkout)
            mutations.append((check, original, checkout[0], "actions/checkout@v7"))
        for check, original, before, after in mutations:
            with self.subTest(before=before, after=after):
                self.assertIn(before, original)
                mutated = original.replace(before, after, 1)
                self.assertNotEqual(mutated, original)
                with self.assertRaises(AssertionError):
                    check(mutated)


if __name__ == "__main__":
    unittest.main()
