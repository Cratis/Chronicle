# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Require integration evidence for a head commit, or refresh its failed ordinary gate."""

import argparse
import json
import os
from pathlib import Path
import subprocess
import time

EVIDENCE_WORKFLOW = "requested-integration.yml"
EVIDENCE_JOB = "hot-core-gate-requested-run-integration"
GATE_WORKFLOW = "hot-core-gate.yml"
GATE_JOB = "hot-core-gate"


def has_green_evidence(runs, head_sha):
    """Only the requested gate in a pull-request run for this exact commit is evidence."""
    return any(run["head_sha"] == head_sha and run["event"] == "pull_request"
               and any(job["name"] == EVIDENCE_JOB and job["conclusion"] == "success"
                       for job in run.get("jobs", [])) for run in runs)


def decide(*, detect, touched, dispatch, build, matrix, fork, evidence, head_sha="", repo="Cratis/Chronicle", run_integration=False):
    if detect != "success":
        return False, (f"::error::Hot-core detection did not complete ({detect}), so whether this pull request "
                       "reaches the hot core is unknown. Failing closed.")
    if touched != "true":
        return True, ("No hot-core path is touched by this pull request. The full out-of-process integration "
                      "matrix is not required here.")
    if dispatch:
        if build != "success" or matrix != "success":
            return False, ("::error::This pull request changes the hot core, and the full out-of-process "
                           f"integration matrix did not pass (build: {build}, matrix: {matrix}). See the "
                           "'Detect hot-core changes' summary for the paths that put it here.")
        return True, "This pull request changes the hot core and the full out-of-process integration matrix is green."
    if fork:
        return False, (f"::error::Requested integration only runs for branches in {repo}. A maintainer must run "
                       "the matrix on a trusted branch containing these commits (workflow_dispatch of Hot Core Gate) "
                       "and merge with bypass.")
    if evidence:
        return True, f"Requested integration {evidence} passed for {head_sha}."
    request = ("Integration runs automatically on each push to a PR labeled `run-integration`; "
               "there is no need to remove and re-add the label." if run_integration else
               "Add the `run-integration` label to start integration.")
    return False, ("::error title=Hot core needs integration::This PR changes the hot core and no passing "
                   f"Requested Integration run exists for {head_sha}. {request} "
                   "This check turns green automatically when that run passes; no new push needed.")


def latest_run(runs, head_sha):
    candidates = [run for run in runs if run["head_sha"] == head_sha and run["event"] == "pull_request"]
    return max(candidates, key=lambda run: run["id"], default=None)


def rerun_target(gate_runs, head_sha, pr_head_sha):
    """Never refresh a superseded head, a running gate, or a gate that already passed."""
    if head_sha != pr_head_sha:
        return None
    run = latest_run(gate_runs, head_sha)
    if run and run["status"] == "completed" and any(
            job["name"] == GATE_JOB and job["conclusion"] == "failure" for job in run.get("jobs", [])):
        return run["id"]
    return None


def api(path, *, method="GET", paginate=False):
    command = ["gh", "api", "-X", method, path]
    if paginate:
        command += ["--paginate", "--slurp"]
    result = subprocess.run(command, capture_output=True, text=True, check=True, timeout=30)
    return json.loads(result.stdout) if result.stdout.strip() else None


def collection(path, key):
    return [item for page in api(path, paginate=True) for item in page[key]]


def workflow_runs(repo, workflow, head_sha):
    return collection(f"repos/{repo}/actions/workflows/{workflow}/runs"
                      f"?head_sha={head_sha}&event=pull_request&per_page=100", "workflow_runs")


def with_jobs(repo, run):
    return dict(run, jobs=collection(f"repos/{repo}/actions/runs/{run['id']}/jobs?filter=latest", "jobs"))


