#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# analyze-issues.sh
#
# Produces an updated IssueAnalysis.md for the Cratis/Chronicle repository.
#
# How it works
# ────────────
# 1. Fetch every open issue (number, title, assignees, labels, body, dates).
# 2. Fetch all open PRs plus recently-closed PRs (last 500).
# 3. For each PR body, scan for closing-keyword patterns
#    ("closes #N", "fixes #N", "resolves #N", etc.) to build a map of
#    issue → list of PRs.
# 4. Determine whether a linked PR was created by Copilot or a human.
# 5. Classify each open issue into one of:
#       A. Excluded  – has at least one open PR
#       B. Copilot   – assigned to Copilot but no open PR yet
#       C. Obsolete  – last updated >730 days ago with no PR and no recent
#                      activity (heuristic for stale/superseded issues)
#       D. Active    – everything else (preserved from manual categorisation)
# 6. Regenerate the auto-managed sections of IssueAnalysis.md while leaving
#    the hand-written "Already Implemented", "Can Do", and "Need More Details"
#    sections intact.
#
# Prerequisites: gh CLI authenticated, jq, python3 (for date arithmetic)
#
# Usage:
#   GH_TOKEN=<token> REPO=Cratis/Chronicle bash .github/scripts/analyze-issues.sh

set -euo pipefail

REPO="${REPO:-Cratis/Chronicle}"
OUTPUT="${OUTPUT:-IssueAnalysis.md}"
TMP_DIR="$(mktemp -d)"

cleanup() { rm -rf "$TMP_DIR"; }
trap cleanup EXIT

echo "==> Fetching open issues for $REPO …"
gh issue list \
    --repo "$REPO" \
    --state open \
    --json number,title,assignees,labels,body,createdAt,updatedAt \
    --limit 1000 \
    > "$TMP_DIR/issues_open.json"

OPEN_ISSUE_COUNT=$(jq 'length' "$TMP_DIR/issues_open.json")
echo "    Found $OPEN_ISSUE_COUNT open issues."

echo "==> Fetching open PRs …"
gh pr list \
    --repo "$REPO" \
    --state open \
    --json number,title,body,labels,author,createdAt \
    --limit 500 \
    > "$TMP_DIR/prs_open.json"

echo "==> Fetching recently closed/merged PRs (last 500) …"
gh pr list \
    --repo "$REPO" \
    --state merged \
    --json number,title,body,labels,author,mergedAt \
    --limit 500 \
    > "$TMP_DIR/prs_merged.json"

# Combine both PR lists into one for link-detection purposes
jq -s 'add' "$TMP_DIR/prs_open.json" "$TMP_DIR/prs_merged.json" \
    > "$TMP_DIR/prs_all.json"

echo "==> Building issue → PR map …"
# Extract closing-keyword references from every PR body.
# Recognises: closes, close, closed, fixes, fix, fixed, resolves, resolve, resolved
# followed by #N or a full GitHub URL ending in /issues/N
python3 - "$TMP_DIR/prs_all.json" "$TMP_DIR/issues_open.json" \
    "$TMP_DIR/issue_pr_map.json" <<'PYEOF'
import json, re, sys

pr_file, issues_file, out_file = sys.argv[1], sys.argv[2], sys.argv[3]

with open(pr_file) as f:
    prs = json.load(f)
with open(issues_file) as f:
    issues = json.load(f)

open_numbers = {i["number"] for i in issues}

KEYWORDS = r"(?:clos(?:es?|ed)|fix(?:es|ed|ing)?|resolv(?:es|ed)?)"
PATTERN = re.compile(
    rf"{KEYWORDS}\s+(?:https?://github\.com/[^/]+/[^/]+/issues/|#)(\d+)",
    re.IGNORECASE,
)

# issue_number → list of PR dicts that reference it
issue_to_prs: dict[int, list[dict]] = {}

for pr in prs:
    body = pr.get("body") or ""
    for match in PATTERN.finditer(body):
        num = int(match.group(1))
        if num in open_numbers:
            issue_to_prs.setdefault(num, []).append({
                "pr_number":  pr["number"],
                "pr_title":   pr["title"],
                "pr_author":  (pr.get("author") or {}).get("login", ""),
                "is_open":    "mergedAt" not in pr,
            })

with open(out_file, "w") as f:
    json.dump(issue_to_prs, f, indent=2)

print(f"    Linked {len(issue_to_prs)} issues to at least one PR.")
PYEOF

echo "==> Classifying issues …"
python3 - \
    "$TMP_DIR/issues_open.json" \
    "$TMP_DIR/issue_pr_map.json" \
    "$TMP_DIR/classified.json" <<'PYEOF'
import json, sys
from datetime import datetime, timezone

issues_file, map_file, out_file = sys.argv[1], sys.argv[2], sys.argv[3]

with open(issues_file) as f:
    issues = json.load(f)
with open(map_file) as f:
    issue_pr_map = {int(k): v for k, v in json.load(f).items()}

NOW = datetime.now(tz=timezone.utc)
STALE_DAYS = 730  # ~2 years without any update → candidate for "obsolete"

def is_copilot(issue):
    return any(
        (a.get("login") or "").lower() in ("copilot", "github-copilot")
        for a in (issue.get("assignees") or [])
    )

def days_since(iso_str):
    if not iso_str:
        return 0
    dt = datetime.fromisoformat(iso_str.replace("Z", "+00:00"))
    return (NOW - dt).days

classified = {
    "excluded": [],   # has at least one OPEN PR
    "copilot":  [],   # assigned to Copilot, no open PR
    "obsolete": [],   # no activity for STALE_DAYS and no PR
    "active":   [],   # everything else
}

