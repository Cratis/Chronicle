# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Pure, fail-closed evidence rules for reusing an executed requested matrix."""
import fnmatch
import hashlib
import re
from typing import Callable, NamedTuple

# Deliberate over-inclusion: a configuration change costs a run, never coverage.
CONFIG_PATHSPECS = (
    ".github/**", "global.json", "Directory.Packages.props", "Chronicle.slnx", "specs.runsettings",
    ".editorconfig", ".globalconfig", "**/Directory.Build.*", "**/.editorconfig", "**/*.props",
    "**/*.targets", "**/nuget.config", "**/package.json", "**/package-lock.json", "**/yarn.lock",
    "**/pnpm-lock.yaml", "**/*.globalconfig",
)
WORKFLOW_TITLES = {
    ".github/workflows/hot-core-gate.yml": "Hot Core Gate",
    ".github/workflows/requested-integration.yml": "Requested Integration",
}
GATE_JOB = "hot-core-gate-requested-run-integration"
MATRIX_JOB = "requested-integration-run-integration"
LANE_JOB = re.compile(r"(?:integration|integration-api|mongodb|neo4j)(?: \([^\n]+\))?\Z")
RECORDED_TITLE = re.compile(r"(?P<workflow>Hot Core Gate|Requested Integration) - PR #(?P<pr>[1-9][0-9]*) @ (?P<head>[0-9a-f]{40}) base (?P<base>[0-9a-f]{40})")


def config_path(path):
    """Match root and nested config alike; NuGet config names are case-insensitive."""
    lowered = path.lower()
    patterns = [pattern.lower() for pattern in CONFIG_PATHSPECS]
    return any(fnmatch.fnmatchcase(lowered, pattern) or
               fnmatch.fnmatchcase(lowered, pattern.removeprefix("**/")) for pattern in patterns)


def config_fingerprint(commit, run):
    """Hash mode/blob/path records, not checkout bytes. ls-tree has no glob pathspec support."""
    code, output, _ = run("git", "ls-tree", "-rz", "--full-tree", commit)
    if code:
        return None
    records = [record for record in output.split("\0") if "\t" in record and config_path(record.split("\t", 1)[1])]
    if not records:
        return None
    return hashlib.sha256("\0".join(sorted(records)).encode("utf-8")).hexdigest()


def passed_gate(gates):
    return len(gates) == 1 and gates[0].get("conclusion") == "success"


def clean_matrix(jobs):
    return all(job.get("conclusion") in ("success", "skipped") for job in jobs)


def successful_lane(jobs):
    return any(LANE_JOB.fullmatch(job["name"].split(" / ")[-1]) and job.get("conclusion") == "success" for job in jobs)


def matrix_executed(jobs):
    """A green aggregate alone is not evidence: at least one actual lane must have run."""
    gates = [job for job in jobs if job.get("name", "").split(" / ")[-1] == GATE_JOB]
    nested = [job for job in jobs if MATRIX_JOB in job.get("name", "").split(" / ")[:-1]]
    return passed_gate(gates) and clean_matrix(nested) and successful_lane(nested)


def recorded_refusal(candidate, current):
    title = RECORDED_TITLE.fullmatch(candidate.get("display_title", ""))
    if not title:
        return "missing or malformed immutable tested-base record"
    expected = (WORKFLOW_TITLES.get(candidate.get("path")), str(current.get("pull_request")),
                candidate.get("head_sha"), current.get("base"))
    if (title["workflow"], title["pr"], title["head"], title["base"]) != expected:
        return "recorded workflow, PR, head or tested base differs"
    return None


def same_repository(candidate, current):
    repo = current.get("repo")
    source_repo = (candidate.get("head_repository") or {}).get("full_name")
    return bool(repo) and current.get("head_repo") == repo and source_repo == repo


def source_identity_refusal(candidate, current):
    if candidate.get("path") not in WORKFLOW_TITLES or candidate.get("conclusion") != "success":
        return "source workflow is not an allowed successful run"
    if not str(candidate.get("id", "")).isdecimal() or not candidate.get("head_sha"):
        return "source run identity is missing"
    if candidate["head_sha"] == current.get("head") or not candidate.get("has_matrix_job"):
        return "source must be an earlier matrix-executing run (no chaining)"
    return None


def source_run_refusal(candidate, current):
    """Never use mutable pull_requests[].base.sha as evidence of the tested merge."""
    if current.get("event") != "pull_request" or candidate.get("event") != "pull_request":
        return "reuse requires pull_request events"
    if not same_repository(candidate, current):
        return "same-repository PR evidence required"
    return source_identity_refusal(candidate, current) or recorded_refusal(candidate, current)


class ReuseEvidence(NamedTuple):
    fingerprint: Callable[[str], str | None]
    files_between: Callable[[str, str], list[str] | None]
    classify_delta: Callable[[list[str]], str]
    ancestor: Callable[[str, str], bool]


def same_config(old, head, evidence):
    source_config = evidence.fingerprint(old)
    return bool(source_config) and source_config == evidence.fingerprint(head)


def non_reaching_delta(old, head, evidence):
    files = evidence.files_between(old, head)
    return files is not None and evidence.classify_delta(files) == "skip"


def history_refusal(old, head, evidence):
    """Each check is evaluated in order; the first failure refuses."""
    checks = ((same_config, "config fingerprint changed or unavailable"),
              (non_reaching_delta, "delta is not proven non-reaching"),
              (lambda o, h, e: e.ancestor(o, h), "source head is missing or not an ancestor"))
    return next((reason for check, reason in checks if not check(old, head, evidence)), None)


def reuse_decision(candidate, current, evidence):
    """Dependencies are injected; any unavailable evidence refuses, not an empty guess."""
    result = dict(state="refused", run_id=candidate.get("id"), head_sha=candidate.get("head_sha"),
                  fingerprint=None, reasons=[])
    reason = source_run_refusal(candidate, current) or history_refusal(candidate["head_sha"], current["head"], evidence)
    if reason:
        return result | dict(reasons=[reason])
    return result | dict(state="reused", fingerprint=evidence.fingerprint(current["head"]))
