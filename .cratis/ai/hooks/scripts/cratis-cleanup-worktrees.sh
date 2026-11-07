#!/usr/bin/env bash
# cratis-ai-managed: hooks/scripts/cratis-cleanup-worktrees.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Deterministic cleanup of delivered work: linked worktrees and local branches whose work has
# reached the default branch. One script, every harness: Claude Code and Pi run it from hooks, and
# the ship-changes prompt runs it as a mandatory last step for harnesses without hooks.
#
# A worktree or branch is removed only on EVIDENCE that its work was delivered, never on a guess:
#   1. its tip is merged into origin/<default> AND was integrated (it is not simply sitting on the
#      default branch's first-parent history, which is where a brand-new, empty branch sits), or
#   2. its tip is merged into origin/<default> and its upstream is gone (the PR branch was deleted), or
#   3. GitHub reports a merged pull request whose head commit is exactly this tip.
# Anything else is KEPT and reported with the reason. Nothing is ever forced: a dirty, locked or
# current worktree is kept, as is one holding .ai-work/ records unless explicitly opted in.
# `git worktree remove` is never given --force.
#
# Usage:
#   cratis-cleanup-worktrees.sh [--dry-run] [--quiet] [--no-fetch] [--include-work-records]
#                                                                 run in the repository (any worktree)
#   --include-work-records                                       allow deletion of .ai-work/ records
#                                                                 by hand only; ignored in hook mode
#   cratis-cleanup-worktrees.sh --hook                                Claude hook mode: reads the hook JSON
#                                                                     on stdin, acts only when relevant
#   cratis-cleanup-worktrees.sh --self-test                           plants cases and fails if any is wrong
#
# Exit: 0 ran (kept items are not failures), 2 could not run. Opt out: CRATIS_HOOKS_SKIP_CLEANUP=1.
# Portable: bash 3.2, BSD + GNU userland.
# shellcheck disable=SC2016  # the self-test checks are single-quoted on purpose: they are eval-ed later
set -uo pipefail

DRY=0; QUIET=0; FETCH=1; HOOK=0; SELFTEST=0; INCLUDE_WORK_RECORDS=0
for a in "$@"; do
    case "$a" in
        --dry-run) DRY=1 ;; --quiet) QUIET=1 ;; --no-fetch) FETCH=0 ;;
        --hook) HOOK=1; QUIET=1 ;; --self-test) SELFTEST=1 ;;
        --include-work-records) INCLUDE_WORK_RECORDS=1 ;;
        *) echo "cratis-cleanup-worktrees: unknown argument '$a'" >&2; exit 2 ;;
    esac
done

