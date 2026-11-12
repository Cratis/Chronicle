# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

"""Require integration evidence for a head commit, or refresh its failed ordinary gate."""

import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import time

from integration_lane_exclusions import with_exclusion_summary

EVIDENCE_WORKFLOW = "requested-integration.yml"
EVIDENCE_JOB = "hot-core-gate-requested-run-integration"
GATE_WORKFLOW = "hot-core-gate.yml"
GATE_JOB = "hot-core-gate"
BUILD_WORKFLOW = "dotnet-build.yml"


def build_triggered(paths, root):
    """Reuse the build contract's reader and GitHub glob semantics, including exclusions."""
    spec = importlib.util.spec_from_file_location(
        "build_affecting_paths", Path(__file__).with_name("assert-build-affecting-paths.py"))
    helper = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(helper)
    patterns, ignored = helper.read_trigger_paths(root, f".github/workflows/{BUILD_WORKFLOW}")
    rules = [(pattern.startswith("!"), helper.to_regex(pattern[1:] if pattern.startswith("!") else pattern))
             for pattern in patterns]
    exclusions = [helper.to_regex(pattern) for pattern in ignored]
    for path in paths:
        included = not patterns
        for negative, expression in rules:
            if expression.match(path):
                included = not negative
        if included and not any(expression.match(path) for expression in exclusions):
            return True
    return False


def detect_build(arguments):
    """Match the PR's merge-base diff; a failed diff or unreadable filter never emits success."""
    triggered = False
    if arguments.base:
        result = subprocess.run(["git", "diff", "--name-only", f"{arguments.base}...{arguments.head}"],
                                capture_output=True, text=True, check=True)
        triggered = build_triggered(result.stdout.splitlines(), ".")
    message = f"build-triggered={str(triggered).lower()}"
    if output := os.environ.get("GITHUB_OUTPUT"):
        with open(output, "a", encoding="utf-8") as file:
            file.write(message + "\n")
    return True, message


def has_green_evidence(runs, head_sha):
    """Only the requested gate in a pull-request run for this exact commit is evidence."""
    return any(run["head_sha"] == head_sha and run["event"] == "pull_request"
               and any(job["name"] == EVIDENCE_JOB and job["conclusion"] == "success"
                       for job in run.get("jobs", [])) for run in runs)


def integration_status(latest, head_sha, repo):
    """Tell apart a run that never started, one still running, and one that finished without passing."""
    if not latest:
        return f"No Requested Integration run has started for {head_sha}."
    url = latest.get("html_url") or f"https://github.com/{repo}/actions/runs/{latest['id']}"
    if latest.get("status") != "completed":
        return f"Requested Integration is still running for {head_sha}: {url}"
    if latest.get("conclusion") == "success":
        return f"Requested Integration finished for {head_sha} but its requested gate did not pass: {url}"
    return (f"Requested Integration ended with {latest.get('conclusion')} for {head_sha}: {url} - fix or re-run "
            "its failing jobs.")


def decide(*, detect, touched, dispatch, build, matrix, fork, evidence, head_sha="", repo="Cratis/Chronicle",
           run_integration=False, latest=None):
    if detect != "success":
        return False, (f"::error::Hot-core detection did not complete ({detect}), so whether this pull request "
                       "reaches the hot core is unknown. Failing closed.")
    if touched != "true":
        if run_integration and not dispatch and not fork:
            if evidence:
                return True, f"Requested integration {evidence} passed for {head_sha}."
            return False, ("::error title=Integration requested::run-integration is set and no passing Requested "
                           f"Integration run exists for {head_sha}. {integration_status(latest, head_sha, repo)} "
                           "Integration runs automatically on each push to "
                           "a PR labeled `run-integration`; if none started for this head, push again or re-run the "
                           "latest Requested Integration run for this PR in the Actions tab. This check turns green "
                           "automatically when that run passes; remove the label and re-run this check to drop the requirement.")
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
                   f"Requested Integration run exists for {head_sha}. {integration_status(latest, head_sha, repo)} "
                   f"{request} "
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


# GitHub answers 5xx now and then, and a timeout is no verdict either: retry those a bounded number of times,
# then fail closed as before. Client errors (4xx) are never retried.
RETRY_DELAYS = (5, 15)


def transient(error):
    if isinstance(error, subprocess.TimeoutExpired):
        return True
    stderr = error.stderr or ""
    return re.search(r"HTTP 5\d\d", stderr) is not None


def api(path, *, method="GET", paginate=False):
    command = ["gh", "api", "-X", method, path]
    if paginate:
        command += ["--paginate", "--slurp"]
    for delay in (*RETRY_DELAYS, None):
        try:
            result = subprocess.run(command, capture_output=True, text=True, check=True, timeout=30)
            return json.loads(result.stdout) if result.stdout.strip() else None
        except (subprocess.CalledProcessError, subprocess.TimeoutExpired) as error:
            if delay is None or not transient(error):
                raise
            time.sleep(delay)


def collection(path, key):
    return [item for page in api(path, paginate=True) for item in page[key]]


def workflow_runs(repo, workflow, head_sha):
    return collection(f"repos/{repo}/actions/workflows/{workflow}/runs"
                      f"?head_sha={head_sha}&event=pull_request&per_page=100", "workflow_runs")


def with_jobs(repo, run):
    # Keep the page small: GitHub 502s on every attempt for runs with ~145 jobs at the default page size.
    return dict(run, jobs=collection(f"repos/{repo}/actions/runs/{run['id']}/jobs?filter=latest&per_page=30",
                                     "jobs"))


