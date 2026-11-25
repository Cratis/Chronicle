# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Require package-write permission for GHCR pushes using the workflow token (#5854).

This is a best-effort lint, not a security boundary: it inspects the steps of workflow
files only, so pushes inside composite actions or called reusable workflows, and
unusual spellings of the login or push commands, are not detected.
"""

from pathlib import Path
import re
import sys

import yaml

DOCKER_PUSH = re.compile(
    r"\bdocker\s+(?:(?:image\s+)?push\b|buildx\b[^\n;&|]*?\s--push\b)"
)
GHCR_LOGIN = re.compile(
    r"\bdocker\s+login\b[^\n;&|]*?\s(?:https?://)?ghcr\.io(?=[\s/]|$)", re.IGNORECASE
)
WORKFLOW_TOKEN = re.compile(r"\b(?:github\.token|secrets\.GITHUB_TOKEN)\b", re.IGNORECASE)
SHELL_TOKEN = re.compile(r"\b(?:GITHUB_TOKEN|github\.token)\b", re.IGNORECASE)


def shell_command(step):
    """Join continued shell lines and ignore full-line comments."""
    command = str(step.get("run", "")).replace("\\\n", " ")
    return "\n".join(line for line in command.splitlines() if not line.lstrip().startswith("#"))


def authenticates_with_workflow_token(step):
    """Only GHCR logins using the workflow token need package-write permission."""
    action = str(step.get("uses", "")).lower()
    if action.startswith("docker/login-action@"):
        inputs = step.get("with", {})
        registry = str(inputs.get("registry", "")).strip().lower()
        return registry == "ghcr.io" and WORKFLOW_TOKEN.search(str(inputs.get("password", ""))) is not None
    command = shell_command(step)
    return GHCR_LOGIN.search(command) is not None and SHELL_TOKEN.search(command) is not None


def pushes_image(step):
    """Recognize build-push-action and direct Docker shell pushes, not registry logins."""
    action = str(step.get("uses", "")).lower()
    if action.startswith("docker/build-push-action@"):
        push = step.get("with", {}).get("push", False)
        return str(push).lower() != "false"
    return DOCKER_PUSH.search(shell_command(step)) is not None


def packages_permission(permissions):
    """Unspecified map entries grant nothing; scalar write-all grants package writes."""
    if isinstance(permissions, dict):
        return permissions.get("packages", "none")
    if permissions == "write-all":
        return "write"
    return "missing" if permissions is None else permissions


def scan(root):
    """Inspect every workflow and count each token-authenticated GHCR pushing job once."""
    workflows = root / ".github/workflows"
    paths = sorted([*workflows.glob("*.yml"), *workflows.glob("*.yaml")])
    pushing_jobs = 0
    violations = []
    for path in paths:
        workflow = yaml.safe_load(path.read_text(encoding="utf-8"))
        for job_id, job in workflow.get("jobs", {}).items():
            steps = job.get("steps", [])
            if not any(pushes_image(step) for step in steps):
                continue
            if not any(authenticates_with_workflow_token(step) for step in steps):
                continue
            pushing_jobs += 1
            permission = packages_permission(job.get("permissions", workflow.get("permissions")))
            if permission != "write":
                violations.append(f"{path.name}: job {job_id} pushes an image but packages permission is {permission}")
    return len(paths), pushing_jobs, violations


def main(argv):
    """Return 0 for a verified checkout, 1 for violations, or 2 when unable to vouch."""
    root = Path(argv[1]) if len(argv) > 1 else Path(__file__).resolve().parents[2]
    try:
        files, pushing_jobs, violations = scan(root)
    except (OSError, UnicodeError, yaml.YAMLError) as error:
        print(f"cannot vouch: {error}", file=sys.stderr)
        return 2
    for violation in violations:
        print(violation, file=sys.stderr)
    if not files or not pushing_jobs:
        print(f"cannot vouch: found {files} workflow files and {pushing_jobs} in-scope pushing jobs", file=sys.stderr)
        return 2
    if violations:
        return 1
    print(f"Checked {files} workflow files: {pushing_jobs} in-scope pushing jobs; all grant packages: write.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