# ── Hook mode: decide whether this event is worth a sweep ─────────────────────
EVENT="PostToolUse"
if [ "$HOOK" = 1 ]; then
    INCLUDE_WORK_RECORDS=0
    payload=''; IFS= read -r -d '' payload || true
    [ "${CRATIS_HOOKS_SKIP_CLEANUP:-}" = 1 ] && exit 0
    case "$payload" in *'"SessionStart"'*) EVENT="SessionStart" ;; esac
    if [ "$EVENT" = "PostToolUse" ]; then
        flat=${payload//\\n/ }; flat=${flat//\\\"/\"}
        # Only commands that deliver or integrate work; everything else costs nothing.
        if [[ ! "$flat" =~ (gh[[:space:]]+pr[[:space:]]+merge|git[[:space:]]+(-C[[:space:]]+[^[:space:]]+[[:space:]]+)?(pull|merge|worktree[[:space:]]+(add|remove|prune))|git[[:space:]]+push[[:space:]].*--delete) ]]; then
            exit 0
        fi
    fi
    cwd=""
    command -v jq >/dev/null 2>&1 && cwd=$(printf '%s' "$payload" | jq -r '.cwd // empty' 2>/dev/null)
    if [ -n "$cwd" ] && [ -d "$cwd" ]; then cd "$cwd" || exit 0; fi
fi
[ "${CRATIS_HOOKS_SKIP_CLEANUP:-}" = 1 ] && [ "$SELFTEST" = 0 ] && exit 0

# ── Self-test ─────────────────────────────────────────────────────────────────
if [ "$SELFTEST" = 1 ]; then
    self=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
    t=$(mktemp -d "${TMPDIR:-/tmp}/cleanup-selftest.XXXXXX"); trap 'rm -rf "$t"' EXIT
    t=$(cd "$t" && pwd -P)
    export GIT_AUTHOR_NAME=t GIT_AUTHOR_EMAIL=t@t GIT_COMMITTER_NAME=t GIT_COMMITTER_EMAIL=t@t CRATIS_CLEANUP_NO_GH=1
    git init -q --bare -b main "$t/origin.git"
    git clone -q "$t/origin.git" "$t/repo" 2>/dev/null; cd "$t/repo" || exit 2
    git checkout -q -b main 2>/dev/null; echo a > a; git add a; git commit -qm base; git push -q origin main
    mk() { git worktree add -q -b "$1" "$t/wt-$1" main; }
    mk delivered;  (cd "$t/wt-delivered" && echo d > d && git add d && git commit -qm d)
    mk dirty;      (cd "$t/wt-dirty" && echo x > x && git add x && git commit -qm x)
    mk unmerged;   (cd "$t/wt-unmerged" && echo u > u && git add u && git commit -qm u)
    mk fresh                                    # brand new, no commits: must survive
    for b in records empty-records locked locked-detached; do
        mk "$b"; (cd "$t/wt-$b" && echo "$b" > "$b" && git add "$b" && git commit -qm "$b")
        git merge -q --no-ff "$b" -m "merge $b"
    done
    mkdir "$t/wt-records/.ai-work" "$t/wt-empty-records/.ai-work"
    echo '.ai-work/' >> "$t/repo/.git/info/exclude"
    echo handoff > "$t/wt-records/.ai-work/.handoff"
    git -C "$t/wt-locked-detached" checkout -q --detach
    git worktree lock "$t/wt-locked"
    git worktree lock --reason pinned "$t/wt-locked-detached"
    git branch loose-delivered main; (git checkout -q loose-delivered && echo l > l && git add l && git commit -qm l && git checkout -q main)
    git merge -q --no-ff delivered -m "merge delivered" && git merge -q --no-ff dirty -m "merge dirty" && git merge -q --no-ff loose-delivered -m "merge loose"
    git push -q origin main
    echo junk > "$t/wt-dirty/untracked"          # makes the merged worktree dirty
    out=$("$self" --no-fetch 2>&1) || { echo "FAIL: exit $?"; echo "$out"; exit 1; }
    fail=0; chk() { if ! eval "$2"; then echo "FAIL: $1"; fail=1; fi; }
    chk "delivered worktree removed"   '[ ! -d "$t/wt-delivered" ]'
    chk "delivered branch removed"     '! git rev-parse -q --verify refs/heads/delivered >/dev/null'
    chk "loose merged branch removed"  '! git rev-parse -q --verify refs/heads/loose-delivered >/dev/null'
    chk "dirty worktree kept"          '[ -d "$t/wt-dirty" ]'
    chk "unmerged worktree kept"       '[ -d "$t/wt-unmerged" ]'
    chk "unmerged branch kept"         'git rev-parse -q --verify refs/heads/unmerged >/dev/null'
    chk "fresh worktree kept"          '[ -d "$t/wt-fresh" ]'
    chk "fresh branch kept"            'git rev-parse -q --verify refs/heads/fresh >/dev/null'
    chk "main kept"                    'git rev-parse -q --verify refs/heads/main >/dev/null'
    chk "report names the removal"     'grep -q "removed worktree" <<<"$out"'
    chk "work records kept"            '[ -f "$t/wt-records/.ai-work/.handoff" ]'
    chk "work records branch kept"     'git rev-parse -q --verify refs/heads/records >/dev/null'
    chk "work records reason reported" 'grep -Fq "has untracked work records (.ai-work/)" <<<"$out"'
    chk "empty work records removed"   '[ ! -d "$t/wt-empty-records" ]'
    chk "locked worktree kept"         '[ -d "$t/wt-locked" ]'
    chk "locked branch kept"           'git rev-parse -q --verify refs/heads/locked >/dev/null'
    chk "locked reason reported"       'grep -Fq "worktree $t/wt-locked (locked): locked" <<<"$out"'
    chk "locked detached kept"         '[ -d "$t/wt-locked-detached" ]'
    chk "locked detached reported"     'grep -Fq "worktree $t/wt-locked-detached (detached): locked" <<<"$out"'
    out=$(printf '%s' '{"hook_event_name":"SessionStart"}' | "$self" --no-fetch --include-work-records --hook 2>&1) || { echo "FAIL: hook exit $?"; echo "$out"; exit 1; }
    chk "hook ignores opt-in"          '[ -f "$t/wt-records/.ai-work/.handoff" ]'
    out=$("$self" --no-fetch --include-work-records 2>&1) || { echo "FAIL: opt-in exit $?"; echo "$out"; exit 1; }
    chk "opt-in removes work records"  '[ ! -d "$t/wt-records" ]'
    chk "opt-in removes branch"        '! git rev-parse -q --verify refs/heads/records >/dev/null'
    chk "opt-in keeps locked"          '[ -d "$t/wt-locked" ]'
    chk "opt-in keeps locked detached" '[ -d "$t/wt-locked-detached" ]'
    [ "$fail" = 0 ] && echo "cratis-cleanup-worktrees self-test: 24 checks passed" && exit 0
    echo "$out"; exit 1
fi

# ── Locate the repository ─────────────────────────────────────────────────────
git rev-parse --git-dir >/dev/null 2>&1 || { [ "$QUIET" = 1 ] || echo "cratis-cleanup-worktrees: not in a git repository" >&2; exit "$([ "$HOOK" = 1 ] && echo 0 || echo 2)"; }
here=$(pwd -P)
main_wt=$(git worktree list --porcelain | awk 'NR==1 && $1=="worktree"{sub(/^worktree /,""); print; exit}')
if [ ! -d "$main_wt" ]; then echo "cratis-cleanup-worktrees: cannot find the main worktree" >&2; exit 2; fi
cd "$main_wt" || exit 2

if [ "$FETCH" = 1 ]; then git fetch --prune --quiet origin >/dev/null 2>&1 || LOG_NOFETCH=1; fi
default=$(git symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>/dev/null || true)
[ -n "$default" ] || { for c in origin/main origin/master; do git rev-parse -q --verify "refs/remotes/$c" >/dev/null && default=$c && break; done; }
[ -n "$default" ] || { [ "$QUIET" = 1 ] || echo "cratis-cleanup-worktrees: no origin default branch; nothing to compare against" >&2; exit "$([ "$HOOK" = 1 ] && echo 0 || echo 2)"; }
default_local=${default#origin/}

firstparent=$(mktemp "${TMPDIR:-/tmp}/cleanup-fp.XXXXXX"); trap 'rm -f "$firstparent"' EXIT
git rev-list --first-parent "$default" > "$firstparent" 2>/dev/null

REMOVED=(); KEPT=(); FREED_KB=0
note_removed() { REMOVED+=("$1"); }
note_kept() { KEPT+=("$1"); }

# The cache is available only after a successful, valid batch. Failed attempts also stamp the window.
GH_PULLS=''
valid_pulls() {
    jq -e -s 'length == 1 and (.[0] | type == "array" and all(.[];
        type == "object" and has("merged_at") and (.merged_at == null or (.merged_at | type == "string")) and
        (.head | type == "object" and (.ref | type == "string") and (.sha | type == "string"))))' "$1" >/dev/null 2>&1
}
lock_stat() { stat -c '%i %Y' "$1" 2>/dev/null || stat -f '%i %m' "$1" 2>/dev/null; }
fresh_stamp() {
    local stamp
    stamp=$(cat "$state/stamp" 2>/dev/null || true)
    [[ "$stamp" =~ ^[0-9]+$ ]] && [ "$now" -lt "$((stamp + 900))" ]
}
cached_pulls() {
    fresh_stamp && [ ! -e "$state/failed" ] && valid_pulls "$state/pulls.json" || return 1
    cat "$state/pulls.json"
}
take_lock() {
    mkdir "$state/lock" 2>/dev/null && return 0
    local inode modified retired acquired current
    read -r inode modified < <(lock_stat "$state/lock")
    [[ "$modified" =~ ^[0-9]+$ ]] && [ "$now" -ge "$((modified + 900))" ] || return 1
    # Rename atomically: a killed reclaimer can leave only a retired directory, never a blocking marker.
    retired="$state/lock.stale.$$.$RANDOM"
    mv "$state/lock" "$retired" 2>/dev/null || return 1
    read -r current _ < <(lock_stat "$retired")
    if [ "$current" != "$inode" ]; then
        # Another reclaimer replaced the stale directory. Restore its lock if possible;
        # never acquire or delete a directory that was not the stale one we inspected.
        [ -e "$state/lock" ] || mv "$retired" "$state/lock" 2>/dev/null
        return 1
    fi
    acquired=1
    mkdir "$state/lock" 2>/dev/null && acquired=0
    rm -rf "$retired"
    return "$acquired"
}
hook_pulls() (
    local repo="$1" common state now temporary lock_inode current
    common=$(git rev-parse --git-common-dir) || exit 1
    state="$common/cratis-cleanup-gh/$repo"
    now=$(date +%s)
    mkdir -p "$state" 2>/dev/null || exit 1
    if ! take_lock; then cached_pulls; exit $?; fi
    read -r lock_inode _ < <(lock_stat "$state/lock")
    temporary=''
    # A retired owner must never remove its replacement's lock.
    trap '[[ -z "$temporary" ]] || rm -f "$temporary"; read -r current _ < <(lock_stat "$state/lock"); [ "$current" != "$lock_inode" ] || rmdir "$state/lock" 2>/dev/null' EXIT
    if fresh_stamp; then cached_pulls; exit $?; fi
    temporary=$(mktemp "$state/pulls.XXXXXX") || exit 1
    printf '%s\n' "$now" > "$temporary" && mv "$temporary" "$state/failed" || exit 1
    printf '%s\n' "$now" > "$temporary" && mv "$temporary" "$state/stamp" || exit 1
    read -r current _ < <(lock_stat "$state/lock")
    [ "$current" = "$lock_inode" ] || exit 1
    # Only the latest 100 closed PRs are searched; older merged branches are simply kept.
    if gh api "repos/$repo/pulls?state=closed&per_page=100" > "$temporary" 2>/dev/null && valid_pulls "$temporary"; then
        read -r current _ < <(lock_stat "$state/lock")
        [ "$current" = "$lock_inode" ] || exit 1
        mv "$temporary" "$state/pulls.json" && rm -f "$state/failed" || exit 1
        cat "$state/pulls.json"
    else exit 1; fi
)
GH_POSSIBLE=0; GH_REPO=''
configure_github() {
    [ "${CRATIS_CLEANUP_NO_GH:-}" != 1 ] || return 0
    command -v gh >/dev/null 2>&1 && command -v jq >/dev/null 2>&1 || return 0
    local remote repo
    remote=$(git remote get-url origin 2>/dev/null) || return 0
    case "$remote" in
        https://github.com/*) repo=${remote#https://github.com/} ;;
        git@github.com:*) repo=${remote#git@github.com:} ;;
        ssh://git@github.com/*) repo=${remote#ssh://git@github.com/} ;;
        *) return 0 ;;
    esac
    repo=${repo%.git}
    [[ "$repo" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || return 0
    GH_REPO=$repo; GH_POSSIBLE=1
}
configure_github
load_pulls() {
    if [ "$HOOK" = 1 ]; then GH_PULLS=$(hook_pulls "$GH_REPO") || return 1
    else GH_PULLS=$(gh api "repos/$GH_REPO/pulls?state=closed&per_page=100" 2>/dev/null) || return 1; fi
    printf '%s' "$GH_PULLS" | valid_pulls /dev/stdin
}

# Ancestry evidence is resolved during planning; GitHub evidence only after the complete plan exists.
ancestry_delivered() {
    local sha="$1" track="$3"
    if git merge-base --is-ancestor "$sha" "$default" 2>/dev/null; then
        if ! grep -qx "$sha" "$firstparent"; then EVIDENCE="merged into $default"; return 0; fi
        if [ "$track" = "[gone]" ]; then EVIDENCE="merged into $default, upstream gone"; return 0; fi
    fi
    return 1
}
github_delivered() {
    printf '%s' "$GH_PULLS" | jq -e --arg br "$1" --arg sha "$2" \
        'any(.[]; .merged_at != null and .head.ref == $br and .head.sha == $sha)' >/dev/null 2>&1
}
PLAN_KIND=(); PLAN_PATH=(); PLAN_SHA=(); PLAN_BRANCH=(); PLAN_EVIDENCE=(); NEEDS_GH=0
plan_item() {
    local kind="$1" path="$2" tip="$3" branch="$4" evidence=''
    if ancestry_delivered "$tip" "$branch" "$([ -n "$branch" ] && track_of "$branch")"; then evidence=$EVIDENCE
    elif [ -n "$branch" ] && [ "$GH_POSSIBLE" = 1 ]; then NEEDS_GH=1; fi
    PLAN_KIND+=("$kind"); PLAN_PATH+=("$path"); PLAN_SHA+=("$tip")
    PLAN_BRANCH+=("$branch"); PLAN_EVIDENCE+=("$evidence")
}
track_of() { git for-each-ref --format='%(upstream:track)' "refs/heads/$1" 2>/dev/null; }

# ── Plan the entire sweep without removing worktrees or branches ──────────────
wt=""; sha=""; br=""; locked=0; first=1
process_worktree() {
    [ -n "$wt" ] || return 0
    if [ "$first" = 1 ]; then first=0; return 0; fi        # the main worktree is never removed
    local name="${br:-detached}"
    if [ "$locked" = 1 ]; then note_kept "worktree $wt ($name): locked"; return 0; fi
    case "$here/" in "$wt"/*) note_kept "worktree $wt ($name): it is the current directory - run the sweep from the main checkout"; return 0 ;; esac
    plan_item worktree "$wt" "$sha" "$br"
}
execute_worktree() {
    local name="${br:-detached}" ev="$1" current
    current=$(git -C "$wt" rev-parse HEAD 2>/dev/null) || return 0
    [ "$current" = "$sha" ] || { note_kept "worktree $wt ($name): tip changed since planning"; return 0; }
    if [ "$INCLUDE_WORK_RECORDS" = 0 ] && [ -d "$wt/.ai-work" ]; then
        local records
        if ! records=$(find "$wt/.ai-work/." -mindepth 1 -maxdepth 1 -print -quit); then
            note_kept "worktree $wt ($name): could not inspect work records (.ai-work/)"; return 0
        fi
        if [ -n "$records" ]; then note_kept "worktree $wt ($name): has untracked work records (.ai-work/)"; return 0; fi
    fi
    if [ -n "$(git -C "$wt" status --porcelain 2>/dev/null)" ]; then note_kept "worktree $wt ($name): delivered but has uncommitted changes"; return 0; fi
    local kb; kb=$(du -sk "$wt" 2>/dev/null | awk '{print $1}')
    if [ "$DRY" = 1 ]; then note_removed "worktree $wt ($name): would remove, $ev"; return 0; fi
    if git worktree remove "$wt" >/dev/null 2>&1; then
        FREED_KB=$((FREED_KB + ${kb:-0})); note_removed "worktree $wt ($name): $ev"
    else note_kept "worktree $wt ($name): git refused to remove it"; fi
}
while IFS= read -r line; do
    case "$line" in
        "worktree "*) process_worktree; wt=${line#worktree }; sha=""; br=""; locked=0 ;;
        "HEAD "*) sha=${line#HEAD } ;;
        "branch "*) br=${line#branch refs/heads/} ;;
        "locked"*) locked=1 ;;
    esac
done < <(git worktree list --porcelain; echo)
process_worktree

in_use=$(git worktree list --porcelain | sed -n 's#^branch refs/heads/##p')
planned_branches=$(printf '%s\n' "${PLAN_BRANCH[@]+"${PLAN_BRANCH[@]}"}")
while IFS='|' read -r b bsha; do
    if [ -z "$b" ] || [ "$b" = "$default_local" ]; then continue; fi
    # Drain producers under pipefail: grep -q can turn a match into a SIGPIPE failure.
    if printf '%s\n' "$in_use" | grep -x "$b" >/dev/null; then
        [ "$DRY" = 0 ] || continue
        printf '%s\n' "$planned_branches" | grep -x "$b" >/dev/null || continue
    fi
    plan_item branch "$b" "$bsha" "$b"
done < <(git for-each-ref --format='%(refname:short)|%(objectname)' refs/heads)

abort_sweep() {
    echo "cratis-cleanup-worktrees: GitHub evidence unavailable; 0 removed, keeping the entire sweep" >&2
    exit 0
}
# Resolve every item's evidence before any deletion, including ancestry-proven items.
if [ "$NEEDS_GH" = 1 ] && ! load_pulls; then abort_sweep; fi
for ((i=0; i<${#PLAN_KIND[@]}; i++)); do
    if [ -z "${PLAN_EVIDENCE[i]}" ] && [ -n "${PLAN_BRANCH[i]}" ] && [ -n "$GH_PULLS" ]; then
        if github_delivered "${PLAN_BRANCH[i]}" "${PLAN_SHA[i]}"; then PLAN_EVIDENCE[i]="pull request merged at this commit"
        else result=$?; [ "$result" = 1 ] || abort_sweep; fi
    fi
done

# ── Execute the resolved plan, rechecking tips and worktree safety ─────────────
git worktree prune >/dev/null 2>&1
for ((i=0; i<${#PLAN_KIND[@]}; i++)); do
    kind=${PLAN_KIND[i]}; path=${PLAN_PATH[i]}; sha=${PLAN_SHA[i]}; br=${PLAN_BRANCH[i]}; ev=${PLAN_EVIDENCE[i]}
    if [ "$kind" = branch ]; then
        git worktree list --porcelain | sed -n 's#^branch refs/heads/##p' | grep -x "$br" >/dev/null && continue
    fi
    if [ -z "$ev" ]; then
        if [ "$kind" = worktree ]; then note_kept "worktree $path (${br:-detached}): work not delivered to $default"
        else note_kept "branch $br: work not delivered to $default"; fi
        continue
    fi
    if [ "$kind" = worktree ]; then wt=$path; execute_worktree "$ev"; continue; fi
    [ "$(git rev-parse -q --verify "refs/heads/$br")" = "$sha" ] || { note_kept "branch $br: tip changed since planning"; continue; }
    if [ "$DRY" = 1 ]; then note_removed "branch $br: would delete, $ev"
    elif git branch -D "$br" >/dev/null 2>&1; then note_removed "branch $br: $ev"
    else note_kept "branch $br: git refused to delete it"; fi
done

# ── Report ────────────────────────────────────────────────────────────────────
summary="cratis-cleanup-worktrees: ${#REMOVED[@]} removed, ${#KEPT[@]} kept"
[ "$FREED_KB" -gt 0 ] && summary="$summary, $((FREED_KB / 1024)) MB freed"
[ "${LOG_NOFETCH:-0}" = 1 ] && summary="$summary (fetch failed: compared against the last fetched $default)"
if [ "$HOOK" = 1 ]; then
    [ "${#REMOVED[@]}" -gt 0 ] || { [ "$EVENT" = "SessionStart" ] && [ "${#KEPT[@]}" -gt 0 ]; } || exit 0
    msg="$summary."
    [ "${#KEPT[@]}" -gt 0 ] && msg="$msg Kept (not delivered or not safe to remove): $(printf '%s; ' "${KEPT[@]}")"
    command -v jq >/dev/null 2>&1 && jq -nc --arg m "$msg" --arg e "$EVENT" '{systemMessage:$m,hookSpecificOutput:{hookEventName:$e,additionalContext:$m}}' || printf '%s\n' "$msg" >&2
    exit 0
fi
if [ "$QUIET" = 0 ]; then
    echo "$summary"
    for r in "${REMOVED[@]+"${REMOVED[@]}"}"; do echo "  removed $r"; done
    for k in "${KEPT[@]+"${KEPT[@]}"}"; do echo "  kept    $k"; done
fi
exit 0
