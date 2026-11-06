#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Decides how much of the requested integration matrix a pull request needs.

Three answers, and the default for anything uncertain is the widest one:

``skip``      Nothing integration-relevant changed since the last Requested Integration run that passed
              for this pull request, so that run's result still holds. The workflow records the new head
              as green without running the matrix again.
``selected``  Only the lanes the changed paths map to need to run (see `.github/integration-selection.json`).
``full``      Everything runs. A hot-core change, an unmapped path, an unknown base, a head that is not a
              descendant of the last green one, or any failure to find out, all end here.

`.github/integration-selection.json` is a committed contract: the first entry matching a changed file
wins, and a file that matches no entry forces ``full``. The nightly run ignores this script entirely.
"""
import argparse
import json
import os
import re
import subprocess
import sys

CONTRACT = ".github/integration-selection.json"
REQUESTED_WORKFLOW = "requested-integration.yml"
LANES = ("client", "kernel", "clustering", "sql")


def to_regex(pattern):
    """Translates a contract pattern; `**` crosses `/`, `*` does not, `{ns}` captures one directory name."""
    compiled = ""
    index = 0
    while index < len(pattern):
        if pattern.startswith("**", index):
            compiled += ".*"
            index += 2
        elif pattern.startswith("{ns}", index):
            compiled += "(?P<ns>[^/]+)"
            index += 4
        elif pattern[index] == "*":
            compiled += "[^/]*"
            index += 1
        elif pattern[index] == "?":
            compiled += "[^/]"
            index += 1
        else:
            compiled += re.escape(pattern[index])
            index += 1
    return re.compile(f"^{compiled}$")


def load_entries(path=CONTRACT):
    """Reads the contract, validating every entry so a typo cannot silently narrow the matrix."""
    with open(path, encoding="utf-8") as file:
        entries = json.load(file)["entries"]
    if not entries:
        raise SystemExit(f"::error::{path} has no entries, so nothing would ever be ignored or selected.")
    for entry in entries:
        if entry.get("action") not in ("ignore", "select", "full"):
            raise SystemExit(f"::error::{path} has an entry with an unknown action: {entry.get('action')!r}")
        if not entry.get("paths"):
            raise SystemExit(f"::error::{path} has an entry without paths.")
        unknown = set(entry) - {"paths", "action", "client", "kernel", "clustering", "sql", "reason"}
        if unknown:
            raise SystemExit(f"::error::{path} has an entry with unknown keys: {sorted(unknown)}")
        if entry["action"] == "select" and not any(entry.get(lane) for lane in LANES):
            raise SystemExit(f"::error::{path} has a `select` entry that selects nothing: {entry['paths']}")
    return entries


def classify(files, entries):
    """Returns `(mode, selection, reasons)` for a set of changed files.

    `mode` is ``skip`` when every file is ignorable, ``full`` when any file forces everything (or matches
    nothing), and ``selected`` otherwise. `selection` is `{client: [..], kernel, clustering, sql}`.
    """
    compiled = [(entry, [to_regex(pattern) for pattern in entry["paths"]]) for entry in entries]
    selection = {"client": set(), "kernel": False, "clustering": False, "sql": False}
    reasons = []
    forced = False
    selected_any = False
    for path in sorted(set(files)):
        outcome = None
        for entry, expressions in compiled:
            match = next((m for m in (e.match(path) for e in expressions) if m), None)
            if match:
                outcome = (entry, match)
                break
        if outcome is None:
            forced = True
            reasons.append(f"{path}: matches no entry, so the whole matrix runs")
            continue
        entry, match = outcome
        if entry["action"] == "ignore":
            continue
        if entry["action"] == "full":
            forced = True
            reasons.append(f"{path}: {entry.get('reason', 'forces the whole matrix')}")
            continue
        selected_any = True
        namespace = match.groupdict().get("ns", "")
        for name in entry.get("client", []):
            selection["client"].add(name.replace("{ns}", namespace))
        for lane in ("kernel", "clustering", "sql"):
            selection[lane] = selection[lane] or bool(entry.get(lane))
        reasons.append(f"{path}: selects {', '.join(sorted(n.replace('{ns}', namespace) for n in entry.get('client', [])))}")
    result = {**selection, "client": sorted(selection["client"])}
    if forced:
        return "full", None, reasons
    if not selected_any:
        return "skip", None, ["Every changed file is one that no integration lane exercises."]
    return "selected", result, reasons


def run(*arguments):
    result = subprocess.run(arguments, capture_output=True, text=True, check=False)
    return result.returncode, result.stdout.strip(), result.stderr.strip()


def changed_files(base, head):
    """Files changed between two commits, or None when git cannot say (never an empty guess)."""
    code, out, _ = run("git", "diff", "--name-only", f"{base}...{head}")
    return [line for line in out.splitlines() if line] if code == 0 else None


def last_green_head(repo, pull_request, current_head):
    """The newest head that passed the requested gate for this pull request, if it is an ancestor of the current one."""
    code, out, _ = run(
        "gh", "api", "--paginate",
        f"repos/{repo}/actions/workflows/{REQUESTED_WORKFLOW}/runs?event=pull_request&status=success&per_page=100",
        "--jq", ".workflow_runs[] | [.head_sha, .created_at, ([.pull_requests[].number] | join(\",\"))] | @tsv")
    if code != 0:
        return None
    candidates = []
    for line in out.splitlines():
        fields = line.split("\t")
        if len(fields) == 3 and str(pull_request) in fields[2].split(",") and fields[0] != current_head:
            candidates.append((fields[1], fields[0]))
    for _, sha in sorted(candidates, reverse=True):
        if run("git", "cat-file", "-e", f"{sha}^{{commit}}")[0] == 0 and run("git", "merge-base", "--is-ancestor", sha, current_head)[0] == 0:
            return sha
    return None


def decide(*, entries, base, head, touched, green_head=None, files_between=changed_files):
    """Applies the rules in order of how much they can save, widest answer on any doubt. Returns `(mode, selection, reasons)`."""
    if green_head:
        since = files_between(green_head, head)
        if since is not None:
            mode, _, reasons = classify(since, entries)
            if mode == "skip":
                return "skip", None, [f"Nothing integration-relevant changed since {green_head}, which passed requested integration."]
    if touched:
        return "full", None, ["This pull request touches the hot core, so the whole matrix runs."]
    files = files_between(base, head) if base else None
    if files is None:
        return "full", None, ["The changed files could not be determined, so the whole matrix runs."]
    return classify(files, entries)


def emit(name, value):
    path = os.environ.get("GITHUB_OUTPUT")
    if path:
        with open(path, "a", encoding="utf-8") as file:
            file.write(f"{name}={value}\n")


def summarize(mode, selection, reasons):
    lines = [f"Integration selection: **{mode}**"]
    if selection:
        lines.append("Client namespaces: " + (", ".join(selection["client"]) or "none")
                     + "; kernel: " + str(selection["kernel"]).lower()
                     + "; clustering: " + str(selection["clustering"]).lower()
                     + "; sql: " + str(selection["sql"]).lower())
    lines += [f"- {reason}" for reason in reasons[:50]]
    text = "\n".join(lines)
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as file:
            file.write(text + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", help="Base commit of the pull request.")
    parser.add_argument("--head", required=True, help="Head commit of the pull request.")
    parser.add_argument("--touched", default="false", help="true when the pull request touches the hot core.")
    parser.add_argument("--repo", help="owner/name; with --pull-request enables reuse of an earlier green run.")
    parser.add_argument("--pull-request", help="Pull request number.")
    args = parser.parse_args()
    entries = load_entries()
    green = last_green_head(args.repo, args.pull_request, args.head) if args.repo and args.pull_request else None
    mode, selection, reasons = decide(entries=entries, base=args.base, head=args.head,
                                      touched=args.touched == "true", green_head=green)
    summarize(mode, selection, reasons)
    emit("mode", mode)
    emit("selection", json.dumps(selection, separators=(",", ":")) if selection else "")


if __name__ == "__main__":
    main()
