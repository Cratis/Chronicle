# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Require explicit package-write permission for checkout-visible image pushes (#5854)."""

from pathlib import Path
import re
import sys

import yaml

DOCKER_PUSH = re.compile(
    r"\bdocker\s+(?:(?:image\s+)?push\b|buildx\b[^\n;&|]*?\s--push\b)"
)


def pushes_image(step):
    """Recognize build-push-action and direct Docker shell pushes, not registry logins."""
    action = str(step.get("uses", "")).lower()
    if action.startswith("docker/build-push-action@"):
        push = step.get("with", {}).get("push", False)
        return str(push).lower() != "false"
    command = str(step.get("run", "")).replace("\\\n", " ")
    command = "\n".join(line for line in command.splitlines() if not line.lstrip().startswith("#"))
    return DOCKER_PUSH.search(command) is not None


def packages_permission(permissions):
    """Unspecified map entries grant nothing; scalar write-all grants package writes."""
    if isinstance(permissions, dict):
        return permissions.get("packages", "none")
    if permissions == "write-all":
        return "write"
    return "missing" if permissions is None else permissions


def scan(root):
    """Inspect every workflow and count each pushing job once, regardless of step count."""
    paths = sorted((root / ".github/workflows").glob("*.yml"))
    pushing_jobs = 0
    violations = []
    for path in paths:
        workflow = yaml.safe_load(path.read_text(encoding="utf-8"))
        for job_id, job in workflow.get("jobs", {}).items():
            if not any(pushes_image(step) for step in job.get("steps", [])):
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
        print(f"cannot vouch: found {files} workflow files and {pushing_jobs} pushing jobs", file=sys.stderr)
        return 2
    if violations:
        return 1
    print(f"Checked {files} workflow files: {pushing_jobs} pushing jobs; all grant packages: write.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
