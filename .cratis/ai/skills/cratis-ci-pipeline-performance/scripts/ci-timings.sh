#!/usr/bin/env bash
# cratis-ai-managed: skills/cratis-ci-pipeline-performance/scripts/ci-timings.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Summarize the timing, failure and queue behavior of one GitHub Actions workflow
# from its recent runs. Read-only; uses gh, jq and POSIX utilities.
#
# Exit codes: 0 ok, 2 usage error, 3 no runs or no data, 4 missing gh/jq or gh auth failure.

set -u

usage() {
  cat <<'USAGE'
Usage: ci-timings.sh --repo OWNER/REPO --workflow FILE_OR_NAME [options]
       ci-timings.sh --self-test
       ci-timings.sh --help

Options:
  --repo OWNER/REPO       Repository to analyze (required).
  --workflow FILE_OR_NAME Workflow file name (ci.yml) or display name (required).
  --limit N               Runs to list, most recent first (default 50).
  --jobs N                Most recent successful first-attempt runs to fetch job
                          and step timings for (default 10).
  --branch B              Only runs on branch B.
  --event E               Only runs triggered by event E (pull_request, push, ...).
  --self-test             Run the analysis on bundled fixtures; needs no network or gh.

Prints a markdown report on stdout: conclusion counts, cancel/fail/rerun rates,
wall-clock median and p90, per-run queue, execution span and runner-minutes,
the slowest jobs and steps, and a classification hint.

Percentiles take the lower nearest rank: sorted[floor((n-1)*p)].
Exit codes: 0 ok, 2 usage error, 3 no runs or no data, 4 missing gh/jq or gh auth failure.
USAGE
}

die() { # die CODE MESSAGE
  code=$1
  shift
  printf 'ci-timings: %s\n' "$*" >&2
  exit "$code"
}

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

# Selects the runs whose job timings are fetched: successful first attempts, newest first.
read -r -d '' SAMPLE_JQ <<'JQ' || true
map(select(.status == "completed" and .conclusion == "success" and .attempt == 1))
| sort_by(.createdAt) | reverse | .[0:$n] | map(.databaseId)
JQ

# Inputs: --slurpfile runs (gh run list JSON), --slurpfile jobs ([{id, jobs: [API job objects]}]),
# --arg repo, --arg workflow, --arg filters, --argjson njobs.
read -r -d '' REPORT_JQ <<'JQ' || true
def ts: fromdateiso8601;
def pct($p): sort as $s | if ($s | length) == 0 then null else $s[((($s | length) - 1) * $p) | floor] end;
def f1: if . == null then "n/a" else ((. * 10 | round) as $r | "\($r / 10 | floor).\($r % 10)") end;
def mins: if . == null then null else . / 60 end;
def rate($a; $b): if $b == 0 then 0 else $a / $b * 100 end;
def dur($o): (($o.completed_at | ts) - ($o.started_at | ts));
def usable: select(.started_at != null and .completed_at != null and .conclusion != "skipped");

$runs[0] as $all
| ($all | map(select(.status == "completed" and .conclusion != "skipped"))) as $done
| ($done | map(select(.conclusion == "success"))) as $succ
| ($done | map(select(.conclusion == "failure"))) as $fail
| ($done | map(select(.conclusion == "cancelled"))) as $canc
| ($done | map(select(.attempt > 1))) as $rerun
| ($done | map(select((.conclusion == "success" or .conclusion == "failure") and .attempt > 1)) | length) as $rerunExcluded
| ($done | map(select((.conclusion == "success" or .conclusion == "failure") and .attempt == 1)
    | ((.updatedAt | ts) - (.createdAt | ts)))) as $walls
