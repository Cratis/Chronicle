#!/usr/bin/env bash
# cratis-ai-managed: skills/cratis-engineering-consolidation-guard/scripts/verify-consolidation.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Prove that a consolidation or re-land branch still carries every change it claims to carry.
#
# Usage:
#   verify-consolidation.sh [options] <consolidation-ref> <source>...
#   verify-consolidation.sh --discover [--base <ref>] <consolidation-ref>
#   verify-consolidation.sh --self-test
#
# A <source> is one folded or re-landed change:
#   <ref>          a folded branch or pull-request head; its change is everything since merge-base(<base>, <ref>)
#   <from>..<ref>  the change <ref> made since merge-base(<from>, <ref>). Required for work that already
#                  landed once: <merge>^1..<merge>^2 for a merged pull request, <commit>^..<commit> for one commit.
#
# Checks, each reported per source:
#   A commits   source commits with no identical patch (git patch-id) in the consolidation's own commits;
#               git range-diff then classifies each as dropped or rewritten. Skipped when the source tip is an
#               ancestor of the consolidation, because every commit then pairs with itself whatever the
#               merge resolution threw away.
#   B content   every path the source touched: an added file must exist, a modified file must not still hold
#               the pre-change blob, a deleted file must not still hold the pre-change blob. This is the check
#               that sees a merge resolution reverting a file.
#   C specs     the consolidation carries at least as many specification files as the base plus every
#               source's net addition.
#
# Options:
#   --base <ref>              mainline the consolidation targets (default: origin/main)
#   --creation-factor <n>     git range-diff pairing aggressiveness, 0-100 (default: 90)
#   --allow-rewritten <sha>   accept one commit reported as rewritten after reading its range-diff. Repeatable.
#   --allow-spec-delta <n>    accept n fewer specification files than the ratchet expects (default: 0)
#   --spec-pattern <regex>    extended regex selecting specification files
#                             (default: (^|/)(for_|when_)[^/]*/.*\.(cs|ts|tsx)$)
#   --discover                print the sources recorded in the consolidation's history, one per line
#   --self-test               plant known defects in a scratch repository and require every one to be found
#
# Dispositions recorded in the consolidation's own commits count like the options, so CI honors them:
#   Consolidation-Allow-Rewritten: <sha>     same as --allow-rewritten <sha>
#   Consolidation-Allow-Spec-Delta: <n>      added to --allow-spec-delta
#
# Exit codes: 0 every source survives, 1 findings, 2 could not run (bad arguments, an unresolvable ref, or a
# source with nothing to compare).
set -euo pipefail

BASE="origin/main"
CREATION_FACTOR=90
ALLOW_SPEC_DELTA=0
ALLOWED_REWRITTEN=""
SPEC_PATTERN='(^|/)(for_|when_)[^/]*/.*\.(cs|ts|tsx)$'
MODE="verify"

usage() { awk 'NR < 5 { next } /^#/ { sub(/^# ?/, ""); print; next } { exit }' "$0"; }
die() { printf 'ERROR %s\n' "$*" >&2; exit 2; }
# A whole number in base 10 (so '08' is eight, not an octal error), or exit 2.
whole_number() { case "$2" in ''|*[!0-9]*) die "$1 must be a whole number, found '$2'." ;; esac; printf '%s' "$((10#$2))"; }

while [ $# -gt 0 ]; do
    case "$1" in
        --base) [ $# -ge 2 ] || die "--base needs a value"; BASE="$2"; shift 2 ;;
        --creation-factor) [ $# -ge 2 ] || die "--creation-factor needs a value"; CREATION_FACTOR="$(whole_number --creation-factor "$2")"; shift 2 ;;
        --allow-rewritten) [ $# -ge 2 ] || die "--allow-rewritten needs a value"; ALLOWED_REWRITTEN="$ALLOWED_REWRITTEN $2"; shift 2 ;;
        --allow-spec-delta) [ $# -ge 2 ] || die "--allow-spec-delta needs a value"; ALLOW_SPEC_DELTA="$(whole_number --allow-spec-delta "$2")"; shift 2 ;;
        --spec-pattern) [ $# -ge 2 ] || die "--spec-pattern needs a value"; SPEC_PATTERN="$2"; shift 2 ;;
        --discover) MODE="discover"; shift ;;
        --self-test) MODE="self-test"; shift ;;
        -h|--help) usage; exit 0 ;;
        --) shift; break ;;
        -*) die "unknown option: $1" ;;
        *) break ;;
    esac
