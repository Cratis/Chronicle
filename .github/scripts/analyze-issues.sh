#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# analyze-issues.sh
#
# Classifies open issues and updates the "Issues Categorized" GitHub Project.
# (https://github.com/orgs/Cratis/projects/7)
#
# How it works
# ────────────
# 1. Fetch every OPEN issue (number, title, assignees, labels, body, dates, node-id).
#    Closed issues are never included.
# 2. Fetch all OPEN PRs plus recently-merged PRs (last 500).
#    Closed (declined) PRs are never fetched.
# 3. For each PR body, scan for closing-keyword patterns
#    ("closes #N", "fixes #N", "resolves #N", etc.) to build a map of
#    issue → list of PRs.
# 4. Determine whether a linked PR was created by Copilot or a human.
# 5. Classify each open issue into one of:
#       A. Excluded  – has at least one OPEN PR
#       B. Copilot   – assigned to Copilot but no open PR and no linked PR at all
#       C. Obsolete  – last updated >730 days ago with no PR and no recent
#                      activity (heuristic for stale/superseded issues)
#       D. Active    – everything else
#    Note: issues that have only MERGED (not open) PRs linked to them are moved
#    to the Active bucket so they do not appear under "No PR Yet".
# 6. Map each issue to a project lane:
#       Potentially Obsolete  – bucket C (stale, no PR)
#       Already Implemented   – active issues where all linked PRs are merged
#       Ready                 – bucket A (open PR) and bucket B (Copilot-assigned)
#       Need more details     – remaining active issues (default)
#    If an issue is already in the project with a "Ready" or "Need more details"
#    lane, that lane is preserved (manual categorisation is respected).
#    For a first-time run, existing IssueAnalysis.md sections are used to seed
#    the "Ready" and "Need more details" lanes.
# 7. Add new issues to the project and update lanes where the auto-classification
#    overrides the current lane.
#
# Prerequisites: gh CLI authenticated, jq, python3
#
# Usage:
#   GH_TOKEN=<token> REPO=Cratis/Chronicle ORG=Cratis PROJECT_NUMBER=7 \
#     bash .github/scripts/analyze-issues.sh

set -euo pipefail

REPO="${REPO:-Cratis/Chronicle}"
ORG="${ORG:-Cratis}"
PROJECT_NUMBER="${PROJECT_NUMBER:-7}"
ISSUE_ANALYSIS_FILE="${ISSUE_ANALYSIS_FILE:-IssueAnalysis.md}"
TMP_DIR="$(mktemp -d)"

cleanup() { rm -rf "$TMP_DIR"; }
trap cleanup EXIT

echo "==> Fetching open issues for $REPO …"
gh issue list \
    --repo "$REPO" \
    --state open \
    --json number,title,assignees,labels,body,createdAt,updatedAt,id \
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
# followed by:
#   - #N (bare issue reference)
#   - owner/repo#N (cross-repo shorthand, e.g. Cratis/Chronicle#123)
#   - https://github.com/owner/repo/issues/N (full URL)
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
    rf"{KEYWORDS}\s+(?:https?://github\.com/[^/]+/[^/]+/issues/|[A-Za-z0-9._-]+/[A-Za-z0-9._-]+#|#)(\d+)",
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
    elif linked_prs:
        # Issue has linked PR(s) but none are open (all merged/closed).
        # Do not show in "Copilot — No PR Yet" since PR(s) already exist.
        classified["active"].append(entry)
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

# GitHub Project V2 operations require a PAT with 'project' scope.
# GITHUB_TOKEN is scoped to the repository and cannot access org-level projects.
if [[ -n "${GH_PROJECT_TOKEN:-}" ]]; then
    export GH_TOKEN="$GH_PROJECT_TOKEN"
    echo "    Using GH_PROJECT_TOKEN for org project operations."
else
    echo ""
    echo "==> WARNING: GH_PROJECT_TOKEN is not set." >&2
    echo "    GitHub Project V2 update requires a Personal Access Token (PAT)" >&2
    echo "    with the 'project' scope, stored as the GH_PROJECT_TOKEN secret." >&2
    echo "    Issue classification completed but project was NOT updated." >&2
    echo ""
    echo "==> Done (project update skipped – GH_PROJECT_TOKEN not configured)."
    exit 0
fi

echo "==> Updating GitHub Project (org=$ORG, project=$PROJECT_NUMBER) …"
python3 - \
    "$TMP_DIR/classified.json" \
    "$TMP_DIR/issues_open.json" \
    "$ISSUE_ANALYSIS_FILE" \
    "$ORG" \
    "$PROJECT_NUMBER" <<'PYEOF'
import json
import os
import re
import subprocess
import sys
import time
from collections import Counter

classified_file  = sys.argv[1]
issues_file      = sys.argv[2]
issue_analysis   = sys.argv[3]   # may not exist – used only for initial seeding
ORG              = sys.argv[4]
PROJECT_NUMBER   = int(sys.argv[5])

with open(classified_file) as f:
    data = json.load(f)
with open(issues_file) as f:
    issues_raw = json.load(f)

