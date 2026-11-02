# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Bounded, unsigned scheduled-main coverage transport. Payloads are data, never code.

GitHub/API identity and checksums are not independent signatures. Producer cache
isolation and real writer permissions still require separately authorized proof.
"""
import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
import math
import os
from pathlib import Path
import re
import selectors
import stat
import subprocess
import sys
import time
import zipfile

REPO = "/repos/Cratis/Chronicle"
WORKFLOW = REPO + "/actions/workflows/dotnet-build.yml"
RUNS = WORKFLOW + "/runs?event=schedule&branch=main&status=success&per_page=30"
WORKFLOW_PATH = ".github/workflows/dotnet-build.yml"
SCOPE = "dotnet-build/coverage-merge-and-cache-v1"
MIB = 1024 * 1024
ZIP_LIMIT = 16 * MIB
JSON_LIMIT = 2 * MIB
SUMMARY_LIMIT = 32 * MIB
SIDECAR_LIMIT = 64 * 1024
SKEW = 300000
SHA = re.compile(r"[0-9a-f]{40}\Z")
DIGEST = re.compile(r"sha256:[0-9a-f]{64}\Z")
PROVENANCE_KEYS = {"schemaVersion", "repository", "repositoryId", "workflowPath", "workflowId", "event", "ref",
                   "runId", "runAttempt", "headSha", "measuredAt", "summarySha256", "scope"}
SOURCE_KEYS = PROVENANCE_KEYS | {"artifactId", "artifactName", "artifactDigest", "artifactSize", "artifactCreatedAt",
                                 "artifactExpiresAt", "runCreatedAt", "runUpdatedAt"}
RUN_FIELDS = ("id", "run_attempt", "workflow_id", "path", "event", "head_branch", "head_sha", "status",
              "conclusion", "created_at", "updated_at", "repository", "head_repository")
ARTIFACT_FIELDS = ("id", "name", "size_in_bytes", "digest", "expired", "created_at", "expires_at", "workflow_run")


class Rejected(Exception):
    """Missing or inconsistent evidence; never rescue it with an older producer."""


class Missing(Rejected):
    """No exact producer within the bounded discovery window."""


class Unavailable(Exception):
    """Transport/tooling could not run, rather than an empty feed."""


def require(condition, reason):
    if not condition:
        raise Rejected(reason)


def integer(value, minimum=1):
    return type(value) is int and minimum <= value <= 8640000000000000


def timestamp(value):
    require(isinstance(value, str) and re.fullmatch(r"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?Z", value),
            "missing or invalid UTC timestamp")
    try:
        return int(datetime.fromisoformat(value.replace("Z", "+00:00")).timestamp() * 1000)
    except ValueError as error:
        raise Rejected("invalid UTC calendar timestamp") from error


def parse_object(data):
    def constant(_):
        raise ValueError("nonfinite JSON")
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError("duplicate JSON key")
            result[key] = value
        return result
    try:
        value = json.loads(data, parse_constant=constant, object_pairs_hook=pairs)
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Rejected("invalid JSON object") from error
    require(type(value) is dict, "expected one JSON object")
    return value


def bounded_command(command, limit, timeout):
    """Drain only bounded stdout; do not retain credentials/redirect URLs from stderr."""
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    chunks, size = [], 0
    deadline = time.monotonic() + timeout
    try:
        with selectors.DefaultSelector() as selector:
            selector.register(process.stdout, selectors.EVENT_READ)
            while True:
                remaining = deadline - time.monotonic()
                if remaining <= 0 or not selector.select(remaining):
                    raise Unavailable("GitHub API timeout")
                chunk = os.read(process.stdout.fileno(), 65536)
                if not chunk:
                    break
                size += len(chunk)
                if size > limit:
                    raise Unavailable("GitHub response exceeds byte limit")
                chunks.append(chunk)
        if process.wait(timeout=max(0.001, deadline - time.monotonic())) != 0:
            raise Unavailable("GitHub API unavailable (authentication, permissions, rate limit or endpoint failure)")
        return b"".join(chunks)
    finally:
        if process.poll() is None:
            process.kill()
        process.wait()
        process.stdout.close()


class GitHub:
    def __init__(self, runner=bounded_command):
        self.runner = runner

    def request(self, path, limit):
        require(path.startswith(REPO) and ".." not in path, "noncanonical API path")
        try:
            payload = self.runner(["gh", "api", "--method", "GET", path], limit, 60)
            if len(payload) > limit:
                raise Unavailable("GitHub response exceeds byte limit")
            return payload
        except (OSError, subprocess.SubprocessError) as error:
            raise Unavailable("GitHub API tool unavailable or timed out") from error

    def json(self, path):
        try:
            return parse_object(self.request(path, JSON_LIMIT))
        except Rejected as error:
            raise Unavailable("GitHub API returned malformed JSON") from error

    def archive(self, path):
        return self.request(path, ZIP_LIMIT)


def canonical(api):
    repository, workflow = api.json(REPO), api.json(WORKFLOW)
    require(repository.get("full_name") == "Cratis/Chronicle" and repository.get("default_branch") == "main"
            and integer(repository.get("id")), "canonical main repository unavailable")
    require(workflow.get("path") == WORKFLOW_PATH and workflow.get("state") == "active"
            and integer(workflow.get("id")), "canonical active workflow unavailable")
    return repository["id"], workflow["id"]


def validate_run(run, repo_id, workflow_id, completed=True):
    require(type(run) is dict, "invalid run object")
    require(all(field in run for field in RUN_FIELDS), "missing required run field")
    require(integer(run["id"]) and integer(run["run_attempt"]) and type(run["head_sha"]) is str
            and SHA.fullmatch(run["head_sha"]), "invalid run identity")
    require(type(run["workflow_id"]) is int and run["workflow_id"] == workflow_id and run["path"] == WORKFLOW_PATH
            and run["event"] == "schedule" and run["head_branch"] == "main", "untrusted run workflow/event/branch")
    for field in ("repository", "head_repository"):
        require(type(run[field]) is dict and type(run[field].get("id")) is int and run[field]["id"] == repo_id,
                "untrusted run repository")
    if completed:
        require(run["status"] == "completed" and run["conclusion"] == "success", "run did not complete successfully")
    timestamp(run["created_at"])
    timestamp(run["updated_at"])


def artifact_name(run):
    return f"coverage-nightly-v1-{run['id']}-{run['run_attempt']}"


def validate_artifact(artifact, run, repo_id, now):
    require(type(artifact) is dict and all(field in artifact for field in ARTIFACT_FIELDS), "missing artifact metadata")
    require(integer(artifact["id"]) and artifact["name"] == artifact_name(run), "invalid artifact identity")
    require(type(artifact["size_in_bytes"]) is int and 0 < artifact["size_in_bytes"] <= ZIP_LIMIT, "artifact size exceeds limit")
    require(type(artifact["digest"]) is str and DIGEST.fullmatch(artifact["digest"]), "missing supported artifact digest")
    require(artifact["expired"] is False and timestamp(artifact["expires_at"]) > now, "artifact expired")
    created = timestamp(artifact["created_at"])
    require(timestamp(run["created_at"]) - SKEW <= created <= now + SKEW
            and created < timestamp(artifact["expires_at"])
            and timestamp(run["created_at"]) <= timestamp(run["updated_at"]) <= now + SKEW,
            "invalid artifact/run creation time")
    link = artifact["workflow_run"]
    expected = {"id": run["id"], "repository_id": repo_id, "head_repository_id": repo_id,
                "head_branch": "main", "head_sha": run["head_sha"]}
    require(type(link) is dict and all(type(link.get(k)) is type(v) and link.get(k) == v for k, v in expected.items()),
            "artifact run/head linkage mismatch")


def production_assemblies(report):
    coverage = report.get("coverage")
    require(type(coverage) is dict and type(coverage.get("assemblies")) is list, "missing assembly coverage")
    assemblies = []
    for assembly in coverage["assemblies"]:
        require(type(assembly) is dict and isinstance(assembly.get("name"), str) and 0 < len(assembly["name"]) <= 512,
                "invalid assembly name")
        value = assembly.get("coverage")
        require(type(value) in (int, float) and math.isfinite(value) and 0 <= value <= 100, "invalid coverage percentage")
        name = assembly["name"]
        if not name.endswith(".Specs") and not name.startswith(("xunit", "NSubstitute", "testhost", "coverlet")):
            assemblies.append(assembly)
    require(bool(assemblies), "no production assemblies in Summary.json")
    return assemblies


def validate_summary(data):
    require(len(data) <= SUMMARY_LIMIT, "Summary.json exceeds limit")
    report = parse_object(data)
    production_assemblies(report)
    return hashlib.sha256(data).hexdigest()


def provenance(run, repo_id, workflow_id, measured_at, digest):
    return {"schemaVersion": 1, "repository": "Cratis/Chronicle", "repositoryId": repo_id,
            "workflowPath": WORKFLOW_PATH, "workflowId": workflow_id, "event": "schedule", "ref": "refs/heads/main",
            "runId": run["id"], "runAttempt": run["run_attempt"], "headSha": run["head_sha"],
            "measuredAt": measured_at, "summarySha256": digest, "scope": SCOPE}


def validate_provenance(manifest, run, repo_id, workflow_id, digest, artifact, now):
    measured_at = manifest.get("measuredAt")
    require(integer(measured_at, 0), "invalid measuredAt")
    expected = provenance(run, repo_id, workflow_id, measured_at, digest)
    require(set(manifest) == PROVENANCE_KEYS and all(type(manifest[k]) is type(v) and manifest[k] == v for k, v in expected.items()),
            "provenance identity/schema/checksum mismatch")
    require(timestamp(run["created_at"]) - SKEW <= measured_at <= timestamp(artifact["created_at"]) + SKEW
            and measured_at <= now + SKEW, "measurement outside producer time bounds")


def unpack(data, artifact):
    require(len(data) == artifact["size_in_bytes"] and len(data) <= ZIP_LIMIT
            and "sha256:" + hashlib.sha256(data).hexdigest() == artifact["digest"], "archive digest/size mismatch")
    limits = {"Summary.json": SUMMARY_LIMIT, "SHA256SUMS": SIDECAR_LIMIT, "provenance.json": SIDECAR_LIMIT}
    try:
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            members = archive.infolist()
            require(len(members) == 3 and {member.filename for member in members} == set(limits), "unexpected ZIP members")
            for member in members:
                mode = member.external_attr >> 16
                require(not member.is_dir() and not member.flag_bits & 1 and not stat.S_ISLNK(mode)
                        and stat.S_IFMT(mode) in (0, stat.S_IFREG) and member.compress_type in (zipfile.ZIP_STORED, zipfile.ZIP_DEFLATED)
                        and 0 <= member.file_size <= limits[member.filename], "unsafe or oversized ZIP member")
            require(sum(member.file_size for member in members) <= sum(limits.values()), "decompressed size exceeds limit")
            contents = {member.filename: archive.read(member) for member in members}
    except (zipfile.BadZipFile, RuntimeError, NotImplementedError, OSError, EOFError) as error:
        raise Rejected("invalid ZIP archive") from error
    digest = validate_summary(contents["Summary.json"])
    require(contents["SHA256SUMS"] == f"{digest}  Summary.json\n".encode(), "Summary.json checksum mismatch")
    return contents["Summary.json"], parse_object(contents["provenance.json"]), digest


def freeze(run, artifact, manifest):
    return {**manifest, "artifactId": artifact["id"], "artifactName": artifact["name"],
            "artifactDigest": artifact["digest"], "artifactSize": artifact["size_in_bytes"],
            "artifactCreatedAt": artifact["created_at"], "artifactExpiresAt": artifact["expires_at"],
            "runCreatedAt": run["created_at"], "runUpdatedAt": run["updated_at"]}


def select(api, repo_id, workflow_id, now):
    response = api.json(RUNS)
    runs = response.get("workflow_runs")
    require(type(runs) is list and len(runs) <= 30 and integer(response.get("total_count"), 0)
            and response["total_count"] >= len(runs), "invalid bounded run listing")
    eligible = []
    for run in runs:
        try:
            validate_run(run, repo_id, workflow_id)
            if now - 30 * 86400000 <= timestamp(run["created_at"]) <= now + SKEW:
                eligible.append(run)
        except Rejected:
            continue  # Untrusted candidates never grant archive access.
    for run in sorted(eligible, key=lambda r: (timestamp(r["created_at"]), r["id"]), reverse=True):
        response = api.json(f"{REPO}/actions/runs/{run['id']}/artifacts?per_page=100")
        artifacts = response.get("artifacts")
        require(type(artifacts) is list and len(artifacts) <= 100 and type(response.get("total_count")) is int
                and response["total_count"] == len(artifacts), "incomplete artifact listing")
        require(all(type(item) is dict for item in artifacts), "invalid artifact listing entry")
        exact = [item for item in artifacts if item.get("name") == artifact_name(run)]
        if not exact:
            continue  # A successful all-skipped run is not a producer.
        require(len(exact) == 1, "duplicate exact feed artifacts")
        return run, exact[0]
    raise Missing("no scheduled main coverage producer within 30 runs/30 days")


def identity(value, fields):
    return {key: value.get(key) for key in fields}


def admit(api, now):
    repo_id, workflow_id = canonical(api)
    listed_run, listed_artifact = select(api, repo_id, workflow_id, now)
    run_path = f"{REPO}/actions/runs/{listed_run['id']}"
    run = api.json(run_path)
    validate_run(run, repo_id, workflow_id)
    require(identity(run, RUN_FIELDS) == identity(listed_run, RUN_FIELDS), "run changed after selection")
    validate_artifact(listed_artifact, run, repo_id, now)
    artifact_path = f"{REPO}/actions/artifacts/{listed_artifact['id']}"
    artifact = api.json(artifact_path)
    validate_artifact(artifact, run, repo_id, now)
    require(identity(artifact, ARTIFACT_FIELDS) == identity(listed_artifact, ARTIFACT_FIELDS), "artifact changed after selection")
    summary, manifest, digest = unpack(api.archive(artifact_path + "/zip"), artifact)
    validate_provenance(manifest, run, repo_id, workflow_id, digest, artifact, now)
    rechecked_run, rechecked_artifact = api.json(run_path), api.json(artifact_path)
    validate_run(rechecked_run, repo_id, workflow_id)
    validate_artifact(rechecked_artifact, rechecked_run, repo_id, now)
    require(identity(rechecked_run, RUN_FIELDS) == identity(run, RUN_FIELDS)
            and identity(rechecked_artifact, ARTIFACT_FIELDS) == identity(artifact, ARTIFACT_FIELDS), "feed changed at handoff")
    return freeze(run, artifact, manifest), summary


def observer_context(value):
    result = {"releaseContext": "unverified"}
    for field in ("runId", "runAttempt"):
        result[field] = value.get(field) if integer(value.get(field)) else None
    sha, version = value.get("executionSha"), value.get("requestedVersion")
    result["executionSha"] = sha if type(sha) is str and SHA.fullmatch(sha) else None
    result["requestedVersion"] = version if type(version) is str and 0 < len(version) <= 128 and all(ord(c) >= 32 for c in version) else None
    if any(result[field] is None for field in ("runId", "runAttempt", "executionSha", "requestedVersion")):
        result["diagnostic"] = "observer labels unavailable; release context remains unverified"
    return result


def atomic_write(path, data):
    temporary = path.with_name(path.name + ".tmp")
    try:
        with temporary.open("xb") as stream:
            stream.write(data)
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)


def consume(api, directory, now, observer):
    try:
        directory.mkdir(parents=True, exist_ok=True)
        for name in ("Summary.json", "result.json", "Summary.json.tmp", "result.json.tmp"):
            (directory / name).unlink(missing_ok=True)
        require(integer(now, 0), "invalid consumer clock")
        result = {"attemptedAt": now, "observer": observer_context(observer)}
        code = 0
        try:
            source, summary = admit(api, now)
            result.update(state="measured", source=source)
            atomic_write(directory / "Summary.json", summary)
        except Missing as error:
            result.update(state="missing", reason=str(error))
            code = 1
        except Rejected as error:
            result.update(state="incomplete", reason=str(error))
            code = 1
        except Unavailable as error:
            result.update(state="incomplete", reason=str(error), unavailable=True)
            code = 2
        atomic_write(directory / "result.json", json.dumps(result, allow_nan=False).encode())
        print(f"Coverage feed: {result['state']}; admission exit {code}; {result.get('reason', 'validated unsigned transport')}")
        return code
    except (OSError, Rejected) as error:
        print(f"Coverage feed could not write handoff: {type(error).__name__}", file=sys.stderr)
        return 2


def produce(api, report, directory, measured_at, run_id, attempt, sha):
    repo_id, workflow_id = canonical(api)
    require(integer(run_id) and integer(attempt) and type(sha) is str and SHA.fullmatch(sha), "invalid producer environment")
    run = api.json(f"{REPO}/actions/runs/{run_id}")
    validate_run(run, repo_id, workflow_id, completed=False)
    require(run["id"] == run_id and run["run_attempt"] == attempt and run["head_sha"] == sha, "producer environment/run mismatch")
    require(integer(measured_at, 0) and timestamp(run["created_at"]) - SKEW <= measured_at, "invalid report completion time")
    data = read_bounded(report, SUMMARY_LIMIT)
    digest = validate_summary(data)
    manifest = provenance(run, repo_id, workflow_id, measured_at, digest)
    directory.mkdir(parents=True, exist_ok=False)
    atomic_write(directory / "Summary.json", data)
    atomic_write(directory / "SHA256SUMS", f"{digest}  Summary.json\n".encode())
    atomic_write(directory / "provenance.json", json.dumps(manifest, allow_nan=False).encode())
    print("Packaged 3 nightly coverage feed files (unsigned)")


def read_bounded(path, limit):
    with path.open("rb") as stream:
        data = stream.read(limit + 1)
    require(len(data) <= limit, "handoff file exceeds limit")
    return data


def validate_source(source, now):
    require(type(source) is dict and set(source) == SOURCE_KEYS, "missing or malformed admitted source")
    require(integer(source.get("repositoryId")) and integer(source.get("workflowId"))
            and integer(source.get("runId")) and integer(source.get("runAttempt")), "invalid source numeric identity")
    run = {"id": source["runId"], "run_attempt": source["runAttempt"], "workflow_id": source["workflowId"],
           "path": source.get("workflowPath"), "event": source.get("event"), "head_branch": "main",
           "head_sha": source.get("headSha"), "status": "completed", "conclusion": "success",
           "created_at": source.get("runCreatedAt"), "updated_at": source.get("runUpdatedAt"),
           "repository": {"id": source["repositoryId"]}, "head_repository": {"id": source["repositoryId"]}}
    validate_run(run, source["repositoryId"], source["workflowId"])
    artifact = {"id": source.get("artifactId"), "name": source.get("artifactName"), "size_in_bytes": source.get("artifactSize"),
                "digest": source.get("artifactDigest"), "expired": False, "created_at": source.get("artifactCreatedAt"),
                "expires_at": source.get("artifactExpiresAt"), "workflow_run": {"id": run["id"],
                    "repository_id": source["repositoryId"], "head_repository_id": source["repositoryId"],
                    "head_branch": "main", "head_sha": run["head_sha"]}}
    validate_artifact(artifact, run, source["repositoryId"], now)
    digest = source.get("summarySha256")
    require(type(digest) is str and re.fullmatch(r"[0-9a-f]{64}", digest), "invalid summary digest")
    validate_provenance({key: source[key] for key in PROVENANCE_KEYS if key in source}, run,
                        source["repositoryId"], source["workflowId"], digest, artifact, now)


def collect(context_path, report_path, output_path, now):
    """Feed-only branch of the legacy collector; validate everything before writing."""
    context = parse_object(read_bounded(context_path, JSON_LIMIT))
    require(context.get("state") in ("measured", "missing", "incomplete") and integer(context.get("attemptedAt"), 0)
            and type(context.get("observer")) is dict and integer(now, 0)
            and context["attemptedAt"] <= now + SKEW, "invalid feed context")
    observer = observer_context(context["observer"])
    attempt = {"state": context["state"], "attemptedAt": context["attemptedAt"], "observer": observer}
    data = {"lastUpdate": 0, "repoUrl": "https://github.com/Cratis/Chronicle", "entries": {}}
    if output_path.exists():
        stored = read_bounded(output_path, SUMMARY_LIMIT).decode()
        require(stored.startswith("window.COVERAGE_DATA = ") and stored.rstrip().endswith(";"), "invalid stored data wrapper")
        data = parse_object(stored[len("window.COVERAGE_DATA = "):].rstrip()[:-1])
    if context["state"] == "measured":
        source = context.get("source")
        validate_source(source, now)
        summary = read_bounded(report_path / "Summary.json", SUMMARY_LIMIT)
        require(validate_summary(summary) == source["summarySha256"], "verified Summary.json changed before collection")
        old = data.get("measurement") or {}
        previous_time = old.get("measuredAt", 0)
        if not previous_time:
            previous_time = max((int(datetime.strptime(entry["date"], "%Y-%m-%d").replace(tzinfo=timezone.utc).timestamp() * 1000)
                                 for entries in data.get("entries", {}).values() for entry in entries), default=0)
        if source == old.get("source"):
            pass  # Identical immutable feed changes only the observation, never measurement history.
        elif source["measuredAt"] <= previous_time:
            attempt.update(state="incomplete", reason="feed is not newer than the previous measurement")
        else:
            measured = source["measuredAt"]
            date = datetime.fromtimestamp(measured / 1000, timezone.utc).strftime("%Y-%m-%d")
            week = f"{date[:4]}-M{date[5:7]}-W{(int(date[8:]) + 6) // 7}"
            data["measurement"] = {"measuredAt": measured, "commit": source["headSha"][:7], "version": None, "source": source}
            data["lastUpdate"] = measured
            entries = data.setdefault("entries", {})
            for assembly in production_assemblies(parse_object(summary)):
                value = assembly["coverage"]
                value = math.floor(value * (10000 if value < 1 else 100)) / 100
                point = {"date": date, "week": week, "lineCoverage": value, "version": None, "commit": source["headSha"][:7]}
                entries[assembly["name"]] = [point] + [entry for entry in entries.get(assembly["name"], []) if entry.get("week") != week]
            data["entries"] = {name: sorted(points, key=lambda p: p["date"], reverse=True)[:52] for name, points in entries.items()}
    else:
        require("source" not in context and type(context.get("reason")) is str and len(context["reason"]) <= 512,
                "invalid rejected feed context")
        attempt["reason"] = context["reason"]
    data.update(schemaVersion=2, latestAttempt=attempt)
    atomic_write(output_path, ("window.COVERAGE_DATA = " + json.dumps(data, indent=2, allow_nan=False) + ";\n").encode())
    print(f"Collected coverage feed: {attempt['state']}; {len(data['entries'])} projects")


def main(argv=None, api=None, clock=lambda: int(time.time() * 1000)):
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    producer = commands.add_parser("package")
    producer.add_argument("--summary", type=Path, required=True)
    producer.add_argument("--output", type=Path, required=True)
    consumer = commands.add_parser("consume")
    consumer.add_argument("--output", type=Path, required=True)
    collector = commands.add_parser("collect")
    collector.add_argument("--context", type=Path, required=True)
    collector.add_argument("--report", type=Path, required=True)
    collector.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    try:
        if args.command == "consume":
            def label_number(name):
                value = os.environ.get(name, "")
                return int(value) if re.fullmatch(r"[0-9]{1,16}", value) else None
            observer = {"runId": label_number("GITHUB_RUN_ID"), "runAttempt": label_number("GITHUB_RUN_ATTEMPT"),
                        "executionSha": os.environ.get("GITHUB_SHA"), "requestedVersion": os.environ.get("COVERAGE_REQUESTED_VERSION")}
            return consume(api or GitHub(), args.output, clock(), observer)
        if args.command == "package":
            produce(api or GitHub(), args.summary, args.output, int(os.environ["COVERAGE_MEASURED_AT_MS"]),
                    int(os.environ["GITHUB_RUN_ID"]), int(os.environ["GITHUB_RUN_ATTEMPT"]), os.environ["GITHUB_SHA"])
        else:
            value = os.environ.get("COVERAGE_NOW_MS", "")
            require(not value or re.fullmatch(r"[0-9]+", value), "invalid collector clock")
            collect(args.context, args.report, args.output, int(value) if value else clock())
        return 0
    except Rejected as error:
        print(f"Coverage feed rejected: {error}", file=sys.stderr)
        return 1
    except (Unavailable, OSError, ValueError, KeyError) as error:
        print(f"Coverage feed could not run: {type(error).__name__}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