def recorded(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def evidence_runs(repo, head_sha, runs_json):
    if runs_json:
        return recorded(runs_json)
    return [with_jobs(repo, run) for run in workflow_runs(repo, EVIDENCE_WORKFLOW, head_sha)
            if run["conclusion"] == "success"]


def latest_requested(repo, head_sha, runs_json):
    runs = recorded(runs_json) if runs_json else workflow_runs(repo, EVIDENCE_WORKFLOW, head_sha)
    return latest_run(runs, head_sha)


def still_requested(repo, head_sha):
    """The payload labels can be stale; honor the request only while the current PR head still carries it."""
    pr = api(f"repos/{repo}/pulls/{os.environ['PR_NUMBER']}")
    return (pr["head"]["sha"] == head_sha
            and any(label["name"] == "run-integration" for label in pr.get("labels", [])))


def gate(arguments):
    state = {name: os.environ.get(name.upper(), "") for name in ("detect", "touched", "build", "matrix")}
    state.update(dispatch=os.environ.get("DISPATCH") == "true", fork=os.environ.get("FORK") == "true",
                 head_sha=os.environ["HEAD_SHA"], repo=os.environ["REPO"], evidence=None,
                 run_integration=os.environ.get("RUN_INTEGRATION") == "true")
    load_integration_evidence(state, arguments.runs_json)
    integration_ok, integration_message = with_exclusion_summary(decide(**state))
    if state["dispatch"] or state["detect"] != "success":
        return integration_ok, integration_message
    latest = latest_build(state["repo"], state["head_sha"], arguments.build_runs_json)
    specs_ok, specs_message = specs_verdict(latest, state)
    return integration_ok and specs_ok, integration_message + "\n" + specs_message


def load_integration_evidence(state, runs_json):
    if state["detect"] != "success" or state["dispatch"] or state["fork"]:
        return
    if state["touched"] != "true" and state["run_integration"]:
        state["run_integration"] = still_requested(state["repo"], state["head_sha"])
    if state["touched"] != "true" and not state["run_integration"]:
        return
    runs = evidence_runs(state["repo"], state["head_sha"], runs_json)
    green = next((run for run in runs if has_green_evidence([run], state["head_sha"])), None)
    if green:
        state["evidence"] = green.get("html_url") or f"https://github.com/{state['repo']}/actions/runs/{green['id']}"
    else:
        state["latest"] = latest_requested(state["repo"], state["head_sha"], runs_json)


def latest_build(repo, head_sha, runs_json):
    """Only the newest PR build for this exact head can supply specs evidence."""
    runs = recorded(runs_json) if runs_json else workflow_runs(repo, BUILD_WORKFLOW, head_sha)
    latest = latest_run(runs, head_sha)
    if latest and latest["status"] == "completed" and not runs_json:
        return with_jobs(repo, latest)
    return latest


def specs_passed(latest):
    return bool(latest and latest["status"] == "completed" and any(
        job["name"] == "specs" and job["conclusion"] == "success" for job in latest.get("jobs", [])))


def specs_verdict(latest, state):
    if os.environ.get("BUILD_TRIGGERED") != "true" and latest is None:
        return True, ".NET Build & Integration was not triggered; specs are not required."
    if specs_passed(latest):
        return True, f".NET Build & Integration specs passed for {state['head_sha']}."
    retry = ("Re-run this check after specs pass; fork PRs do not refresh automatically." if state["fork"] else
             "This check turns green automatically when it passes.")
    if latest is None:
        detail = f".NET Build & Integration has not started for {state['head_sha']}."
    else:
        url = latest.get("html_url") or f"https://github.com/{state['repo']}/actions/runs/{latest['id']}"
        if latest["status"] != "completed":
            detail = f".NET Build & Integration is still running for {state['head_sha']}: {url}"
        else:
            job = next((job for job in latest.get("jobs", []) if job["name"] == "specs"), None)
            conclusion = job["conclusion"] if job else "missing"
            detail = f".NET Build & Integration specs conclusion: {conclusion} for {state['head_sha']}: {url}"
    return False, f"::error title=Specs required::{detail} {retry}"


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
    if os.environ.get("SOURCE_WORKFLOW", "").rsplit("/", 1)[-1] == BUILD_WORKFLOW:
        if not specs_passed(latest_build(repo, head_sha, arguments.build_runs_json)):
            return True, "Nothing to do: no green specs evidence for this head commit."
    elif not has_green_evidence(evidence_runs(repo, head_sha, arguments.runs_json), head_sha):
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
    gate_parser.add_argument("--build-runs-json", help="Recorded .NET Build & Integration runs with jobs.")
    refresh_parser = commands.add_parser("refresh")
    refresh_parser.add_argument("--runs-json", help="Recorded requested-integration runs with jobs.")
    refresh_parser.add_argument("--gate-runs-json", help="Recorded Hot Core Gate runs with jobs.")
    refresh_parser.add_argument("--pr-json", help="Recorded current pull request, including head.sha.")
    refresh_parser.add_argument("--dry-run", action="store_true")
    refresh_parser.add_argument("--build-runs-json", help="Recorded .NET Build & Integration runs with jobs.")
    detect_parser = commands.add_parser("detect-build")
    detect_parser.add_argument("--base", default="")
    detect_parser.add_argument("--head", default="HEAD")
    arguments = parser.parse_args()
    try:
        ok, message = {"gate": gate, "refresh": refresh, "detect-build": detect_build}[arguments.command](arguments)
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