for issue in issues:
    num = issue["number"]
    linked_prs = issue_pr_map.get(num, [])
    open_prs = [p for p in linked_prs if p.get("is_open")]

    since_update = days_since(issue.get("updatedAt"))

    entry = {
        "number": num,
        "title":  issue["title"],
        "prs":    linked_prs,
        "open_prs": open_prs,
        "is_copilot": is_copilot(issue),
        "days_since_update": since_update,
        "assignees": [a.get("login","") for a in (issue.get("assignees") or [])],
        "labels": [l.get("name","") for l in (issue.get("labels") or [])],
    }

    if open_prs:
        classified["excluded"].append(entry)
    elif entry["is_copilot"]:
        classified["copilot"].append(entry)
    elif since_update >= STALE_DAYS:
        classified["obsolete"].append(entry)
    else:
        classified["active"].append(entry)

# Sort each bucket by issue number descending (newest first)
for key in classified:
    classified[key].sort(key=lambda e: e["number"], reverse=True)

with open(out_file, "w") as f:
    json.dump(classified, f, indent=2)

print(f"    excluded={len(classified['excluded'])} "
      f"copilot={len(classified['copilot'])} "
      f"obsolete={len(classified['obsolete'])} "
      f"active={len(classified['active'])}")
PYEOF

echo "==> Generating updated IssueAnalysis.md …"
python3 - \
    "$TMP_DIR/classified.json" \
    "$OUTPUT" <<'PYEOF'
import json, sys, re
from datetime import datetime, timezone

classified_file, output_file = sys.argv[1], sys.argv[2]

with open(classified_file) as f:
    data = json.load(f)

NOW = datetime.now(tz=timezone.utc)
REPO = "Cratis/Chronicle"

def issue_link(num, title):
    return f"[#{num}](https://github.com/{REPO}/issues/{num})"

def pr_badge(pr):
    state = "open" if pr.get("is_open") else "merged"
    author = pr["pr_author"]
    tag = " *(Copilot)*" if author.lower() in ("copilot", "github-copilot") else ""
    return f"PR [#{pr['pr_number']}](https://github.com/{REPO}/pull/{pr['pr_number']}){tag}"

# ── Build auto-generated sections ────────────────────────────────────────────

lines = []
lines.append("# Chronicle Repository Issue Analysis\n")
lines.append(f"> **Auto-generated** on {NOW.strftime('%Y-%m-%d')} by the weekly "
             "[issue-analysis workflow](.github/workflows/issue-analysis.yml). "
             "Sections 1–3 below are maintained by hand and updated by AI analysis.\n")
lines.append("")

# ── Section 0: Excluded ──────────────────────────────────────────────────────
lines.append("## Excluded — Has an Open Pull Request\n")
lines.append("These issues are actively being worked on and are excluded from the backlog triage.\n")
lines.append("| # | Issue | Pull Request(s) |")
lines.append("|---|-------|-----------------|")
for e in sorted(data["excluded"], key=lambda x: x["number"], reverse=True):
    pr_list = ", ".join(pr_badge(p) for p in e["open_prs"])
    lines.append(f"| {issue_link(e['number'], e['title'])} | {e['title']} | {pr_list} |")
lines.append("")

# ── Section Copilot ──────────────────────────────────────────────────────────
lines.append("## Assigned to Copilot — No PR Yet\n")
lines.append("These issues are assigned to Copilot but do not yet have an open PR. "
             "They may be in progress or waiting to be picked up.\n")
lines.append("| # | Issue | Assignees |")
lines.append("|---|-------|-----------|")
for e in data["copilot"]:
    assignees = ", ".join(f"@{a}" for a in e["assignees"] if a)
    lines.append(f"| {issue_link(e['number'], e['title'])} | {e['title']} | {assignees} |")
lines.append("")

# ── Section Obsolete ─────────────────────────────────────────────────────────
lines.append("## Potentially Obsolete\n")
lines.append("These issues have had no activity for **≥ 2 years** and no linked PR. "
             "They may be superseded by later work, no longer relevant, or waiting for "
             "someone to verify whether they still apply.\n")
lines.append("| # | Issue | Last updated (days ago) |")
lines.append("|---|-------|------------------------|")
for e in sorted(data["obsolete"], key=lambda x: x["days_since_update"], reverse=True):
    lines.append(
        f"| {issue_link(e['number'], e['title'])} | {e['title']} | {e['days_since_update']} days |"
    )
lines.append("")

# ── Preserve hand-written sections if the file already exists ────────────────
HAND_WRITTEN_MARKER = "<!-- HAND-WRITTEN SECTIONS START -->"

try:
    with open(output_file) as f:
        existing = f.read()
    idx = existing.find(HAND_WRITTEN_MARKER)
    if idx != -1:
        preserved = existing[idx:]
    else:
        # File exists but has no marker — keep everything after the first hand-written section
        m = re.search(r'\n## 1\.', existing)
        preserved = (HAND_WRITTEN_MARKER + "\n\n" + existing[m.start():].lstrip()) if m else ""
except FileNotFoundError:
    preserved = ""

if not preserved:
    preserved = (
        HAND_WRITTEN_MARKER + "\n\n"
        "---\n\n"
        "## 1. Already Implemented in Code\n\n"
        "*To be filled in by AI analysis or manual review.*\n\n"
        "---\n\n"
        "## 2. Can Do Without More Details\n\n"
        "*To be filled in by AI analysis or manual review.*\n\n"
        "---\n\n"
        "## 3. Need More Details\n\n"
        "*To be filled in by AI analysis or manual review.*\n"
    )

content = "\n".join(lines) + "\n---\n\n" + preserved + "\n"

with open(output_file, "w") as f:
    f.write(content)

print(f"    Written to {output_file}")
PYEOF

echo ""
echo "==> Done. Review $OUTPUT before committing."