done

commit_of() { git rev-parse --quiet --verify "$1^{commit}" 2>/dev/null || die "cannot resolve '$1' to a commit; fetch it first (a pull request: git fetch origin refs/pull/<n>/head:pr<n>)."; }
blob_of() { git rev-parse --quiet --verify "$1:$2" 2>/dev/null || true; }
is_ancestor() { git merge-base --is-ancestor "$1" "$2"; }
# True when an allowed entry (at least 7 hex characters) is a prefix of the full commit SHA.
is_allowed_rewrite() {
    local allowed
    for allowed in $ALLOWED_REWRITTEN; do
        [ "${#allowed}" -ge 7 ] || continue
        case "$1" in "$allowed"*) return 0 ;; esac
    done
    return 1
}

spec_count() {
    local count
    count="$(git ls-tree -r --name-only "$1" | grep -cE "$SPEC_PATTERN" || true)"
    printf '%s' "${count:-0}"
}

patch_ids() {
    # One line per non-merge commit with a diff: "<patch-id> <commit>".
    git log --no-merges -p --no-color --format='commit %H' "$1" | git patch-id --stable
}

# ---------------------------------------------------------------------------------------------------------
discover() {
    [ $# -eq 1 ] || die "--discover takes exactly one consolidation ref"
    local consolidation base_commit cons_base merge parent candidates="" reverted original parents
    consolidation="$(commit_of "$1")"
    base_commit="$(commit_of "$BASE")"
    cons_base="$(git merge-base "$base_commit" "$consolidation")"

    # Folded branches: every non-first parent of a merge that the base does not already contain.
    for merge in $(git rev-list --merges "$cons_base..$consolidation"); do
        for parent in $(git rev-list --parents -n 1 "$merge" | cut -d' ' -f3-); do
            is_ancestor "$parent" "$base_commit" || candidates="$candidates $parent"
        done
    done
    # A branch folded twice contributes each tip; an older tip is checked too, which only costs time.
    for parent in $candidates; do printf '%s\n' "$parent"; done | awk '!seen[$0]++'

    # Restorations recorded by git cherry-pick -x.
    git log --format=%B "$cons_base..$consolidation" \
        | sed -n 's/.*(cherry picked from commit \([0-9a-f]\{7,40\}\)).*/\1/p' \
        | sort -u | while read -r original; do printf '%s^..%s\n' "$original" "$original"; done

    # Re-lands recorded by git revert of a revert: the original change is what the reverted revert undid.
    git log --format=%B "$cons_base..$consolidation" \
        | sed -n 's/^This reverts commit \([0-9a-f]\{7,40\}\).*/\1/p' | sort -u \
        | while read -r reverted; do
            git rev-parse --quiet --verify "$reverted^{commit}" >/dev/null || die "a revert names $reverted, which is not in this clone; fetch it before discovering."
            original="$(git log -1 --format=%B "$reverted" | sed -n 's/^This reverts commit \([0-9a-f]\{7,40\}\).*/\1/p' | head -n 1)"
            [ -n "$original" ] || continue
            parents="$(git rev-list --parents -n 1 "$original" | wc -w | tr -d ' ')"
            if [ "$parents" -gt 2 ]; then printf '%s^1..%s^2\n' "$original" "$original"; else printf '%s^..%s\n' "$original" "$original"; fi
        done
}

# ---------------------------------------------------------------------------------------------------------
verify() {
    [ $# -ge 2 ] || { usage >&2; exit 2; }
    local consolidation base_commit cons_base source from to fork
    consolidation="$(commit_of "$1")"; shift
    base_commit="$(commit_of "$BASE")"
    cons_base="$(git merge-base "$base_commit" "$consolidation")"

    # Dispositions recorded as commit trailers in the consolidation's own history.
    local trailer
    for trailer in $(git log --format='%(trailers:key=Consolidation-Allow-Rewritten,valueonly)' "$cons_base..$consolidation"); do
        printf 'disposition: Consolidation-Allow-Rewritten %s\n' "$trailer"
        ALLOWED_REWRITTEN="$ALLOWED_REWRITTEN $trailer"
    done
    for trailer in $(git log --format='%(trailers:key=Consolidation-Allow-Spec-Delta,valueonly)' "$cons_base..$consolidation"); do
        trailer="$(whole_number Consolidation-Allow-Spec-Delta "$trailer")"
        printf 'disposition: Consolidation-Allow-Spec-Delta %s\n' "$trailer"
        ALLOW_SPEC_DELTA=$((ALLOW_SPEC_DELTA + trailer))
    done

    # Resolve every source before checking any, so a bad argument cannot produce a partial verdict.
    local resolved="" changed
    for source in "$@"; do
        case "$source" in
            *..*) from="${source%%..*}"; to="${source#*..}" ;;
            *) from="$BASE"; to="$source" ;;
        esac
        from="$(commit_of "$from")"; to="$(commit_of "$to")"
        fork="$(git merge-base "$from" "$to")" || die "'$source' shares no history with its base."
        if [ "$fork" = "$to" ]; then
            die "'$source' is already contained in its base, so there is nothing to compare. A change that landed once needs the <from>..<ref> form, for example <merge>^1..<merge>^2."
        fi
        changed="$(git diff --name-only "$fork" "$to" | wc -l | tr -d ' ')"
        [ "$changed" -gt 0 ] || die "'$source' changes no files relative to ${fork:0:12}; there is nothing to verify."
        resolved="$resolved $source|$fork|$to"
    done

    local failures=0 paths=0 commits=0 entry left status subject cons_ids="" unmatched range_line abbrev
    fail() { printf 'FAIL  %s\n' "$*"; failures=$((failures + 1)); }
    note() { printf '      %s\n' "$*"; }

    for entry in $resolved; do
        source="${entry%%|*}"; entry="${entry#*|}"; fork="${entry%%|*}"; to="${entry#*|}"
        printf '== %s (change since %s, tip %s)\n' "$source" "${fork:0:12}" "${to:0:12}"

        # --- Check A: commits ----------------------------------------------------------------------------
        if is_ancestor "$to" "$consolidation"; then
            note "commits: tip is an ancestor of the consolidation, so commit pairing proves nothing; the content check decides."
        else
            [ -n "$cons_ids" ] || cons_ids="$(patch_ids "$cons_base..$consolidation" | cut -d' ' -f1)"
            unmatched=""
            while read -r patch_id left; do
                [ -n "$patch_id" ] || continue
                commits=$((commits + 1))
                printf '%s\n' "$cons_ids" | grep -qxF "$patch_id" || unmatched="$unmatched $left"
            done < <(patch_ids "$fork..$to")
            for left in $unmatched; do
                subject="$(git log -1 --format=%s "$left")"
                status=""
                while read -r range_line; do
                    abbrev="$(printf '%s' "$range_line" | sed -E 's/^ *([0-9]+|-+): +([0-9a-f]+|-+) .*/\2/')"
                    case "$left" in "$abbrev"*) status="$(printf '%s' "$range_line" | sed -E 's/^ *([0-9]+|-+): +([0-9a-f]+|-+) +([<>=!]) .*/\3/')"; break ;; esac
                done < <(git range-diff --no-color --creation-factor="$CREATION_FACTOR" "$fork..$to" "$cons_base..$consolidation" | grep -E '^ *([0-9]+|-+): ')
                if [ "$status" = "!" ]; then
                    if is_allowed_rewrite "$left"; then
                        note "allowed rewrite: ${left:0:12} ($subject)"
                    else
                        fail "$source: commit ${left:0:12} ($subject) survives with a different patch. Read 'git range-diff $fork..$to $cons_base..$consolidation'; if the difference is only conflict resolution, re-run with --allow-rewritten ${left:0:12}."
                    fi
                elif [ -z "$status" ] || [ "$status" = "<" ]; then
                    fail "$source: commit ${left:0:12} ($subject) has no counterpart in the consolidation; it was dropped."
                else
                    fail "$source: commit ${left:0:12} ($subject) has no patch-identical counterpart, but range-diff pairs it ('$status'). Read 'git range-diff $fork..$to $cons_base..$consolidation' and decide."
                fi
            done
        fi

        # --- Check B: content ---------------------------------------------------------------------------
        local kind path dest fork_blob tip_blob cons_blob
        while IFS="$(printf '\t')" read -r kind path dest; do
            paths=$((paths + 1))
            case "$kind" in R*|C*) ;; *) dest="$path" ;; esac
            fork_blob="$(blob_of "$fork" "$path")"
            tip_blob="$(blob_of "$to" "$dest")"
            cons_blob="$(blob_of "$consolidation" "$dest")"
            case "$kind" in
                A*|R*|C*)
                    if [ -z "$cons_blob" ]; then fail "$source: added '$dest', which does not exist in the consolidation."
                    elif [ "$cons_blob" != "$tip_blob" ]; then note "review: '$dest' differs from the source tip (git diff $to $consolidation -- '$dest')"; fi
                    case "$kind" in R*) [ -z "$(blob_of "$consolidation" "$path")" ] || note "review: '$path' was renamed to '$dest' but still exists in the consolidation" ;; esac
                    ;;
                M*)
                    if [ -z "$cons_blob" ]; then fail "$source: modified '$path', which does not exist in the consolidation."
                    elif [ "$cons_blob" = "$fork_blob" ]; then fail "$source: modified '$path', but the consolidation still holds the pre-change version byte for byte; the change was lost."
                    elif [ "$cons_blob" != "$tip_blob" ]; then note "review: '$path' differs from the source tip (git diff $to $consolidation -- '$path')"; fi
                    ;;
                D*)
                    if [ -n "$cons_blob" ] && [ "$cons_blob" = "$fork_blob" ]; then fail "$source: deleted '$path', but the consolidation still holds the pre-change version; the deletion was lost."
                    elif [ -n "$cons_blob" ]; then note "review: '$path' was deleted by the source but exists, changed, in the consolidation"; fi
                    ;;
                *) note "unclassified change '$kind' to '$path'" ;;
            esac
        done < <(git diff --name-status --find-renames "$fork" "$to")
    done

    # --- Check C: specification ratchet -----------------------------------------------------------------
    local expected actual
    # Count from where the consolidation left the base, not the base tip: specification files the base gained
    # or lost since then are not the consolidation's to account for.
    expected="$(spec_count "$cons_base")"
    for entry in $resolved; do
        entry="${entry#*|}"; fork="${entry%%|*}"; to="${entry#*|}"
        expected=$(( expected + $(spec_count "$to") - $(spec_count "$fork") ))
    done
    actual="$(spec_count "$consolidation")"
    printf '== specification files: consolidation has %s, base plus sources account for %s\n' "$actual" "$expected"
    if [ "$actual" -lt $(( expected - ALLOW_SPEC_DELTA )) ]; then
        fail "specification ratchet: the consolidation carries $actual specification files where $expected are accounted for. Find the deleted ones; if two sources added the same path, re-run with --allow-spec-delta $(( expected - actual ))."
    fi

    printf '\nChecked %s source(s), %s path(s), %s commit(s) compared; %s finding(s).\n' "$#" "$paths" "$commits" "$failures"
    if [ "$failures" -ne 0 ]; then
        printf 'Every finding needs a disposition before this merges.\n'
        exit 1
    fi
}