| (rate($fail | length; $done | length)) as $failP
| (rate($canc | length; $done | length)) as $cancP
| (rate($rerun | length; $done | length)) as $rerunP
| ($all | map({id: .databaseId, created: (.createdAt | ts), wall: ((.updatedAt | ts) - (.createdAt | ts))})) as $runIndex
| ($jobs[0]
   | map(. as $e
       | ($runIndex | map(select(.id == $e.id)) | first) as $r
       | select($r != null)
       | ($e.jobs | map(usable)) as $j
       | select(($j | length) > 0)
       | {id: $e.id, wall: $r.wall, jobs: $j,
          count: ($j | length),
          first: (($j | map(.started_at | ts) | min) - $r.created),
          maxq: ($j | map(((.started_at | ts) - ((.created_at // .started_at) | ts))) | max),
          sumq: ($j | map(((.started_at | ts) - ((.created_at // .started_at) | ts))) | add),
          span: (($j | map(.completed_at | ts) | max) - ($j | map(.started_at | ts) | min)),
          runner: ($j | map(dur(.)) | add)})) as $sample
| ($sample | map(.jobs | length) | add // 0) as $njobsTotal
| ($sample | map(.jobs[] | (.steps // [])[] | select(.started_at != null and .completed_at != null and .conclusion != "skipped")) | length) as $nsteps
| ($sample | map(.maxq / .wall * 100 | select(. != null))) as $shares
| (if ($shares | length) == 0 then null else ($shares | pct(0.5)) end) as $qshare
| ([ (if ($failP >= 15 or $rerunP >= 15) then "failure-bound" else empty end),
     (if $cancP >= 20 then "cancel-bound" else empty end),
     (if ($qshare != null and $qshare >= 25) then "queue-bound" else empty end) ]) as $flags
| (if ($flags | length) == 0 then "execution-bound" else $flags[0] end) as $primary
| (if ($flags | length) > 1 then " (also: " + ($flags[1:] | join(", ")) + ")" else "" end) as $also
| "@@COUNTS \($done | length) \($walls | length) \($sample | length) \($nsteps)",
  "# CI timings: \($repo) / \($workflow)",
  "",
  "Sample: \($all | length) runs listed\($filters), \($done | length) completed, \($all | map(select(.status != "completed")) | length) not finished, \($all | map(select(.conclusion == "skipped")) | length) skipped.",
  "Analyzed: \($done | length) completed runs, \($walls | length) first-attempt runs for wall clock, \($sample | length) sampled runs with \($njobsTotal) jobs and \($nsteps) steps.",
  (if ($done | length) < 10 then "Small sample: fewer than 10 completed runs; treat every figure as indicative." else empty end),
  "",
  "| Conclusion | Runs |",
  "|---|---|",
  ($all | map({k: (if .status == "completed" then .conclusion else .status end)})
        | group_by(.k) | map({k: .[0].k, n: length}) | sort_by([-.n, .k])
        | .[] | "| \(.k) | \(.n) |"),
  "",
  "Rates over \($done | length) completed runs: cancelled \($cancP | f1)%, failed \($failP | f1)%, rerun (attempt > 1) \($rerunP | f1)%.",
  "Wall clock (success and failure, first attempts only, \($walls | length) runs; \($rerunExcluded) rerun runs excluded): median \($walls | pct(0.5) | mins | f1) min, p90 \($walls | pct(0.9) | mins | f1) min.",
  "",
  (
     "## Per run (\($sample | length) most recent successful first-attempt runs)",
     "",
     "Wall is run creation to last update. First start wait is run creation to the first job start. Max job queue is the longest job wait for a runner. Execution span is first job start to last job end. Runner-min is the sum of job durations.",
     "",
     "| Run | Wall min | First start wait min | Max job queue min | Sum job queue min | Execution span min | Runner-min | Jobs |",
     "|---|---|---|---|---|---|---|---|",
     ($sample | .[] | "| \(.id) | \(.wall | mins | f1) | \(.first | mins | f1) | \(.maxq | mins | f1) | \(.sumq | mins | f1) | \(.span | mins | f1) | \(.runner | mins | f1) | \(.count) |"),
     "| median | \($sample | map(.wall) | pct(0.5) | mins | f1) | \($sample | map(.first) | pct(0.5) | mins | f1) | \($sample | map(.maxq) | pct(0.5) | mins | f1) | \($sample | map(.sumq) | pct(0.5) | mins | f1) | \($sample | map(.span) | pct(0.5) | mins | f1) | \($sample | map(.runner) | pct(0.5) | mins | f1) | \($sample | map(.count) | pct(0.5)) |",
     "",
     "## Slowest jobs (median minutes across the sampled runs)",
     "",
     "| Job | Median min | Runs |",
     "|---|---|---|",
     ($sample | map(.jobs[] | {k: .name, d: dur(.)}) | group_by(.k)
        | map({k: .[0].k, m: (map(.d) | pct(0.5)), n: length}) | sort_by([-.m, .k]) | .[0:8]
        | .[] | "| \(.k) | \(.m | mins | f1) | \(.n) |"),
     "",
     "## Slowest steps (median minutes across the sampled runs)",
     "",
     "| Job / step | Median min | Runs |",
     "|---|---|---|",
     ($sample | map(.jobs[] | .name as $jn | (.steps // [])[]
          | select(.started_at != null and .completed_at != null and .conclusion != "skipped")
          | {k: "\($jn) / \(.name)", d: dur(.)})
        | group_by(.k) | map({k: .[0].k, m: (map(.d) | pct(0.5)), n: length}) | sort_by([-.m, .k]) | .[0:8]
        | .[] | "| \(.k) | \(.m | mins | f1) | \(.n) |")
  ),
  "",
  "## Classification",
  "",
  "Classification: \($primary)\($also) (queue share \(if $qshare == null then "n/a" else ($qshare | f1) + "%" end), failed \($failP | f1)%, cancelled \($cancP | f1)%, rerun \($rerunP | f1)%).",
  "Hint only: thresholds are failure-bound at failed or rerun >= 15%, cancel-bound at cancelled >= 20%, queue-bound at median max-job-queue share of wall >= 25%, otherwise execution-bound. Confirm on the critical path of the required checks."
JQ

# report RUNS_JSON JOBS_JSON REPO WORKFLOW FILTERS NJOBS
# Every analyzed population must be non-empty; otherwise exit 3 with the reason on stderr.
report() {
  local count out counts done_n walls_n sample_n steps_n why
  count=$(jq 'length' "$1") || return 4
  if [ "$count" = "0" ]; then
    printf 'ci-timings: no runs found for %s / %s%s\n' "$3" "$4" "$5" >&2
    return 3
  fi
  out=$(jq -n -r --slurpfile runs "$1" --slurpfile jobs "$2" \
    --arg repo "$3" --arg workflow "$4" --arg filters "$5" --argjson njobs "$6" \
    "$REPORT_JQ") || return 4
  counts=$(printf '%s\n' "$out" | head -n 1)
  read -r _ done_n walls_n sample_n steps_n <<EOF_COUNTS
$counts
EOF_COUNTS
  why=""
  if [ "$done_n" = "0" ]; then
    why="$count runs found but none completed (all unfinished or skipped)"
  elif [ "$walls_n" = "0" ]; then
    why="no first-attempt run ended in success or failure (all cancelled, rerun or skipped)"
  elif [ "$sample_n" = "0" ]; then
    why="no successful first-attempt run has usable job timestamps; job, queue and runner-minute figures cannot be computed"
  elif [ "$steps_n" = "0" ]; then
    why="the sampled jobs carry no step timings"
  fi
  if [ -n "$why" ]; then
    printf 'ci-timings: no data for %s / %s%s: %s. Widen --limit, drop --branch/--event, or pick another workflow.\n' "$3" "$4" "$5" "$why" >&2
    return 3
  fi
  printf '%s\n' "$out" | tail -n +2
}

sample_ids() { # sample_ids RUNS_JSON N
  jq -r --argjson n "$2" "$SAMPLE_JQ | .[]" "$1"
}

expect_exit3() { # expect_exit3 LABEL RUNS JOBS (uses and updates failures of self_test)
  local out rc
  out=$(report "$2" "$3" "example/repo" "ci.yml" "" 3 2>/dev/null)
  rc=$?
  if [ "$rc" -ne 3 ] || [ -n "$out" ]; then
    printf 'FAIL %s: expected exit 3 and empty stdout, got exit %s\n' "$1" "$rc" >&2
    failures=$((failures + 1))
  fi
}

self_test() {
  local fx="$SCRIPT_DIR/fixtures" tmp out rc ids line
  failures=0
  command -v jq >/dev/null 2>&1 || die 4 "jq is required"
  tmp=$(mktemp -d) || die 4 "mktemp failed"
  trap 'rm -rf "$tmp"' EXIT

  ids=$(sample_ids "$fx/runs.json" 3 | tr '\n' ' ')
  if [ "$ids" != "1010 1007 1003 " ]; then
    printf 'FAIL sample selection: expected "1010 1007 1003 " got "%s"\n' "$ids" >&2
    failures=$((failures + 1))
  fi

  : >"$tmp/jobs.ndjson"
  for id in $ids; do
    jq -n --argjson id "$id" --slurpfile j "$fx/jobs/$id.json" '{id: $id, jobs: $j[0]}' >>"$tmp/jobs.ndjson"
  done
  jq -s '.' "$tmp/jobs.ndjson" >"$tmp/jobs.json"

  out=$(report "$fx/runs.json" "$tmp/jobs.json" "example/repo" "ci.yml" "" 3)
  rc=$?
  if [ "$rc" -ne 0 ]; then
    printf 'FAIL report exited %s\n' "$rc" >&2
    failures=$((failures + 1))
  fi

  while IFS= read -r line; do
    [ -z "$line" ] && continue
    if ! printf '%s\n' "$out" | grep -F -x -q -- "$line"; then
      printf 'FAIL missing expected line: %s\n' "$line" >&2
      failures=$((failures + 1))
    fi
  done <<'EXPECTED'
Sample: 10 runs listed, 8 completed, 1 not finished, 1 skipped.
Analyzed: 8 completed runs, 6 first-attempt runs for wall clock, 3 sampled runs with 9 jobs and 21 steps.
| success | 6 |
| cancelled | 1 |
| failure | 1 |
| in_progress | 1 |
| skipped | 1 |
Rates over 8 completed runs: cancelled 12.5%, failed 12.5%, rerun (attempt > 1) 12.5%.
Wall clock (success and failure, first attempts only, 6 runs; 1 rerun runs excluded): median 15.0 min, p90 25.0 min.
| 1010 | 25.0 | 1.2 | 3.0 | 5.0 | 11.0 | 11.0 | 3 |
| 1007 | 15.0 | 1.2 | 3.0 | 5.0 | 11.0 | 11.0 | 3 |
| 1003 | 30.0 | 1.2 | 3.0 | 5.0 | 16.0 | 16.0 | 3 |
| median | 25.0 | 1.2 | 3.0 | 5.0 | 11.0 | 11.0 | 3 |
| build | 5.0 | 3 |
| test | 5.0 | 3 |
| lint | 1.0 | 3 |
| test / dotnet test | 4.7 | 3 |
| build / dotnet build | 4.5 | 3 |
| lint / eslint | 0.8 | 3 |
Classification: execution-bound (queue share 12.0%, failed 12.5%, cancelled 12.5%, rerun 12.5%).
EXPECTED

  # A rerun that ended cancelled is not excluded from wall clock; only success or failure reruns are.
  jq 'map(if .databaseId == 1005 then .attempt = 2 else . end)' "$fx/runs.json" >"$tmp/rerun-cancelled.json"
  out=$(report "$tmp/rerun-cancelled.json" "$tmp/jobs.json" "example/repo" "ci.yml" "" 3)
  if ! printf '%s\n' "$out" | grep -F -x -q -- 'Wall clock (success and failure, first attempts only, 6 runs; 1 rerun runs excluded): median 15.0 min, p90 25.0 min.'; then
    printf 'FAIL rerun exclusion count: %s\n' "$(printf '%s\n' "$out" | grep -F 'Wall clock')" >&2
    failures=$((failures + 1))
  fi

  # A skipped job must not be counted as a job.
  if printf '%s\n' "$out" | grep -F -q '| deploy |'; then
    printf 'FAIL skipped job appeared in the report\n' >&2
    failures=$((failures + 1))
  fi

  # Non-vacuity: every empty population must exit 3 and print nothing on stdout.
  printf '[]\n' >"$tmp/empty.json"
  jq 'map(.conclusion = "cancelled")' "$fx/runs.json" >"$tmp/cancelled.json"
  jq 'map(.jobs |= map(.started_at = null))' "$tmp/jobs.json" >"$tmp/jobs-untimed.json"
  jq 'map(.jobs |= map(.steps = []))' "$tmp/jobs.json" >"$tmp/jobs-nosteps.json"
  expect_exit3 "empty run list" "$tmp/empty.json" "$tmp/jobs.json"
  expect_exit3 "all runs cancelled" "$tmp/cancelled.json" "$tmp/jobs.json"
  expect_exit3 "no job data" "$fx/runs.json" "$tmp/empty.json"
  expect_exit3 "jobs without timestamps" "$fx/runs.json" "$tmp/jobs-untimed.json"
  expect_exit3 "jobs without steps" "$fx/runs.json" "$tmp/jobs-nosteps.json"

  if [ "$failures" -ne 0 ]; then
    printf 'ci-timings self-test: %s failure(s)\n' "$failures" >&2
    exit 1
  fi
  printf 'ci-timings self-test: ok\n'
  exit 0
}

is_uint() { case "$1" in '' | *[!0-9]*) return 1 ;; *) [ "$1" -gt 0 ] ;; esac }

REPO=""
WORKFLOW=""
LIMIT=50
JOBS=10
BRANCH=""
EVENT=""
SELF_TEST=0

while [ $# -gt 0 ]; do
  case "$1" in
    --help | -h) usage; exit 0 ;;
    --self-test) SELF_TEST=1; shift ;;
    --repo | --workflow | --limit | --jobs | --branch | --event)
      [ $# -ge 2 ] || { usage >&2; die 2 "option $1 needs a value"; }
      case "$1" in
        --repo) REPO=$2 ;;
        --workflow) WORKFLOW=$2 ;;
        --limit) LIMIT=$2 ;;
        --jobs) JOBS=$2 ;;
        --branch) BRANCH=$2 ;;
        --event) EVENT=$2 ;;
      esac
      shift 2
      ;;
    *) usage >&2; die 2 "unknown argument: $1" ;;
  esac
done

if [ "$SELF_TEST" -eq 1 ]; then
  self_test
fi

[ -n "$REPO" ] || { usage >&2; die 2 "--repo is required"; }
[ -n "$WORKFLOW" ] || { usage >&2; die 2 "--workflow is required"; }
case "$REPO" in */*) ;; *) die 2 "--repo must be OWNER/REPO" ;; esac
is_uint "$LIMIT" || die 2 "--limit must be a positive integer"
is_uint "$JOBS" || die 2 "--jobs must be a positive integer"

command -v jq >/dev/null 2>&1 || die 4 "jq is required"
command -v gh >/dev/null 2>&1 || die 4 "gh is required"
gh auth status >/dev/null 2>&1 || die 4 "gh is not authenticated; run 'gh auth login'"

TMP=$(mktemp -d) || die 4 "mktemp failed"
trap 'rm -rf "$TMP"' EXIT

set -- run list --repo "$REPO" --workflow "$WORKFLOW" --limit "$LIMIT" \
  --json databaseId,event,headBranch,status,conclusion,attempt,createdAt,updatedAt
[ -z "$BRANCH" ] || set -- "$@" --branch "$BRANCH"
[ -z "$EVENT" ] || set -- "$@" --event "$EVENT"
FILTERS=""
[ -z "$BRANCH" ] || FILTERS="$FILTERS, branch $BRANCH"
[ -z "$EVENT" ] || FILTERS="$FILTERS, event $EVENT"

if ! gh "$@" >"$TMP/runs.json"; then
  die 3 "gh run list failed for $REPO / $WORKFLOW (see the gh message above)"
fi

: >"$TMP/jobs.ndjson"
fetched=0
for id in $(sample_ids "$TMP/runs.json" "$JOBS"); do
  if gh api --paginate "repos/$REPO/actions/runs/$id/jobs?per_page=100&filter=latest" --jq '.jobs[]' >"$TMP/job-$id.ndjson"; then
    jq -s --argjson id "$id" '{id: $id, jobs: .}' "$TMP/job-$id.ndjson" >>"$TMP/jobs.ndjson"
    fetched=$((fetched + 1))
  else
    printf 'ci-timings: skipping run %s: could not fetch its jobs\n' "$id" >&2
  fi
done
jq -s '.' "$TMP/jobs.ndjson" >"$TMP/jobs.json"

report "$TMP/runs.json" "$TMP/jobs.json" "$REPO" "$WORKFLOW" "$FILTERS" "$JOBS"
exit $?
