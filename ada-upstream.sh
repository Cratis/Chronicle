#!/usr/bin/env bash
# Launch a Claude Code session on Ada's upstream backlog for Chronicle.
#
#   ./ada-upstream.sh        start at item 1
#   ./ada-upstream.sh 7      start at item 7
#
# Reads ADA-UPSTREAM.md (this repo) plus the full reports in Ada's Planning tree.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ADA="/Volumes/sourcecode/repos/hive/Ada"
ITEM="${1:-1}"

[ -f "$REPO_ROOT/ADA-UPSTREAM.md" ] || { echo "ADA-UPSTREAM.md not found in $REPO_ROOT" >&2; exit 1; }
[ -d "$ADA/Planning" ]              || { echo "Ada Planning tree not found at $ADA/Planning" >&2; exit 1; }
command -v claude >/dev/null         || { echo "claude CLI not on PATH" >&2; exit 1; }

cd "$REPO_ROOT"
echo "==> Chronicle  ($(git rev-parse --abbrev-ref HEAD))  item $ITEM"
exec claude --add-dir "$ADA/Planning" "Read $REPO_ROOT/ADA-UPSTREAM.md in full before doing anything else. It is Ada's upstream backlog for this repo: 22 open items - 13 defect reports (fixes) and 9 improvement proposals (features and missing seams). Work item $ITEM end to end, following the discipline section of that file: re-verify every cited file:line at HEAD, reproduce at the right tier before fixing, mutation-prove the spec, run this repo's full gates with zero warnings, then report which report claims you confirmed, refuted or corrected. Do not start another item until I say so. /Volumes/sourcecode/repos/hive/Ada is READ-ONLY reference material - never create, edit or delete anything under it. Work on a branch. Do not push and do not open a PR."