def recorded(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def evidence_runs(repo, head_sha, runs_json):
    if runs_json:
        return recorded(runs_json)
    return [with_jobs(repo, run) for run in workflow_runs(repo, EVIDENCE_WORKFLOW, head_sha)
            if run["conclusion"] == "success"]


def gate(arguments):
    state = {name: os.environ.get(name.upper(), "") for name in ("detect", "touched", "build", "matrix")}
    state.update(dispatch=os.environ.get("DISPATCH") == "true", fork=os.environ.get("FORK") == "true",
                 head_sha=os.environ["HEAD_SHA"], repo=os.environ["REPO"], evidence=None,
                 run_integration=os.environ.get("RUN_INTEGRATION") == "true")
    if state["detect"] == "success" and state["touched"] == "true" and not state["dispatch"] and not state["fork"]:
        runs = evidence_runs(state["repo"], state["head_sha"], arguments.runs_json)
        green = next((run for run in runs if has_green_evidence([run], state["head_sha"])), None)
        if green:
            state["evidence"] = green.get("html_url") or f"https://github.com/{state['repo']}/actions/runs/{green['id']}"
    return decide(**state)


def current_pr(repo, head_sha, pr_json):
    if pr_json:
        return recorded(pr_json)
    number = os.environ.get("PR_NUMBER")
    if number:
        return api(f"repos/{repo}/pulls/{number}")
    pulls = api(f"repos/{repo}/commits/{head_sha}/pulls")
    return next((pr for pr in pulls if pr["state"] == "open" and pr["head"]["sha"] == head_sha
                 and pr["head"]["repo"]["full_name"] == repo), None)


def gate_runs(repo, head_sha, runs_json):
    if runs_json:
        return recorded(runs_json)
    runs = workflow_runs(repo, GATE_WORKFLOW, head_sha)
    latest = latest_run(runs, head_sha)
    if latest and latest["status"] == "completed":
        return [with_jobs(repo, latest)]
    return runs


def refresh(arguments):
    repo, head_sha = os.environ["REPO"], os.environ["HEAD_SHA"]
    pr = current_pr(repo, head_sha, arguments.pr_json)
    if not pr or pr["head"]["sha"] != head_sha:
        return True, "Nothing to do: the pull request head has moved or no matching pull request exists."
    if not has_green_evidence(evidence_runs(repo, head_sha, arguments.runs_json), head_sha):
        return True, "Nothing to do: no green requested-integration evidence for this head commit."
    for attempt in range(21):
        runs = gate_runs(repo, head_sha, arguments.gate_runs_json)
        latest = latest_run(runs, head_sha)
        if not latest or latest["status"] == "completed":
            break
        if arguments.gate_runs_json or attempt == 20:
            return False, "::error::The latest hot-core gate is still running; refresh did not complete."
        time.sleep(30)
    # Re-read after polling: a new push owns the check now if the head moved while waiting.
    pr = current_pr(repo, head_sha, arguments.pr_json)
    target = rerun_target(runs, head_sha, pr["head"]["sha"] if pr else "")
    if target is None:
        return True, "Nothing to do: the latest gate did not fail or the pull request head moved."
    if arguments.dry_run:
        return True, f"Would re-run failed jobs for Hot Core Gate run {target}."
    api(f"repos/{repo}/actions/runs/{target}/rerun-failed-jobs", method="POST")
    return True, f"Re-running failed jobs for Hot Core Gate run {target}."


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    gate_parser = commands.add_parser("gate")
    gate_parser.add_argument("--runs-json", help="Recorded runs with embedded jobs (no API lookup).")
    refresh_parser = commands.add_parser("refresh")
    refresh_parser.add_argument("--runs-json", help="Recorded requested-integration runs with jobs.")
    refresh_parser.add_argument("--gate-runs-json", help="Recorded Hot Core Gate runs with jobs.")
    refresh_parser.add_argument("--pr-json", help="Recorded current pull request, including head.sha.")
    refresh_parser.add_argument("--dry-run", action="store_true")
    arguments = parser.parse_args()
    try:
        ok, message = gate(arguments) if arguments.command == "gate" else refresh(arguments)
    except (subprocess.SubprocessError, OSError, ValueError, KeyError, TypeError) as error:
        detail = getattr(error, "stderr", None) or str(error)
        ok, message = False, f"::error::Hot-core gate API/input error; failing closed: {detail.strip()}"
    print(message)
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(summary, "a", encoding="utf-8") as file:
            file.write(message + "\n")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