# ---------------------------------------------------------------------------------------------------------
self_test() {
    local self result=0 output status
    self="$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
    SCRATCH="$(mktemp -d)"
    trap 'rm -rf "$SCRATCH"' EXIT
    local scratch="$SCRATCH"
    export HOME="$scratch" GIT_CONFIG_NOSYSTEM=1
    export GIT_AUTHOR_NAME=self-test GIT_AUTHOR_EMAIL=self-test@example.com
    export GIT_COMMITTER_NAME=self-test GIT_COMMITTER_EMAIL=self-test@example.com
    cd "$scratch"
    git init -q -b main
    commit() { git add -A && git commit -q -m "$1"; }
    mkdir -p src for_shared/when_used
    printf 'one\n' > src/shared.txt; printf 'spec\n' > for_shared/when_used/spec.cs; printf 'old\n' > src/obsolete.txt
    commit "Add base"

    git checkout -q -b first main
    printf 'first\n' > src/first.txt; mkdir -p for_first/when_added; printf 'spec\n' > for_first/when_added/spec.cs
    commit "Add first feature"
    printf 'one\nfirst\n' > src/shared.txt; git rm -q src/obsolete.txt
    commit "Extend shared and remove obsolete"
    git checkout -q -b second main
    printf 'second\n' > src/second.txt
    commit "Add second feature"

    expect() {
        local name="$1" code="$2" needle="$3"; shift 3
        set +e; output="$(cd "$scratch" && bash "$self" --base main "$@" 2>&1)"; status=$?; set -e
        if [ "$status" -ne "$code" ]; then
            printf 'SELF-TEST FAIL %s: exit %s, expected %s\n%s\n' "$name" "$status" "$code" "$output"; result=1
        elif [ -n "$needle" ] && ! printf '%s' "$output" | grep -qF -- "$needle"; then
            printf 'SELF-TEST FAIL %s: output lacks "%s"\n%s\n' "$name" "$needle" "$output"; result=1
        else
            printf 'self-test pass: %s\n' "$name"
        fi
    }

    # A clean consolidation by true merges.
    git checkout -q -b clean main
    git merge -q --no-ff -m "Fold first" first; git merge -q --no-ff -m "Fold second" second
    expect "clean consolidation passes" 0 "0 finding(s)" clean first second

    # The base moves on after the consolidation branched: its new specification files are not the consolidation's.
    git checkout -q main
    mkdir -p for_later/when_added; printf 'spec\n' > for_later/when_added/spec.cs
    commit "Add a later spec on main"
    expect "base gaining a spec after the branch point passes" 0 "0 finding(s)" clean first second
    expect "non-numeric spec delta cannot run" 2 "whole number" --allow-spec-delta abc clean first second
    expect "leading-zero spec delta is decimal" 0 "0 finding(s)" --allow-spec-delta 08 clean first second

    # A merge resolution that throws away a file, a modification, a deletion and a spec.
    git checkout -q -b lossy main
    git merge -q --no-ff -m "Fold second" second
    git merge -q --no-ff --no-commit first >/dev/null 2>&1
    git checkout -q main -- src/shared.txt src/obsolete.txt
    git rm -q --cached for_first/when_added/spec.cs; rm -f for_first/when_added/spec.cs
    git commit -q -m "Fold first"
    expect "lost added spec found" 1 "added 'for_first/when_added/spec.cs'" lossy first second
    expect "reverted modification found" 1 "modified 'src/shared.txt'" lossy first second
    expect "lost deletion found" 1 "deleted 'src/obsolete.txt'" lossy first second
    expect "spec ratchet trips" 1 "specification ratchet" lossy first second

    # A cherry-picked consolidation that leaves one commit behind.
    git checkout -q -b picked main
    git cherry-pick -x first~1 >/dev/null
    expect "dropped commit found" 1 "has no counterpart" picked first

    # A re-land: landed, reverted, restored by reverting the revert.
    # A restoration whose cherry-pick needed a different patch, then accepted through a recorded disposition.
    local first_commit; first_commit="$(git rev-parse first~1)"
    git checkout -q -b rewritten main
    git cherry-pick -n "$first_commit" >/dev/null
    printf 'first, resolved\n' > src/first.txt
    commit "Restore first feature"
    expect "rewritten commit found" 1 "different patch" rewritten "$first_commit^..$first_commit"
    git commit -q --allow-empty -m "Accept the resolved first feature" -m "Consolidation-Allow-Rewritten: ${first_commit:0:12}"
    expect "recorded disposition accepts the rewrite" 0 "allowed rewrite" rewritten "$first_commit^..$first_commit"

    git checkout -q main
    git merge -q --no-ff -m "Land first" first
    local landed; landed="$(git rev-parse HEAD)"
    git revert -m 1 --no-edit "$landed" >/dev/null
    local revert; revert="$(git rev-parse HEAD)"
    git checkout -q -b reland main
    git revert --no-edit "$revert" >/dev/null
    expect "clean re-land passes" 0 "0 finding(s)" reland "$landed^1..$landed^2"
    expect "landed source without a range cannot run" 2 "already contained" reland first
    expect "discover finds the re-land" 0 "$landed^1..$landed^2" --discover reland
    git checkout -q -b lossy-reland main
    git revert --no-edit --no-commit "$revert"
    git rm -q -r --cached for_first; rm -rf for_first
    git commit -q -m "Re-land first"
    expect "re-land that drops a spec found" 1 "added 'for_first/when_added/spec.cs'" lossy-reland "$landed^1..$landed^2"

    expect "discover finds folded tips" 0 "$(git rev-parse second)" --discover clean
    expect "unknown ref cannot run" 2 "cannot resolve" clean no-such-ref

    if [ "$result" -ne 0 ]; then printf 'Self-test failed: a planted defect was not reported as expected.\n'; exit 1; fi
    printf 'Self-test passed: every planted defect was found.\n'
}

case "$MODE" in
    discover) discover "$@" ;;
    self-test) self_test ;;
    *) verify "$@" ;;
esac