# Build lookup: issue number → GraphQL node ID
issue_node_ids = {i["number"]: i.get("id", "") for i in issues_raw}

# ── Lane constants ────────────────────────────────────────────────────────────
LANE_OBSOLETE     = "Potentially Obsolete"
LANE_IMPLEMENTED  = "Already Implemented"
LANE_READY        = "Ready"
LANE_NEEDS_DETAILS = "Need more details"
LANES = [LANE_OBSOLETE, LANE_IMPLEMENTED, LANE_READY, LANE_NEEDS_DETAILS]

# ── Parse IssueAnalysis.md for initial seeding ────────────────────────────────
# Sections 1 / 2 / 3 contain issue numbers that serve as a one-time seed for
# the project lanes when the project is first populated.
implemented_from_file: set[int] = set()
ready_from_file: set[int]       = set()
needs_from_file: set[int]       = set()

if os.path.exists(issue_analysis):
    with open(issue_analysis) as f:
        md = f.read()
    m1 = re.search(r'## 1\. Already Implemented', md)
    m2 = re.search(r'## 2\. Can Do', md)
    m3 = re.search(r'## 3\. Need More Details', md)
    if m1 and m2:
        for m in re.finditer(r'\[#(\d+)\]', md[m1.start():m2.start()]):
            implemented_from_file.add(int(m.group(1)))
    if m2 and m3:
        for m in re.finditer(r'\[#(\d+)\]', md[m2.start():m3.start()]):
            ready_from_file.add(int(m.group(1)))
    if m3:
        for m in re.finditer(r'\[#(\d+)\]', md[m3.start():]):
            needs_from_file.add(int(m.group(1)))
    print(f"    Seeded from IssueAnalysis.md: "
          f"{len(implemented_from_file)} implemented, "
          f"{len(ready_from_file)} ready, "
          f"{len(needs_from_file)} needs-details")

# ── GraphQL helper ────────────────────────────────────────────────────────────
def run_graphql(query: str, fail_on_error: bool = True, **kwargs) -> dict | None:
    """Call `gh api graphql` and return the parsed JSON response."""
    args = ["gh", "api", "graphql"]
    for k, v in kwargs.items():
        if v is None:
            continue
        elif isinstance(v, int):
            args += ["-F", f"{k}={v}"]
        else:
            args += ["-f", f"{k}={v}"]
    args += ["-f", f"query={query}"]
    result = subprocess.run(args, capture_output=True, text=True)
    if result.returncode != 0:
        msg = f"gh api graphql failed (exit {result.returncode}): {result.stderr.strip()}"
        if fail_on_error:
            print(msg, file=sys.stderr)
            sys.exit(1)
        print(f"    WARNING: {msg}", file=sys.stderr)
        return None
    response = json.loads(result.stdout)
    if "errors" in response:
        msg = f"GraphQL errors: {json.dumps(response['errors'])}"
        if fail_on_error:
            print(msg, file=sys.stderr)
            sys.exit(1)
        print(f"    WARNING: {msg}", file=sys.stderr)
        return None
    return response

# ── Step 1: Get project info ──────────────────────────────────────────────────
print("    Querying project metadata …")
resp = run_graphql("""
query($org: String!, $number: Int!) {
  organization(login: $org) {
    projectV2(number: $number) {
      id
      fields(first: 20) {
        nodes {
          ... on ProjectV2SingleSelectField {
            id
            name
            options { id name }
          }
        }
      }
    }
  }
}
""", org=ORG, number=PROJECT_NUMBER)

project    = resp["data"]["organization"]["projectV2"]
project_id = project["id"]
print(f"    Project ID: {project_id}")

# Find the field that contains our lane options
status_field = None
for field in project["fields"]["nodes"]:
    if not field:
        continue
    field_option_names = {opt["name"] for opt in field.get("options", [])}
    if any(lane in field_option_names for lane in LANES):
        status_field = field
        break

if not status_field:
    # Fallback: look for a field named "Status"
    for field in project["fields"]["nodes"]:
        if field and field.get("name", "").lower() == "status":
            status_field = field
            break

if not status_field:
    available = [f.get("name") for f in project["fields"]["nodes"] if f]
    print(f"ERROR: No field with the expected lane options found in project.", file=sys.stderr)
    print(f"       Available fields: {available}", file=sys.stderr)
    sys.exit(1)

field_id = status_field["id"]
options  = {opt["name"]: opt["id"] for opt in status_field["options"]}
print(f"    Using field '{status_field['name']}' (ID: {field_id})")
print(f"    Available options: {list(options.keys())}")

missing = [lane for lane in LANES if lane not in options]
if missing:
    print(f"ERROR: Required lane(s) not found in project field: {missing}", file=sys.stderr)
    sys.exit(1)

