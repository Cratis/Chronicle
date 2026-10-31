#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Security guards for the two Python workflows, including in-memory mutation tests.

The reader deliberately accepts only the existing block-style security sections.
Unsupported layouts fail closed rather than silently skipping a policy check.
"""

import re
import subprocess
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


def plain_entry(line, indent):
    content = line[indent:]
    if content.startswith("- "):
        content = content[2:]
    entry = re.fullmatch(r"([A-Za-z_][\w-]*):(?: (.*))?", content)
    if entry:
        key, value = entry.groups()
        value = (value or "").strip()
        return key, value if key == "run" else value.split(" #", 1)[0].strip()
    require(line[indent:].startswith("- "), "Expected a plain YAML key or scalar list item")
    return None, content


def check_scalar(value):
    if not value or value == "{}":
        return
    if value.startswith("'"):
        require(re.fullmatch(r"'(?:[^']|'')*'", value), "Unsupported quoted scalar or key")
    elif value.startswith('"'):
        require(re.fullmatch(r'"(?:[^"\\]|\\.)*"', value), "Unsupported quoted scalar or key")
    elif value.startswith("${{"):
        require(value.endswith("}}"), "Unsupported multiline expression")
    elif value.startswith("["):
        require(re.fullmatch(r'\[[A-Za-z0-9" .,\-_]+\]', value), "Unsupported flow collection")
    else:
        require(value[0] not in "&*!>|{[", "Unsupported YAML scalar")
        require(not value.startswith(("---", "...")), "Unsupported YAML document")
        require(": " not in value, "Unsupported inline YAML mapping")


def check_run_scalar(value):
    """Never decode YAML or parse Actions expressions inside executable source."""
    require("${{" not in value, "Run source must not interpolate Actions expressions")
    if value == "|":
        return
    require(value and value[0] not in "\"'&*!>|{[?%@`", "Run must be literal block or plain single-line source")
    check_scalar(value)


def check_entry(key, value):
    require(key != "defaults", "Shell defaults are not supported")
    if key == "run":
        check_run_scalar(value)
    else:
        check_scalar(value)


def check_block_grammar(text):
    """Fail closed on YAML syntax the block reader cannot interpret.

    Only run: | may introduce literal content; shell lines inside that block
    are data, not YAML. Elsewhere require plain keys and simple scalar values.
    This intentionally excludes quoted keys, flow mappings, tags and aliases.
    """
    run_indent = None
    literal_run = False
    for line in text.splitlines():
        indent = len(line) - len(line.lstrip(" "))
        if run_indent is not None and indent > run_indent:
            require(literal_run, "Plain run source must stay on one line")
            require("${{" not in line, "Run source must not interpolate Actions expressions")
            continue
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        run_indent = None
        require("\t" not in line[:indent + 1] and indent % 2 == 0, "Unsupported YAML indentation")
        key, value = plain_entry(line, indent)
        check_entry(key, value)
        if key == "run":
            run_indent = indent + (2 if line[indent:].startswith("- ") else 0)
            literal_run = value == "|"


def check_common(text, expected_jobs):
    check_block_grammar(text)
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
    require("continue-on-error" not in mapping_keys(build, 4), "Build failure must be fatal")
    build_steps = steps(build)
    require(set(mapping_keys(build_steps[0], 8)) == {"name", "env", "run"}, "Validation must be unconditional and fatal")
    require(step_field(build_steps[0], "name") == "Validate version format", "Validate version before checkout")
    require(step_field(build_steps[0], "env").strip() == "VERSION: ${{ inputs.version }}", "Version must come from env")
    require(any("actions/checkout@" in step for step in build_steps[1:]), "Missing checkout after validation")
    script = step_field(build_steps[0], "run")
    matches = re.findall(r'if ! \[\[ "\$VERSION" =~ (\S+) \]\]; then', script)
    require(len(matches) == 1, "Missing rejecting version validation")
    pattern = matches[0]
    require(pattern.startswith("^") and pattern.endswith("$"), "Version regex must be anchored")
    expected = (
        "|\n"
        f'          if ! [[ "$VERSION" =~ {pattern} ]]; then\n'
        '            echo "::error::Version \'$VERSION\' is not a valid release version"\n'
        "            exit 1\n"
        "          fi"
    )
    require(script == expected, "Validation must unconditionally reject invalid versions")
    return pattern


def bash_matches(pattern, value):
    """Pass both operands as data, never interpolate them into Bash source."""
    result = subprocess.run(
        ["bash", "--noprofile", "--norc", "-c", '[[ "$1" =~ $2 ]]', "version-check", value, pattern],
        check=False,
        capture_output=True,
        text=True,
    )
    require(result.returncode in (0, 1), "Bash regex evaluation failed")
    return result.returncode == 0


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
                self.assertTrue(bash_matches(pattern, value))
        for value in ("v19.7.1", "1.0", "1.0.0; rm -rf /", "$(id)", "1.0.0\n1.0.1", ""):
            with self.subTest(value=value):
                self.assertFalse(bash_matches(pattern, value))

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
        self.assert_mutations_rejected(mutations)

    def assert_mutations_rejected(self, mutations):
        for check, original, before, after in mutations:
            with self.subTest(before=before, after=after):
                self.assertIn(before, original)
                mutated = original.replace(before, after, 1)
                self.assertNotEqual(mutated, original)
                with self.assertRaises(AssertionError):
                    check(mutated)
        print(f"Rejected {len(mutations)} in-memory mutations")

    def test_unsupported_yaml_is_rejected(self):
        mutations = [
            ("pull_request:", '\"pull_request_target\":'),
            ("pull_request:", "'workflow_run':"),
            ("    steps:", '    steps:\n      - "uses": actions/checkout@v7'),
            ("    steps:", "    steps:\n      - 'uses': actions/checkout@v7"),
            ("    steps:", "    steps:\n      - {uses: actions/checkout@v7}"),
            ("    steps:", "    steps: &steps"),
            ("    steps:", "    steps: *steps"),
            ("    steps:", "    steps: !tag"),
            ("    steps:", "    steps: >"),
        ]
        self.assert_mutations_rejected([(check_verify, self.verify, a, b) for a, b in mutations])

    def test_input_expression_variants_are_rejected(self):
        expressions = [
            "inputs['version']",
            "github.event.inputs['version']",
            "github['event']['inputs']['version']",
            "github.event['inputs'].version",
            "format('{0}', inputs.version)",
            "false || inputs.version",
            "INPUTS.version",
            "github.event[format('in{0}', 'puts')].version",
        ]
        self.assert_mutations_rejected([
            (check_publish, self.publish, "run: python build_package.py", f"run: echo ${{{{ {expression} }}}}")
            for expression in expressions
        ])

    def test_validation_cannot_be_disabled(self):
        mutations = [
            ("name: Validate version format", "name: Validate version format\n        if: false"),
            ("name: Validate version format", "name: Validate version format\n        continue-on-error: true"),
            ("name: Validate version format", "name: Validate version format\n        shell: sh"),
            ("            exit 1", "            echo ignored"),
            ("            exit 1", "            # exit 1"),
            ("            exit 1", "            exit 0"),
            ("            exit 1", "            if false; then exit 1; fi"),
            ("=~ ^[0-9]", "=~ [0-9]"),
            ("[0-9]+)?$ ]];", "[0-9]+)? ]];"),
            ("    steps:", "    continue-on-error: true\n    steps:"),
            ("jobs:", "defaults:\n  run:\n    shell: sh\n\njobs:"),
        ]
        self.assert_mutations_rejected([(check_publish, self.publish, a, b) for a, b in mutations])

    def test_run_source_forms_are_rejected(self):
        sources = [
            r'"echo ${{ \u0069nputs.version }}"',
            "echo ${{ format('{0}}}', inputs.version) }}",
            r'"echo \u0024\u007b\u007b inputs.version }}"',
            "'echo safe'",
            '"echo safe"',
            r'"echo safe\necho more"',
            ">\n          echo safe",
            "*script",
            "&script echo safe",
            "!tag echo safe",
            "",
            "echo safe\n          echo continuation",
            "echo safe\n          - echo ${{ inputs.version }}",
            "|\n          echo ${{ env.VERSION }}",
            "|\n          # ${{ inputs.version }}",
            "echo safe # ${{ inputs.version }}",
            "echo ${{ env.VERSION }}",
        ]
        mutations = []
        for source in sources:
            mutations.append((check_publish, self.publish, "run: python build_package.py", f"run: {source}"))
            mutations.append((check_verify, self.verify, "    steps:", f"    steps:\n      - run: {source}"))
        self.assert_mutations_rejected(mutations)

    def test_plain_and_literal_run_sources_remain_supported(self):
        for source in ("echo safe", "|\n          echo safe\n          echo more"):
            with self.subTest(source=source):
                alternate = self.verify.replace("    steps:", f"    steps:\n      - run: {source}", 1)
                self.assertNotEqual(alternate, self.verify)
                check_verify(alternate)
                build = self.publish.replace("run: python build_package.py", f"run: {source}", 1)
                self.assertNotEqual(build, self.publish)
                check_publish(build)

    def test_shell_content_is_not_parsed_as_yaml(self):
        script = self.verify.replace(
            '          python -m pip install --upgrade pip',
            '          echo \'"uses": actions/checkout@v7\'\n'
            '          echo \'&anchor {key: value} > text\'\n'
            '          python -m pip install --upgrade pip',
            1,
        )
        self.assertNotEqual(script, self.verify)
        check_verify(script)


if __name__ == "__main__":
    unittest.main()