# ── Step 2: Fetch all existing project items ─────────────────────────────────
print("    Fetching existing project items …")
existing_items: dict[int, dict] = {}   # issue_number → {item_id, status}
cursor = None
while True:
    resp = run_graphql("""
query($projectId: ID!, $after: String) {
  node(id: $projectId) {
    ... on ProjectV2 {
      items(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes {
          id
          content { ... on Issue { number } }
          fieldValues(first: 10) {
            nodes {
              ... on ProjectV2ItemFieldSingleSelectValue {
                name
                field { ... on ProjectV2SingleSelectField { name } }
              }
            }
          }
        }
      }
    }
  }
}
""", projectId=project_id, after=cursor)
    items_page = resp["data"]["node"]["items"]
    for item in items_page["nodes"]:
        content = item.get("content") or {}
        if "number" not in content:
            continue
        issue_num = content["number"]
        current_status = None
        for fv in item["fieldValues"]["nodes"]:
            fv_field = fv.get("field", {})
            if fv_field.get("name") == status_field["name"]:
                current_status = fv.get("name")
                break
        existing_items[issue_num] = {"item_id": item["id"], "status": current_status}
    if items_page["pageInfo"]["hasNextPage"]:
        cursor = items_page["pageInfo"]["endCursor"]
    else:
        break
print(f"    Found {len(existing_items)} existing items in project.")

# ── Step 3: Compute lane assignments ─────────────────────────────────────────
# The auto-classification always wins for objective lanes (Obsolete, Implemented).
# For subjective lanes (Ready / Need more details) the existing project state is
# respected; IssueAnalysis.md is used only for issues not yet in the project.
lane_assignments: dict[int, str] = {}

# Potentially Obsolete
for e in data["obsolete"]:
    lane_assignments[e["number"]] = LANE_OBSOLETE

# Already Implemented – active issues whose linked PRs are all merged
for e in data["active"]:
    if e.get("prs") and not e.get("open_prs"):
        lane_assignments[e["number"]] = LANE_IMPLEMENTED

# Ready – Copilot-assigned issues and issues with open PRs
for e in data["copilot"]:
    lane_assignments[e["number"]] = LANE_READY
for e in data["excluded"]:
    lane_assignments[e["number"]] = LANE_READY

# Remaining active issues (no PRs, not stale, not Copilot)
for e in data["active"]:
    num = e["number"]
    if num in lane_assignments:
        continue  # already assigned above (e.g. has merged PRs)
    # If already in project with a valid lane, preserve it
    if num in existing_items and existing_items[num]["status"] in LANES:
        lane_assignments[num] = existing_items[num]["status"]
    # Seed from IssueAnalysis.md sections
    elif num in implemented_from_file:
        lane_assignments[num] = LANE_IMPLEMENTED
    elif num in ready_from_file:
        lane_assignments[num] = LANE_READY
    elif num in needs_from_file:
        lane_assignments[num] = LANE_NEEDS_DETAILS
    else:
        lane_assignments[num] = LANE_NEEDS_DETAILS   # default

counts = Counter(lane_assignments.values())
print("    Lane assignment summary:")
for lane in LANES:
    print(f"      {lane}: {counts.get(lane, 0)}")

# ── Step 4: Add / update project items ───────────────────────────────────────
print("    Updating project …")
added = updated = skipped = errors = 0

for issue_num, target_lane in sorted(lane_assignments.items()):
    node_id   = issue_node_ids.get(issue_num)
    option_id = options[target_lane]

    if not node_id:
        print(f"    WARNING: no node ID for issue #{issue_num}; skipping.", file=sys.stderr)
        errors += 1
        continue

    existing = existing_items.get(issue_num)

    if existing and existing["status"] == target_lane:
        skipped += 1
        continue

    if existing:
        # Update existing item's lane
        resp = run_graphql("""
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId
    itemId: $itemId
    fieldId: $fieldId
    value: { singleSelectOptionId: $optionId }
  }) {
    projectV2Item { id }
  }
}
""", fail_on_error=False,
     projectId=project_id, itemId=existing["item_id"],
     fieldId=field_id, optionId=option_id)
        if resp is None:
            errors += 1
        else:
            updated += 1
    else:
        # Add issue to project
        resp = run_graphql("""
mutation($projectId: ID!, $contentId: ID!) {
  addProjectV2ItemById(input: {
    projectId: $projectId
    contentId: $contentId
  }) {
    item { id }
  }
}
""", fail_on_error=False, projectId=project_id, contentId=node_id)
        if resp is None:
            errors += 1
            continue
        item_id = resp["data"]["addProjectV2ItemById"]["item"]["id"]
        # Set the lane
        resp2 = run_graphql("""
mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $projectId
    itemId: $itemId
    fieldId: $fieldId
    value: { singleSelectOptionId: $optionId }
  }) {
    projectV2Item { id }
  }
}
""", fail_on_error=False,
     projectId=project_id, itemId=item_id,
     fieldId=field_id, optionId=option_id)
        if resp2 is None:
            errors += 1
        else:
            added += 1

    time.sleep(0.05)   # stay well within GitHub's rate limit

print(f"    Done: added={added} updated={updated} skipped={skipped} errors={errors}")
if errors:
    print(f"    {errors} error(s) encountered – review warnings above.", file=sys.stderr)
PYEOF

echo ""
echo "==> Done."
