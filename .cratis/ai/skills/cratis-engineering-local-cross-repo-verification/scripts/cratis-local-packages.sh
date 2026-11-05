#!/usr/bin/env bash
# cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/scripts/cratis-local-packages.sh
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

usage() {
    cat <<'HELP'
Usage: cratis-local-packages.sh [--dry-run] COMMAND [ARGS]
Portable local package verification for macOS/Linux (Bash 3.2+). Windows: use WSL.
Exit codes: 0 success, 1 findings/failed operation, 2 cannot run/invalid input.

Commands (COMMAND --help shows these instructions too):
  stamp [--base-version X.Y.Z] PRODUCER [PRODUCER...]
    Next patch of the highest reachable vX.Y.Z tag, UTC minute and HEAD sha7.
    With several producers the highest next patch wins (one stamp for a multi-hop
    chain, so no stamped package sorts below a released pin) and sha7 is the last
    producer's; every HEAD is printed to stderr.
  stamp --train [--base-version X.Y.Z] PRODUCER [PRODUCER...]
    --base-version overrides the tag-derived base (strict SemVer core, no suffix).
    Train stamp X.Y.Z-train.g<sha7>: the same next patch, no minute, so one code
    state always yields one stamp. Accepted everywhere a local stamp is.
  closure [--map FILE] [--train] CONSUMER=PATH PRODUCER=PATH...
    Read-only. Print, from the same plan, the commands to run in order: one stamp
    (--train: the train stamp), per producer the NuGet overlay of its upstream
    producers, one pack command per map pack row (with its --property arguments)
    and the restore, then ONE verify-pack for the whole plan, then the consumer
    overlay, its verify-assets, restore, check and clean. List producers in dependency order.
    Nothing is printed when the plan is invalid (exit 2). Warns when a producer
    reaches the consumer through an unnamed hop.
  plan [--map FILE] CONSUMER PRODUCER...
    Read-only. Print the plan derived from scripts/dependency-map.tsv (or
    CRATIS_LOCAL_MAP) for repository names listed in dependency order: pack rows
    (producer, ecosystem), overlay rows (producer, upstream) and consume rows
    (the consumer's own edges). Exit 2, nothing printed: unknown or repeated name,
    no edge, wrong order, a planned pack without ids or pack rows.
  verify-pack STAMP --consumer CONSUMER --producer PRODUCER... [--json] [--map FILE]
    Check exactly the plan that "plan" prints for those names: every planned package
    exists in the feed at exactly STAMP (never another stamp), and each nuspec
    dependency on another planned NuGet ID is exactly STAMP, [STAMP] or [STAMP, )
    (an unreadable element fails). Run once,
    after the last pack. Exit 1 lists the findings ("missing: ..." / "dependency: ...").
    Exit 2: invalid plan, or a plan without pack rows (image-only). --json prints one
    line, {"stamp":..,"packages":[{"id":..,"version":..}]}, and sends findings to stderr.
  verify-pack STAMP NAME... [--ecosystem nuget|npm] [--json]
    Manual, weaker form: the IDs the map lists for each named repository (optionally
    one ecosystem) exist at STAMP and their planned NuGet dependencies point at STAMP.
  verify-assets CONSUMER-PATH STAMP [--npm] [--dir REL] [--json] --consumer C --producer P...
    Checks the same plan inside the consumer. Exit 1: a planned package missing from
    the feed; a planned ID resolved at another version ("ID VERSION PATH"); a producer
    the consumer uses in this ecosystem with none of its IDs resolved ("unconsumed:").
    NuGet reads obj/project.assets.json (none: exit 2); --npm reads installed copies
    under node_modules (under REL if given). --json: the consumer's resolved ids and versions.
  verify-assets CONSUMER-PATH STAMP [--npm] [--dir REL] [--json]
    Manual, weaker form without a plan: every package at STAMP in the feed that the
    consumer resolves must be STAMP (none resolved: exit 1).
  verify-pack --composition FILE [--json]
  verify-assets CONSUMER-PATH --composition FILE [--json]
  use-nuget CONSUMER --composition FILE [--task ID] [--pin-transitive] [--private-cache]
    Opt-in mixed-version NuGet closure: tab-separated producer NAME STAMP
    ABSOLUTE_WORKTREE rows, then package PRODUCER PACKAGE_ID rows. Case-insensitive
    IDs have one owner. All declared artifacts must exist; nothing is built/repacked.
    Uses Node, unzip and the selected SDK's native NuGet.Versioning APIs; unavailable
    validation fails closed (2). Bad artifacts/ranges/assets are findings (1).
    Composition JSON: {"packages":[{"id":..,"version":..,"producer":..}]} (no stamp).
    Activation publishes a journaled overlay with exclusive exact source mappings.
    The next mutation recovers interrupted work, or refuses and preserves unknown
    ownership/drift. Active compositions retain member artifacts. Restore before
    changing a composition or switching between it and a legacy overlay.
  list [--json] [--stamp STAMP]
    Read-only inventory of feed artifacts and stamp records, including legacy stamps.
    Shows producer, task state, live/total consumers and age. Requires node; npm
    package names are read from tarballs with tar. Takes no session lock.
  finish --task ID [--apply]
    Print records naming ID; --apply marks that task finished. No match is success.
    Finishing never restores consumers or removes packages. Requires node.
  pack-nuget PRODUCER STAMP [--task ID] [--property NAME=VALUE]... PROJECT-OR-SLN...
    Release-pack each target into the NuGet feed; requires dotnet. Each --property
    becomes one -p:NAME=VALUE argument of that command only (closure prints the
    properties of each map pack row); no environment variable carries them.
  pack-npm PRODUCER STAMP [--task ID]
    Requires node, npm and Yarn 4, and a completed repository build. Temporarily
    version non-private workspaces, pack each with npm pack --ignore-scripts (the
    publish workflows' tool, so the tarball matches the published one) as
    <scope>-<name>-<version>.tgz, and restore tracked manifests even on failure.
    This helper does not build or run lifecycle scripts.
  use-nuget CONSUMER STAMP [--task ID] [--pin-transitive] [--private-cache] [PACKAGE-ID...]
    Create a CPM overlay importing the repository's own Directory.Packages.props.
    Transitive pinning stays as the consumer sets it; --pin-transitive forces it on so
    stamped IDs the consumer does not pin still take effect (can raise NU1109 floors).
    Defaults to every stamped package ID in the feed. Prints properties and sourceable
    env. With source mapping, also prints the required restore --configfile.
    --private-cache (or CRATIS_LOCAL_PRIVATE_CACHE=1) exports NUGET_PACKAGES in env
    and registers a marked cache at <absolute-git-dir>/cratis-local-nuget.
    Non-CPM: manually use an untracked project copy with explicit PackageReference
    versions and dotnet restore --source FEED/nuget; do not edit shipped versions.
  use-npm CONSUMER STAMP [--task ID] [--dir REL]
    --dir REL names a Yarn project in a subdirectory (restore and clean follow it).
    Requires node and tar. Back up package.json, yarn.lock (including existing edits),
    and git diff; add file-tarball resolutions for every stamped feed package.
    Run yarn install afterward. Only one active npm overlay per session.
  restore CONSUMER
    Restore npm backups when only overlay resolutions and their local lockfile
    entries changed (including yarn install). Refuse unrelated changes and retain
    backups for review. Remove CPM/env overlays
    and run check. Keep the private NuGet cache. No git checkout is used on consumer files.
  check REPO [--staged]
    Scan added/changed tracked lines (staged-only if requested), plus all contents
    of untracked, non-ignored files. Reports path:line:pattern and file counts.
  clean [CONSUMER] [--stamp STAMP | --all | --expired [--online]] [--apply] [--dir REL] [--private-cache]
    Default: list all local feed/cache/image/session subjects, without deleting.
    --apply deletes listed subjects (docker rmi, never force). Refuses more than
    CRATIS_LOCAL_CLEAN_MAX subjects (default 500), or unresolved npm backups.
    Refuses selected stamps referenced by registered live consumer overlays/manifests;
    restore those consumers first. Dry-run uses the same protection.
    Known sessions are registered in FEED/sessions; an optional consumer also
    discovers its per-worktree session. Registered private caches are also scanned;
    missing/mismatched markers or unsafe cache paths refuse the entire clean (2).
    clean CONSUMER --private-cache lists the whole marked cache for removal; --apply
    removes it and its registration only after restore and with no live stamp.
    Removes selected FEED/stamps/STAMP records with the artifacts on --apply.
    --expired keeps unrecorded stamps, compositions and live references; expires
    finished producing tasks, gone consumers or stamps superseded by a release.
    Release evidence is offline NuGet caches unless --online opts into 10-second
    NuGet/npm checks. Kept stamps succeed (0). --apply also prunes gone consumers
    and missing sessions. Uses the same fuse, lock and dry-run default.
    Docker probes have a 10-second deadline.

Pack/use commands (including --composition) accept --task ID, overriding
CRATIS_LOCAL_TASK, else record null. Convention: owner/repo#number. IDs match
^[A-Za-z0-9][A-Za-z0-9._#:/-]{0,127}$ and are JSON values, never path components.
Registry mutations require node. Successful packs record producer HEAD and branch;
use records consumers after publication; restore retains their history.
Invalid records: pack/use and selected clean subjects refuse with 2. Restore reads only
session stamps after restoring overlays; finish skips invalid records with WARNINGs.
Restore/finish exit 1 for invalid records; list shows invalid rows and exits 1.
  --self-test
    Isolated scratch HOME tests; npm uses inherited PATH to discover node.
    Other tests use system PATH. Reports passed and skipped counts.

Configuration: source ${XDG_CONFIG_HOME:-$HOME/.config}/cratis/local-packages.env
if present (trusted shell code), then use these defaults for unset variables:
  CRATIS_LOCAL_FEED=${XDG_CACHE_HOME:-$HOME/.cache}/cratis/local-feed
  CRATIS_LOCAL_SESSION_DIR=<absolute-git-dir>/cratis-local (per checkout)
  NUGET_PACKAGES=$HOME/.nuget/packages
  CRATIS_LOCAL_PRIVATE_CACHE=0 (1 opts use-nuget into a private worktree cache)
  CRATIS_LOCAL_TASK=optional task ID (overridden by --task ID)
  CRATIS_LOCAL_CLEAN_MAX=500
  CRATIS_LOCAL_RUN=optional executable wrapper, invoked for each heavy step as:
    "$CRATIS_LOCAL_RUN" build LABEL -- COMMAND ARGUMENTS...
Global --dry-run prints mutations instead of executing them; read-only discovery
still runs. Use a unique stamp once per producer build. Never publish local builds.
HELP
}

die() { printf 'ERROR: %s\n' "$*" >&2; exit 2; }
failed() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }
need() { command -v "$1" >/dev/null 2>&1 || die "required executable not found: $1"; }
quote_command() { printf ' +'; printf ' %q' "$@"; printf '\n'; }
DRY=0
ARGS=()
for argument in "$@"; do
    case "$argument" in --dry-run) DRY=1 ;; *) ARGS+=("$argument") ;; esac
done
set -- ${ARGS[@]+"${ARGS[@]}"}
case " ${*:-} " in *' --help '*|*' -h '*) usage; exit 0 ;; esac
[ $# -gt 0 ] || { usage; exit 2; }
SELF=$(cd "$(dirname "$0")" && pwd)/$(basename "$0")
# Self-test dispatch precedes configuration so the real HOME/config cannot affect it.
MODE=$1
shift
if [ "$MODE" != --self-test ]; then
    CONFIG=${XDG_CONFIG_HOME:-$HOME/.config}/cratis/local-packages.env
    if [ -f "$CONFIG" ]; then
        # shellcheck source=/dev/null
        source "$CONFIG"
    fi
fi
FEED=${CRATIS_LOCAL_FEED:-${XDG_CACHE_HOME:-$HOME/.cache}/cratis/local-feed}
NUGET_CACHE=${NUGET_PACKAGES:-$HOME/.nuget/packages}
case "$FEED" in /*) ;; *) FEED=$PWD/$FEED ;; esac
# The configured spelling (for example /var/... on macOS, or a symlinked cache) is kept
# so leak scans catch consumer references written through either path.
FEED_ALIAS=$FEED
# Resolve configured/system symlink parents without creating a missing feed.
# Subsequent checks concern helper-owned components, not /tmp, /var or HOME.
resolve_feed() {
    local directory=$FEED suffix='' physical
    while [ ! -d "$directory" ]; do
        [ ! -e "$directory" ] && [ ! -L "$directory" ] || die "not a feed directory: $directory"
        suffix=/${directory##*/}$suffix
        directory=${directory%/*}; [ -n "$directory" ] || directory=/
    done
    physical=$(cd -P "$directory" && pwd -P) || die "cannot resolve feed: $FEED"
    if [ -n "$suffix" ]; then FEED=${physical%/}$suffix; else FEED=$physical; fi
}
[ "$MODE" = --self-test ] || resolve_feed
case "$NUGET_CACHE" in /*) ;; *) NUGET_CACHE=$PWD/$NUGET_CACHE ;; esac
TMP=''
PACK_REPO=''
PACK_MANIFESTS=()
SESSION_LOCK=''
SESSION_RECOVERY_GUARD=''
cleanup() {
    local status=$?
    trap - EXIT
    if [ ${#PACK_MANIFESTS[@]} -gt 0 ]; then
        if ! git -C "$PACK_REPO" checkout -- "${PACK_MANIFESTS[@]}"; then
            printf 'ERROR: producer manifest restoration failed: %s\n' "$PACK_REPO" >&2
            status=1
        fi
    fi
    if [ -n "$SESSION_LOCK" ] && [ ! -e "$SESSION_LOCK/journal.json" ] &&
        [ -f "$SESSION_LOCK/owner" ] && [ "$(cat "$SESSION_LOCK/owner")" = "$$" ]; then
        rm -- "$SESSION_LOCK/owner"
        rmdir -- "$SESSION_LOCK" || status=1
    fi
    if [ -n "$SESSION_RECOVERY_GUARD" ]; then
        rmdir -- "$SESSION_RECOVERY_GUARD" || status=1
    fi
    if [ -n "$TMP" ]; then rm -rf -- "$TMP"; fi
    exit "$status"
}
trap cleanup EXIT
trap 'exit 1' HUP INT TERM
scratch() { if [ -z "$TMP" ]; then TMP=$(mktemp -d "${TMPDIR:-/tmp}/cratis-local.XXXXXXXX"); fi; }
mutate() { if [ "$DRY" -eq 1 ]; then quote_command "$@"; else "$@"; fi; }
heavy() {
    local kind=$1 label=$2
    shift 2
    if [ -n "${CRATIS_LOCAL_RUN:-}" ]; then
        mutate "$CRATIS_LOCAL_RUN" "$kind" "$label" -- "$@"
    else
        mutate "$@"
    fi
}
repo_root() { git -C "$1" rev-parse --show-toplevel 2>/dev/null || die "not a Git repository: $1"; }
valid_stamp() {
    [[ "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+-(local\.[0-9]{12}|train)\.g[0-9a-f]{7}$ ]] || die "invalid local stamp: $1"
}
session_path() {
    local path git_dir
    git_dir=$(git -C "$1" rev-parse --absolute-git-dir)
    path=${CRATIS_LOCAL_SESSION_DIR:-$git_dir/cratis-local}
    case "$path" in /*) ;; *) path=$PWD/$path ;; esac
    [ "$path" != / ] && [ "$path" != "$1" ] && [ ! -L "$path" ] || die "unsafe session directory: $path"
    # Git's own directory is never tracked, but check-ignore does not classify it
    # as ignored. Other in-checkout overrides must still be explicitly ignored.
    case "$path/" in
        "$git_dir/"*) ;;
        "$1/"*) git -C "$1" check-ignore -q "$path/" || die "session override must be outside tracked files or ignored" ;;
    esac
    printf '%s\n' "$path"
}
validate_session() {
    local session=$1 entry
    [ ! -L "$session" ] || die "symlink session directory: $session"
    [ -d "$session" ] || return 0
    for entry in "$session"/* "$session"/.[!.]* "$session"/..?*; do
        [ -e "$entry" ] || [ -L "$entry" ] || continue
        [ ! -L "$entry" ] || die "symlink in session: $entry"
        case "${entry##*/}" in
            repo|stamp|env|npm-dir|nuget-cache|Directory.Packages.props|cratis-local.targets|nuget.config) [ -f "$entry" ] || die "unexpected session entry: $entry" ;;
            npm-backup|composition) [ -d "$entry" ] || die "unexpected session entry: $entry" ;;
            *) die "unrecognized file in session (preserved): $entry" ;;
        esac
    done
}
register_session() {
    local repo=$1 session=$2 stamp=$3
    validate_session "$session"
    if [ -f "$session/repo" ] && [ "$(cat "$session/repo")" != "$repo" ]; then die 'session belongs to another checkout'; fi
    mkdir -p "$session" "$FEED"
    printf '%s\n' "$repo" > "$session/repo"
    printf '%s\n' "$stamp" > "$session/stamp"
    if [ ! -f "$FEED/sessions" ] || ! grep -Fxq -- "$session" "$FEED/sessions"; then
        printf '%s\n' "$session" >> "$FEED/sessions"
    fi
}
# Validate every helper-owned path component before trusting a cache registration.
validate_private_cache() {
    local repo=$1 cache=$2 git_dir parent relative
    git_dir=$(git -C "$repo" rev-parse --absolute-git-dir) || die "cannot find cache repository: $repo"
    case "$cache" in "$git_dir/"*) relative=${cache#"$git_dir/"} ;; *) die "private cache outside repository git directory: $cache" ;; esac
    case "/$relative/" in */../*|*/./*|*//*) die "unsafe private cache path: $cache" ;; esac
    parent=$cache
    while [ "$parent" != "$git_dir" ]; do
        [ -d "$parent" ] && [ ! -L "$parent" ] || die "unsafe private cache directory: $parent"
        parent=${parent%/*}
    done
    [ -f "$cache/.cratis-local-cache" ] && [ ! -L "$cache/.cratis-local-cache" ] || die "private cache marker missing: $cache"
    [ "$(cat "$cache/.cratis-local-cache")" = "$repo" ] || die "private cache marker mismatch: $cache"
}
registered_private_cache() {
    local session=$1 repo cache
    validate_session "$session"
    [ -f "$session/repo" ] || die "private cache session has no repository: $session"
    repo=$(cat "$session/repo"); cache=$(cat "$session/nuget-cache")
    [ "$(repo_root "$repo")" = "$repo" ] && [ "$(session_path "$repo")" = "$session" ] || die "private cache session no longer matches repository: $session"
    validate_private_cache "$repo" "$cache"
    printf '%s\n' "$cache"
}
prepare_private_cache() {
    local repo=$1 cache=$2 session=$3
    validate_session "$session"
    if [ -e "$cache" ] || [ -L "$cache" ]; then validate_private_cache "$repo" "$cache"; fi
    if [ -f "$session/nuget-cache" ]; then
        [ "$(registered_private_cache "$session")" = "$cache" ] || die "different private cache already registered: $session"
    fi
}
register_private_cache() {
    local repo=$1 cache=$2 session=$3
    prepare_private_cache "$repo" "$cache" "$session"
    mkdir -p "$cache"
    printf '%s\n' "$repo" > "$cache/.cratis-local-cache"
    printf '%s\n' "$cache" > "$session/nuget-cache"
}
private_cache_restored() {
    local session=$1 entry
    for entry in stamp env Directory.Packages.props cratis-local.targets nuget.config npm-backup composition; do
        [ ! -e "$session/$entry" ] && [ ! -L "$session/$entry" ] || die "private cache still referenced by $session/$entry; restore first"
    done
}
xml_escape() { printf '%s' "$1" | sed -e 's/\&/\&amp;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g' -e 's/"/\&quot;/g' -e "s/'/\&apos;/g"; }

# Next patch of the highest reachable vX.Y.Z tag; sets NEXT_BASE.
next_base() {
    local tag major minor patch
    tag=$(git -C "$1" tag --merged HEAD | awk '/^v[0-9]+\.[0-9]+\.[0-9]+$/' | sort -t. -k1.2,1n -k2,2n -k3,3n | tail -n 1)
    if [ -n "$tag" ]; then
        IFS=. read -r major minor patch <<< "${tag#v}"
        NEXT_BASE="$major.$minor.$((10#$patch + 1))"
    else
        printf 'WARNING: no reachable vX.Y.Z tag in %s; using 0.1.0\n' "$1" >&2
        NEXT_BASE=0.1.0
    fi
}
# True when version $1 sorts above $2 (major.minor.patch).
base_above() {
    local top
    top=$(printf '%s\n%s\n' "$1" "$2" | sort -t. -k1,1n -k2,2n -k3,3n | tail -n 1)
    [ "$top" = "$1" ] && [ "$1" != "$2" ]
}

stamp() {
    [ $# -ge 1 ] || die 'stamp requires a producer repository'
    local repo base='' override='' sha='' arg train=0 lines='' rname origin
    local producers=()
    if [ "$1" = --train ]; then train=1; shift; [ $# -ge 1 ] || die 'stamp --train requires a producer repository'; fi
    while [ $# -gt 0 ]; do
        arg=$1; shift
        case "$arg" in
            --base-version)
                [ $# -gt 0 ] || die '--base-version requires X.Y.Z'
                [ -z "$override" ] || die '--base-version may only be supplied once'
                [[ "$1" =~ ^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$ ]] || die "invalid base version: $1 (expected SemVer core X.Y.Z)"
                override=$1; shift ;;
            --train) train=1 ;;
            -*) die "unknown stamp option: $arg" ;;
            *) producers+=("$arg") ;;
        esac
    done
    [ ${#producers[@]} -gt 0 ] || die 'stamp requires a producer repository'
    set -- "${producers[@]}"
    base=$override
    for arg in "$@"; do
        repo=$(repo_root "$arg")
        if [ -z "$override" ]; then
            next_base "$repo"
            if [ -z "$base" ] || base_above "$NEXT_BASE" "$base"; then base=$NEXT_BASE; fi
        fi
        sha=$(git -C "$repo" rev-parse HEAD) || die 'producer has no HEAD commit'
        if [ $# -gt 1 ]; then printf 'HEAD %s %s\n' "$repo" "$sha" >&2; fi
        # The repository name is its origin's last path part (stable across checkouts), else its folder name.
        origin=$(git -C "$repo" remote get-url origin 2>/dev/null || true)
        rname=$(basename "${origin:-$repo}"); rname=${rname%.git}
        lines="$lines$rname $sha"$'\n'
    done
    # With several producers the highest base wins, so no stamped package sorts
    # below a released pin of another stamped package (NU1605).
    # A train stamp has no minute and its sha7 covers every producer: the first 7 hex characters of the
    # SHA-1 of the sorted "<repo> <full HEAD sha>" lines. One combined code state always yields the same
    # stamp, and a change in any producer changes it. The grammar stays X.Y.Z-train.g<sha7>.
    if [ "$train" -eq 1 ]; then
        sha=$(printf '%s' "$lines" | LC_ALL=C sort -u | shasum -a 1) || die 'cannot hash the producer HEADs'
        printf '%s-train.g%.7s\n' "$base" "${sha%% *}"; return 0
    fi
    printf '%s-local.%s.g%.7s\n' "$base" "$(date -u +%Y%m%d%H%M)" "$sha"
}

# All stamp-record mutations use this path, after builds or overlay publication.
registry_update() {
    [ "$DRY" -eq 0 ] || return 0
    [ -n "$SESSION_LOCK" ] || acquire_session_lock
    registry "$@"
}
registry_consumer() {
    if ! (registry_update use "$@"); then
        printf 'WARNING: stamp registry update failed; overlay kept; restore before cleaning\n' >&2
        exit 1
    fi
}
registry() {
    need node
    case "$1" in list|expiry) expiry_context "$@" ;; esac
    node - "$FEED" "$TASK" "$TMP" "$@" <<'REGISTRY'
const fs = require('node:fs'), path = require('node:path'), cp = require('node:child_process');
const [feed, taskValue, scratch, operation, ...args] = process.argv.slice(2);
const task = taskValue || null, root = path.join(feed, 'stamps');
const stampPattern = /^([0-9]+\.[0-9]+\.[0-9]+)-(local\.[0-9]{12}|train)\.g[0-9a-f]{7}$/;
const taskPattern = /^[A-Za-z0-9][A-Za-z0-9._#:/-]{0,127}$/;
const now = new Date().toISOString();
function fail(message) { throw new Error(message); }
function safe(file, directory = false) {
    const relative = path.relative(feed, file);
    if (relative.startsWith('..') || path.isAbsolute(relative)) fail('outside stamp registry: ' + file);
    let current = feed;
    for (const part of ['', ...relative.split(path.sep).filter(Boolean)]) {
        current = path.join(current, part);
        const stat = fs.lstatSync(current, {throwIfNoEntry:false});
        if (stat && (stat.isSymbolicLink() || (current !== file || directory ? !stat.isDirectory() : !stat.isFile()))) fail('unsafe registry path: ' + current);
    }
}
function recordPath(stamp) {
    if (!stampPattern.test(stamp)) fail('invalid registry stamp: ' + stamp);
    const file = path.join(root, stamp, 'record.json'); safe(file); return file;
}
function read(stamp) {
    const file = recordPath(stamp);
    if (!fs.existsSync(file)) return null;
    const value = JSON.parse(fs.readFileSync(file, 'utf8'));
    const match = stamp.match(stampPattern);
    if (value.schema !== 1 || value.stamp !== stamp || value.base !== match[1] || value.kind !== (match[2] === 'train' ? 'train' : 'local') ||
        !Array.isArray(value.packs) || !Array.isArray(value.consumers) || !value.tasks || typeof value.tasks !== 'object' || Array.isArray(value.tasks) ||
        !Number.isFinite(Date.parse(value.created)) || !Number.isFinite(Date.parse(value.updated))) fail('invalid stamp record: ' + file);
    for (const pack of value.packs) {
        if (!['nuget','npm'].includes(pack.ecosystem) || !Array.isArray(pack.ids) || pack.ids.some(id => typeof id !== 'string') ||
            !pack.producer || ['repo','worktree','branch','head'].some(k => typeof pack.producer[k] !== 'string') ||
            !(pack.task === null || typeof pack.task === 'string' && taskPattern.test(pack.task)) || !Number.isFinite(Date.parse(pack.time))) fail('invalid pack record: ' + file);
    }
    for (const consumer of value.consumers) {
        if (['worktree','session','ecosystem','time'].some(k => typeof consumer[k] !== 'string') || !['nuget','npm'].includes(consumer.ecosystem) ||
            !(consumer.task === null || typeof consumer.task === 'string' && taskPattern.test(consumer.task)) ||
            !Number.isFinite(Date.parse(consumer.time)) || consumer.restored && !Number.isFinite(Date.parse(consumer.restored))) fail('invalid consumer record: ' + file);
    }
    for (const [id, state] of Object.entries(value.tasks)) {
        if (!taskPattern.test(id) || !state || !(state.finished === null || typeof state.finished === 'string' && Number.isFinite(Date.parse(state.finished)))) fail('invalid task record: ' + file);
    }
    return value;
}
function warning(stamp, error) {
    console.error('WARNING: stamp registry: ' + path.join(root, stamp, 'record.json') + ': ' + error.message);
    process.exitCode = 1;
}
function sessionStamps(session) {
    const files = ['stamp','Directory.Packages.props','cratis-local.targets','nuget.config','env','npm-backup/expected-package.json','composition/manifest.tsv'];
    const found = new Set();
    for (const name of files) {
        const file = path.join(session, name);
        if (!fs.existsSync(file)) continue;
        for (const stamp of fs.readFileSync(file, 'utf8').match(/[0-9]+\.[0-9]+\.[0-9]+-(?:local\.[0-9]{12}|train)\.g[0-9a-f]{7}/g) || []) found.add(stamp);
    }
    for (const stamp of found) console.log(stamp);
}
function stamps() {
    safe(root, true);
    if (!fs.existsSync(root)) return [];
    return fs.readdirSync(root).filter(s => stampPattern.test(s)).sort();
}
function write(value) {
    const file = recordPath(value.stamp), next = file + '.next'; safe(next);
    fs.mkdirSync(path.dirname(file), {recursive:true, mode:0o700});
    value.updated = now;
    fs.writeFileSync(next, JSON.stringify(value, null, 2) + '\n', {mode:0o600});
    fs.chmodSync(next, 0o600); fs.renameSync(next, file);
}
function consumerState(consumer) {
    if (consumer.restored) return 'restored';
    if (!fs.existsSync(consumer.worktree)) return 'gone';
    const git = cp.spawnSync('git', ['-C', consumer.worktree, 'rev-parse', '--show-toplevel'], {encoding:'utf8'});
    if (git.status !== 0 || git.stdout.trim() !== consumer.worktree) return 'gone';
    return ['env','Directory.Packages.props','cratis-local.targets','nuget.config','npm-backup','composition'].some(n => fs.existsSync(path.join(consumer.session,n))) ? 'live' : 'restored';
}
function packages() {
    const found = new Map();
    for (const ecosystem of ['nuget','npm']) {
        const directory = path.join(feed,ecosystem); safe(directory,true);
        if (!fs.existsSync(directory)) continue;
        for (const name of fs.readdirSync(directory)) {
            const match = name.match(/^(.*?)[.-]([0-9]+\.[0-9]+\.[0-9]+-(?:local\.[0-9]{12}|train)\.g[0-9a-f]{7})\.(nupkg|tgz)$/);
            if (!match || (ecosystem === 'nuget' ? match[3] !== 'nupkg' : match[3] !== 'tgz')) continue;
            const file = path.join(directory,name); safe(file);
            let id = match[1];
            if (ecosystem === 'npm') id = JSON.parse(cp.execFileSync('tar',['-xOf',file,'package/package.json'],{encoding:'utf8'})).name;
            if (typeof id !== 'string' || !id) fail('invalid package ID: ' + file);
            const entries = found.get(match[2]) || [];
            entries.push({id,version:match[2],ecosystem,file}); found.set(match[2],entries);
        }
    }
    return found;
}
function worktreeGone(consumer) {
    if (!fs.statSync(consumer.worktree,{throwIfNoEntry:false})?.isDirectory()) return true;
    const git = cp.spawnSync('git',['-C',consumer.worktree,'rev-parse','--show-toplevel'],{encoding:'utf8'});
    return git.status !== 0 || git.stdout.trim() !== consumer.worktree;
}
function gone(consumer) {
    return !fs.statSync(consumer.session,{throwIfNoEntry:false})?.isDirectory() || worktreeGone(consumer);
}
function expiryInputs() {
    const selectedIndex = args.indexOf('--stamp'), selected = selectedIndex < 0 ? null : args[selectedIndex + 1];
    const sessions = path.join(feed,'sessions'); safe(sessions);
    if (fs.existsSync(sessions)) {
        for (const session of fs.readFileSync(sessions,'utf8').split('\n').filter(Boolean)) {
            const repo = path.join(session,'repo');
            if (fs.existsSync(repo)) console.log(fs.readFileSync(repo,'utf8').trim() + '\t' + session);
            else console.log('\t' + session);
        }
    }
    for (const stamp of selected ? [selected] : stamps()) {
        let value;
        try { value = read(stamp); } catch (error) { if (args[0] === 'list') continue; throw error; }
        for (const c of value?.consumers || []) console.log(c.worktree + '\t' + c.session);
    }
}
function contextLines(name) { return fs.readFileSync(path.join(scratch,'expiry-' + name),'utf8').split('\n').filter(Boolean); }
function atLeast(version, base) {
    if (!/^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/.test(version)) return false;
    const a = version.split('.').map(BigInt), b = base.split('.').map(BigInt);
    for (let i = 0; i < 3; i++) if (a[i] !== b[i]) return a[i] > b[i];
    return true;
}
function offlineRelease(value) {
    for (const pack of value.packs.filter(p => p.ecosystem === 'nuget')) for (const id of pack.ids) {
        if (!/^[A-Za-z0-9_][A-Za-z0-9_.-]*$/.test(id)) fail('invalid NuGet release ID: ' + id);
        for (const cache of contextLines('caches')) {
            const directory = path.join(cache,id.toLowerCase());
            // A missing cache is no evidence; actual scan/read errors fail closed.
            if (!fs.statSync(directory,{throwIfNoEntry:false})) continue;
            for (const version of fs.readdirSync(directory).sort()) {
                if (atLeast(version,value.base) && fs.statSync(path.join(directory,version)).isDirectory() &&
                    fs.statSync(path.join(directory,version,'.nupkg.metadata'),{throwIfNoEntry:false})?.isFile()) return 'released:' + id + '@' + version;
            }
        }
    }
    return null;
}
async function onlineRelease(value) {
    for (const pack of value.packs) for (const id of pack.ids) {
        // Test-only override: hermetic unreachable-host fixtures, never a feed setting.
        const override = process.env.CRATIS_LOCAL_RELEASE_URL_BASE;
        const url = pack.ecosystem === 'nuget' ? (override || 'https://api.nuget.org/v3-flatcontainer') + '/' + encodeURIComponent(id.toLowerCase()) + '/index.json' :
            (override || 'https://registry.npmjs.org') + '/' + encodeURIComponent(id);
        try {
            const response = await fetch(url,{signal:AbortSignal.timeout(10000)});
            if (!response.ok) continue;
            const data = await response.json(), versions = pack.ecosystem === 'nuget' ? data.versions : Object.keys(data.versions || {});
            if (!Array.isArray(versions)) continue;
            const version = versions.find(v => typeof v === 'string' && atLeast(v,value.base));
            if (version) return 'released:' + id + '@' + version;
        } catch { /* Network failures are not release evidence and never fail cleanup. */ }
    }
    return null;
}
async function expiry(stamp, value, online = false) {
    const kept = reason => ({expired:false,reasons:[],kept:[reason]});
    if (!value) return kept('unrecorded; use clean --stamp ' + stamp);
    if (contextLines('compositions').includes(stamp)) return kept('active composition member');
    const reference = contextLines('live').find(line => line.split('\t')[0] === stamp);
    if (reference) return kept('live reference in ' + reference.slice(reference.indexOf('\t') + 1));
    const tasks = value.packs.map(p => p.task).filter(t => t !== null);
    let reason = tasks.length && tasks.every(t => value.tasks[t]?.finished) ? 'task-finished' : null;
    if (!reason && value.consumers.length && value.consumers.every(gone)) reason = 'consumers-gone';
    if (!reason) reason = offlineRelease(value);
    if (!reason && online) reason = await onlineRelease(value);
    return reason ? {expired:true,reasons:[reason],kept:[]} : kept('active');
}
async function expired() {
    const names = [...new Set([...packages().keys(),...stamps()])].sort();
    // Validate every subject before reporting decisions or allowing any deletion.
    const records = names.map(stamp => [stamp,read(stamp)]);
    for (const [stamp,value] of records) {
        const decision = await expiry(stamp,value,args.includes('--online'));
        console.log([decision.expired ? 'expired' : 'kept',stamp,(decision.expired ? decision.reasons : decision.kept).join('; ')].join('\t'));
    }
}
function prune() {
    const values = stamps().map(read), sessions = path.join(feed,'sessions'); safe(sessions);
    const before = fs.existsSync(sessions) ? fs.readFileSync(sessions,'utf8') : null;
    for (const value of values.filter(Boolean)) {
        // Keep a consumer while its worktree exists: its manifests are still scanned for live references.
        const consumers = value.consumers.filter(c => !worktreeGone(c));
        if (consumers.length !== value.consumers.length) { value.consumers = consumers; write(value); }
    }
    if (before !== null) {
        const after = before.split('\n').filter(s => s && fs.statSync(s,{throwIfNoEntry:false})?.isDirectory()).join('\n');
        const next = sessions + '.next'; safe(next);
        fs.writeFileSync(next,after ? after + '\n' : '',{mode:0o600}); fs.renameSync(next,sessions);
    }
}
async function list() {
    const json = args.includes('--json'), index = args.indexOf('--stamp');
    const selected = index < 0 ? null : args[index + 1];
    const remaining = args.filter((_,i) => i !== index && i !== index + 1 || index < 0);
    if (remaining.some(a => a !== '--json') || index >= 0 && !stampPattern.test(selected || '')) fail('list accepts --json and --stamp STAMP');
    const artifacts = packages(), names = [...new Set([...artifacts.keys(), ...stamps()])].sort();
    const rows = [];
    for (const stamp of names.filter(s => !selected || s === selected)) {
        let value = null, error;
        try { value = read(stamp); } catch (failure) { error = failure.message; warning(stamp, failure); }
        const match = stamp.match(stampPattern), created = value?.created || null;
        rows.push({stamp,base:match[1],kind:match[2] === 'train' ? 'train' : 'local',recorded:error ? 'invalid' : !!value,...(error ? {error} : {}),packages:artifacts.get(stamp) || [],
            producers:value?.packs.map(p => p.producer) || [],tasks:Object.entries(value?.tasks || {}).map(([id,s]) => ({id,finished:s.finished})),
            consumers:value?.consumers.map(c => ({worktree:c.worktree,task:c.task,state:consumerState(c)})) || [],created,
            ageSeconds:created ? Math.max(0,Math.floor((Date.now()-Date.parse(created))/1000)) : null,
            expiry:error ? {expired:false,reasons:[],kept:['invalid record']} : await expiry(stamp,value)});
    }
    if (json) console.log(JSON.stringify({schema:1,feed,stamps:rows}));
    else {
        console.log('STAMP\tECOSYS\tPACKAGES(n)\tOWNER(repo@branch)\tTASK(state)\tCONSUMERS(live/total)\tAGE\tRECORDED');
        for (const r of rows) console.log([r.stamp,[...new Set(r.packages.map(p => p.ecosystem))].join(',') || '-',r.packages.length,
            r.producers.map(p => p.repo+'@'+p.branch).join(',') || '-',r.tasks.map(t => t.id+'('+ (t.finished ? 'finished' : 'active') +')').join(',') || '-',
            r.consumers.filter(c => c.state === 'live').length+'/'+r.consumers.length,r.ageSeconds === null ? '-' : r.ageSeconds+'s',r.recorded].join('\t'));
    }
}
function update() {
    const [stamp, ecosystem, repo, session, cache, ...ids] = args;
    let value = read(stamp);
    if (!value) {
        const match = stamp.match(stampPattern);
        value = {schema:1,stamp,base:match[1],kind:match[2] === 'train' ? 'train' : 'local',created:now,updated:now,packs:[],consumers:[],tasks:{}};
    }
    if (task && !Object.hasOwn(value.tasks,task)) Object.defineProperty(value.tasks,task,{value:{finished:null},enumerable:true,writable:true,configurable:true});
    if (operation === 'pack') {
        const git = (...argv) => cp.execFileSync('git',['-C',repo,...argv],{encoding:'utf8'}).trim();
        const producer = {repo:path.basename(repo),worktree:repo,branch:git('branch','--show-current'),head:git('rev-parse','HEAD')};
        const entry = {ecosystem,ids,producer,task,time:now};
        const index = value.packs.findIndex(p => p.ecosystem === ecosystem && p.producer.worktree === repo && p.task === task);
        if (index < 0) value.packs.push(entry); else value.packs[index] = {...entry,ids:[...new Set([...value.packs[index].ids,...ids])]};
    } else {
        const entry = {worktree:repo,session,task,ecosystem,time:now,...(cache ? {cache} : {})};
        const index = value.consumers.findIndex(c => c.worktree === repo && c.session === session && c.ecosystem === ecosystem && c.task === task);
        if (index < 0) value.consumers.push(entry); else value.consumers[index] = entry;
    }
    write(value);
}
async function main() {
    if (operation === 'list') await list();
    else if (operation === 'expiry-inputs') expiryInputs();
    else if (operation === 'expiry') await expired();
    else if (operation === 'prune') prune();
    else if (operation === 'pack' || operation === 'use') update();
    else if (operation === 'restore-stamps') sessionStamps(args[0]);
    else if (operation === 'remove') {
        const file = recordPath(args[0]); read(args[0]);
        if (fs.readdirSync(path.dirname(file)).some(n => n !== 'record.json')) fail('unexpected stamp directory entry');
        fs.unlinkSync(file); fs.rmdirSync(path.dirname(file));
    } else {
        const selected = operation === 'restore' ? [...new Set(args.slice(1))] :
            operation === 'clean' && args[0] && args[0] !== 'all' ? [args[0]] : stamps();
        let count = 0;
        for (const stamp of selected) {
            let value;
            try { value = read(stamp); } catch (error) {
                if (operation !== 'finish' && operation !== 'restore') throw error;
                warning(stamp, error); continue;
            }
            if (!value) continue;
            if (operation === 'finish' && Object.hasOwn(value.tasks,task)) {
                console.log((args.includes('--apply') ? 'finish: ' : 'would finish: ') + stamp + ' ' + task); count++;
                if (args.includes('--apply')) { value.tasks[task].finished = now; write(value); }
            } else if (operation === 'restore' && value.consumers.some(c => c.session === args[0] && !c.restored)) {
                for (const c of value.consumers) if (c.session === args[0] && !c.restored) c.restored = now;
                write(value);
            } else if (operation === 'clean' && (!args[0] || args[0] === 'all' || stamp === args[0])) {
                const directory = path.dirname(recordPath(stamp));
                if (fs.readdirSync(directory).some(n => n !== 'record.json')) fail('unexpected stamp directory entry: ' + directory);
                console.log(directory);
            }
        }
        if (operation === 'finish' && !count) console.log('no records name task ' + task);
    }
}
main().catch(error => { console.error('ERROR: stamp registry: ' + error.message); process.exitCode = 2; });
REGISTRY
}

# Compare file identities/times so a multi-producer stamp does not attribute earlier
# producers' packages to the current pack. Repacking the same ID is still recorded.
nuget_pack_files() {
    node - "$FEED/nuget" "$@" <<'NODE'
const fs = require('node:fs'), path = require('node:path');
const [directory, stamp, before] = process.argv.slice(2);
const files = {};
for (const name of fs.existsSync(directory) ? fs.readdirSync(directory) : []) {
    if (!name.endsWith('.' + stamp + '.nupkg')) continue;
    const file = path.join(directory,name), st = fs.statSync(file, {bigint:true});
    files[file] = [st.ino,st.size,st.mtimeNs,st.ctimeNs].map(String).join(':');
}
if (!before) console.log(JSON.stringify(files));
else {
    const previous = JSON.parse(fs.readFileSync(before,'utf8'));
    for (const [file,identity] of Object.entries(files)) if (previous[file] !== identity) process.stdout.write(file + '\0');
}
NODE
}

pack_nuget() {
    [ $# -ge 3 ] || die 'pack-nuget requires producer, stamp and targets'
    local repo version target path count=0 property ids=()
    local properties=()
    repo=$(repo_root "$1"); version=$2; shift 2
    valid_stamp "$version"; need dotnet
    # Extra MSBuild properties (from the dependency map) go on the command line, never the environment:
    # an environment variable also reaches nested builds such as Chronicle's `dotnet run` code generator.
    while [ "${1:-}" = --property ]; do
        [ $# -ge 2 ] || die '--property requires NAME=VALUE'
        property=$2; shift 2
        [[ "$property" =~ ^[A-Za-z_][A-Za-z0-9_]*=[A-Za-z0-9_.,-]*$ ]] || die "invalid pack property: $property"
        properties+=("-p:$property")
    done
    [ $# -ge 1 ] || die 'pack-nuget requires at least one target after the properties'
    if [ "$DRY" -eq 0 ]; then
        need node; scratch
        nuget_pack_files "$version" > "$TMP/pack-before"
    fi
    mutate mkdir -p "$FEED/nuget"
    for target in "$@"; do
        (cd "$repo" && heavy build "pack NuGet $target" dotnet pack "$target" -c Release "-p:PackageVersion=$version" "-p:Version=$version" ${properties[@]+"${properties[@]}"} -o "$FEED/nuget")
    done
    [ "$DRY" -eq 0 ] || return 0
    nuget_pack_files "$version" "$TMP/pack-before" > "$TMP/pack-produced"
    while IFS= read -r -d '' path; do
        printf '%s\n' "$path"; count=$((count + 1))
        target=${path##*/}; ids+=("${target%."$version".nupkg}")
    done < "$TMP/pack-produced"
    [ "$count" -gt 0 ] || failed 'pack produced no matching nupkgs'
    printf 'produced %s nupkgs\n' "$count"
    registry_update pack "$version" nuget "$repo" '' '' "${ids[@]}"
}

# Pack one workspace with npm pack (what publish does) into the feed as <scope>-<name>-<version>.tgz.
pack_npm_workspace() {
    local repo=$1 manifest=$2 version=$3 dir name slug
    dir=$repo/$(dirname "$manifest")
    name=$(node -p "require(process.argv[1]).name" "$dir/package.json") || failed "cannot read package name: $manifest"
    slug=${name//\//-}
    if [ "$DRY" -eq 1 ]; then
        quote_command npm pack --ignore-scripts --pack-destination "$FEED/npm" "(in $dir)"
        return 0
    fi
    # Packing is a file copy, not a build: no queue slot per workspace.
    (cd "$dir" && mutate npm pack --ignore-scripts --pack-destination "$FEED/npm") >/dev/null
    [ "$FEED/npm/${slug#@}-$version.tgz" = "$FEED/npm/$slug-$version.tgz" ] || \
        mv -f "$FEED/npm/${slug#@}-$version.tgz" "$FEED/npm/$slug-$version.tgz" || failed "npm pack did not produce $name"
}

pack_npm() {
    [ $# -eq 2 ] || die 'pack-npm requires producer and stamp'
    local repo version path count=0 ids=()
    repo=$(repo_root "$1"); version=$2
    valid_stamp "$version"; need node; need npm; need yarn; scratch
    git -C "$repo" diff --name-only HEAD -- 'package.json' '*/package.json' > "$TMP/modified"
    git -C "$repo" diff --cached --name-only -- 'package.json' '*/package.json' >> "$TMP/modified"
    git -C "$repo" diff --name-only -- 'package.json' '*/package.json' >> "$TMP/modified"
    [ ! -s "$TMP/modified" ] || die 'tracked package.json is already modified; preserve it before packing'
    (cd "$repo" && yarn workspaces list --json) > "$TMP/workspaces"
    node - "$repo" "$TMP/workspaces" > "$TMP/manifests" <<'NODE'
const fs = require('fs'), path = require('path');
const [repo, list] = process.argv.slice(2);
for (const line of fs.readFileSync(list, 'utf8').trim().split('\n')) {
    const workspace = JSON.parse(line);
    const file = path.join(workspace.location, 'package.json');
    if (!JSON.parse(fs.readFileSync(path.join(repo, file), 'utf8')).private)
        process.stdout.write(file + '\0');
}
NODE
    PACK_REPO=$repo
    while IFS= read -r -d '' path; do
        git -C "$repo" ls-files --error-unmatch -- "$path" >/dev/null || die "untracked workspace manifest: $path"
        PACK_MANIFESTS+=("$path")
    done < "$TMP/manifests"
    [ ${#PACK_MANIFESTS[@]} -gt 0 ] || die 'no non-private workspaces found'
    if [ "$DRY" -eq 1 ]; then
        quote_command node '<set non-private workspace versions>' "$version" "${PACK_MANIFESTS[@]}"
        PACK_MANIFESTS=()
    else
        node - "$repo" "$version" "${PACK_MANIFESTS[@]}" <<'NODE'
const fs = require('fs'), path = require('path');
const [repo, version, ...files] = process.argv.slice(2);
for (const file of files) {
    const target = path.join(repo, file), json = JSON.parse(fs.readFileSync(target, 'utf8'));
    json.version = version;
    fs.writeFileSync(target, JSON.stringify(json, null, 2) + '\n');
}
NODE
    fi
    mutate mkdir -p "$FEED/npm"
    # npm pack, like the publish workflows' npm publish: yarn pack drops files of a gitignored dist folder.
    while IFS= read -r -d '' path; do
        pack_npm_workspace "$repo" "$path" "$version"
    done < "$TMP/manifests"
    if [ "$DRY" -eq 1 ]; then quote_command git -C "$repo" checkout -- '<versioned manifests>'; return 0; fi
    git -C "$repo" checkout -- "${PACK_MANIFESTS[@]}"
    PACK_MANIFESTS=()
    while IFS= read -r -d '' path; do
        printf '%s\n' "$path"; count=$((count + 1))
    done < <(find "$FEED/npm" -type f -name "*-$version.tgz" -print0)
    [ "$count" -gt 0 ] || failed 'pack produced no matching npm tarballs'
    printf 'produced %s npm tarballs\n' "$count"
    while IFS= read -r -d '' path; do
        ids+=("$(node -p 'require(process.argv[1]).name' "$repo/$path")")
    done < "$TMP/manifests"
    registry_update pack "$version" npm "$repo" '' '' "${ids[@]}"
}

source_mapping() {
    local config=$1 output=$2 id
    shift 2
    scratch
    printf '%s\n' "$@" > "$TMP/ids"
    # Preserve all existing configuration, but remove exact mappings for overridden
    # IDs: NuGet must not select both the remote source and our local exact match.
    awk -v ids="$TMP/ids" -v feed="$(xml_escape "$FEED/nuget")" -v base="$(xml_escape "$(dirname "$config")")" '
        BEGIN { while ((getline id < ids)>0) { selected[tolower(id)]=1; ordered[++count]=id }; RS="<"; ORS="" }
        NR==1 { print; next }
        /^package[[:space:]]/ {
            value=$0; gsub(sprintf("%c",39),"\"",value)
            sub(/^.*pattern[[:space:]]*=[[:space:]]*"/,"",value); sub(/".*$/,"",value)
            if (tolower(value) in selected) next
        }
        /^packageSources[[:space:]]*>/ { sources=1 }
        /^\/packageSources[[:space:]]*>/ { sources=0 }
        sources && /^add[[:space:]]/ {
            # Relative source paths must retain their meaning after relocation.
            value=$0; gsub(sprintf("%c",39),"\"",value)
            if (match(value,/value[[:space:]]*=[[:space:]]*"[^"]*"/)) {
                attribute=substr(value,RSTART,RLENGTH); sub(/^[^"]*"/,"",attribute); sub(/"$/,"",attribute)
                if (attribute !~ /^([A-Za-z][A-Za-z0-9+.-]*:|\/|\\\\|%)/) {
                    sub(/value[[:space:]]*=[[:space:]]*"[^"]*"/,"value=\"" base "/" attribute "\"",value)
                    $0=value
                }
            }
        }
        /^\/packageSources[[:space:]]*>/ { print "<add key=\"cratis-local\" value=\"" feed "\" />\n" }
        /^\/packageSourceMapping[[:space:]]*>/ {
            print "<packageSource key=\"cratis-local\">\n"
            for (i=1;i<=count;i++) print "<package pattern=\"" ordered[i] "\" />\n"
            print "</packageSource>\n"
        }
        { print "<" $0 }
    ' "$config" > "$output"
}

# These headers are shared by the legacy and composition writers. The session
# lives below the Git directory, never above a consumer project in the lookup.
nuget_props_start() {
    cat <<'XML'
<Project>
  <PropertyGroup><_CratisLocalPackagesProps>$([MSBuild]::GetPathOfFileAbove('Directory.Packages.props', '$(MSBuildProjectDirectory)'))</_CratisLocalPackagesProps></PropertyGroup>
  <Import Project="$(_CratisLocalPackagesProps)" Condition="'$(_CratisLocalPackagesProps)' != '' and Exists('$(_CratisLocalPackagesProps)')" />
XML
    if [ "$1" -eq 1 ]; then
        printf '  <PropertyGroup Condition="%s"><CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled></PropertyGroup>\n' "'\$(ManagePackageVersionsCentrally)' == 'true'"
    fi
    printf '  <ItemGroup Condition="%s">\n' "'\$(ManagePackageVersionsCentrally)' == 'true'"
}
nuget_targets_start() {
    cat <<'XML'
<Project>
  <Import Project="$(_CratisLocalPreviousAfterMicrosoftCommonTargets)" Condition="'$(_CratisLocalPreviousAfterMicrosoftCommonTargets)' != '' and '$(_CratisLocalPreviousAfterMicrosoftCommonTargets)' != '$(MSBuildThisFileFullPath)' and Exists('$(_CratisLocalPreviousAfterMicrosoftCommonTargets)')" />
  <ItemGroup Condition="'$(ManagePackageVersionsCentrally)' != 'true'">
XML
}
nuget_overlay_end() { printf '  </ItemGroup>\n</Project>\n'; }
nuget_env() {
    local session=$1 targets_key=CustomAfterMicrosoftCommon"Targets"
    printf 'export %s=%q\n' DirectoryPackages"PropsPath" "$session/Directory.Packages.props"
    printf 'export %s=%q\n' RestoreAdditional"ProjectSources" "$FEED/nuget"
    # Capture at source time, not activation time; sourcing twice must not recurse.
    printf 'if [ "%s" != %q ]; then\n' "\${$targets_key:-}" "$session/cratis-local.targets"
    printf '  export _CratisLocalPreviousAfterMicrosoftCommonTargets="%s"\nfi\n' "\${$targets_key:-}"
    printf 'export %s=%q\n' "$targets_key" "$session/cratis-local.targets"
    if [ -n "${PRIVATE_NUGET_CACHE:-}" ]; then printf 'export NUGET_PACKAGES=%q\n' "$PRIVATE_NUGET_CACHE"; fi
}

use_nuget() {
    local private=${CRATIS_LOCAL_PRIVATE_CACHE:-0} PRIVATE_NUGET_CACHE='' option filtered=() private_repo private_session
    for option in "$@"; do
        if [ "$option" = --private-cache ]; then private=1; else filtered+=("$option"); fi
    done
    set -- ${filtered[@]+"${filtered[@]}"}
    if [ "$private" = 1 ]; then
        [ $# -ge 2 ] || die 'use-nuget requires consumer and stamp or composition'
        private_repo=$(repo_root "$1"); private_session=$(session_path "$private_repo")
        PRIVATE_NUGET_CACHE=$(git -C "$private_repo" rev-parse --absolute-git-dir)/cratis-local-nuget
        prepare_private_cache "$private_repo" "$PRIVATE_NUGET_CACHE" "$private_session"
    fi
    if [ "${2:-}" = --composition ]; then
        composition use-nuget "$@"
        if [ "$private" = 1 ] && [ "$DRY" -eq 0 ]; then register_private_cache "$private_repo" "$PRIVATE_NUGET_CACHE" "$private_session"; fi
        if [ "$DRY" -eq 0 ]; then
            local composition_repo composition_session composition_stamp
            composition_repo=$(repo_root "$1"); composition_session=$(session_path "$composition_repo")
            scratch
            cut -f3 "$composition_session/composition/manifest.tsv" | sort -u > "$TMP/registry-stamps"
            while IFS= read -r composition_stamp; do
                registry_consumer "$composition_stamp" nuget "$composition_repo" "$composition_session" "$PRIVATE_NUGET_CACHE"
            done < "$TMP/registry-stamps"
        fi
        return
    fi
    case " $* " in *' --composition '*) die 'cannot mix a stamp or IDs with --composition' ;; esac
    [ $# -ge 2 ] || die 'use-nuget requires consumer and stamp'
    [ ! -d "$(session_path "$(repo_root "$1")")/composition" ] || die 'active composition; restore first'
    local repo version session props id file config='' props_key sources_key targets_key pin=0 args=()
    local ids=()
    repo=$(repo_root "$1"); version=$2; shift 2
    for id in "$@"; do
        if [ "$id" = --pin-transitive ]; then pin=1; else args+=("$id"); fi
    done
    set -- ${args[@]+"${args[@]}"}
    valid_stamp "$version"; props=$repo/Directory.Packages.props
    [ -f "$props" ] || die 'no Directory.Packages.props (non-CPM). Manual alternative: untracked project copy, explicit PackageReference versions, dotnet restore --source FEED/nuget'
    session=$(session_path "$repo"); scratch
    if [ $# -gt 0 ]; then
        ids=("$@")
    elif [ -d "$FEED/nuget" ]; then
        while IFS= read -r -d '' file; do
            id=${file##*/}; id=${id%."$version".nupkg}
            ids+=("$id")
        done < <(find "$FEED/nuget" -type f -name "*.$version.nupkg" -print0)
    fi
    [ ${#ids[@]} -gt 0 ] || die 'no matching NuGet package IDs found'
    for id in "${ids[@]}"; do
        [[ "$id" =~ ^[A-Za-z0-9_.-]+$ ]] || die "invalid package ID: $id"
        [ -f "$FEED/nuget/$id.$version.nupkg" ] || die "package missing from feed: $id $version"
    done
    while IFS= read -r -d '' file; do
        case "$file" in
            nuget.config|NuGet.Config|NuGet.config)
                if grep -q '<packageSourceMapping' "$repo/$file"; then config=$repo/$file; fi ;;
        esac
    done < <(git -C "$repo" ls-files -z)
    if [ -n "$config" ]; then
        if ! grep -q '</packageSources>' "$config" || ! grep -q '</packageSourceMapping>' "$config"; then
            die 'cannot overlay incomplete source mapping configuration'
        fi
        if grep -q 'key="cratis-local"' "$config"; then die 'source key cratis-local already exists'; fi
    fi
    if [ "$DRY" -eq 1 ]; then
        quote_command mkdir -p "$session"
        printf 'write %s importing %s; override %s IDs to %s\n' "$session/Directory.Packages.props" "$props" "${#ids[@]}" "$version"
        printf 'write %s/env%s\n' "$session" "${config:+ and nuget.config}"
        return 0
    fi
    register_session "$repo" "$session" "$version"
    if [ "$private" = 1 ]; then register_private_cache "$repo" "$PRIVATE_NUGET_CACHE" "$session"; fi
    {
        nuget_props_start "$pin"
        for id in "${ids[@]}"; do
            # Remove/include works with any nearest props, not only root IDs.
            printf '    <PackageVersion Remove="%s" /><PackageVersion Include="%s" Version="%s" />\n' "$id" "$id" "$version"
        done
        nuget_overlay_end
    } > "$session/Directory.Packages.props"
    {
        nuget_targets_start
        for id in "${ids[@]}"; do
            printf '    <PackageReference Update="%s" Version="%s" />\n' "$id" "$version"
        done
        nuget_overlay_end
    } > "$session/cratis-local.targets"
    nuget_env "$session" > "$session/env"
    props_key=DirectoryPackages"PropsPath"; sources_key=RestoreAdditional"ProjectSources"; targets_key=CustomAfterMicrosoftCommon"Targets"
    printf ' %q' "-p:$props_key=$session/Directory.Packages.props" "-p:$sources_key=$FEED/nuget" "-p:$targets_key=$session/cratis-local.targets"
    if [ "${CustomAfterMicrosoftCommonTargets:-}" != "$session/cratis-local.targets" ]; then
        printf ' %q' "-p:_CratisLocalPreviousAfterMicrosoftCommonTargets=${CustomAfterMicrosoftCommonTargets:-}"
    else
        printf ' %q' "-p:_CratisLocalPreviousAfterMicrosoftCommonTargets=${_CratisLocalPreviousAfterMicrosoftCommonTargets:-}"
    fi
    printf '\n'
    printf 'source %q\n' "$session/env"
    if [ -n "$config" ]; then
        source_mapping "$config" "$session/nuget.config" "${ids[@]}"
        printf 'For restore, also pass: --configfile %q\n' "$session/nuget.config"
    elif [ -f "$session/nuget.config" ]; then
        rm -- "$session/nuget.config"
    fi
    registry_consumer "$version" nuget "$repo" "$session" "$PRIVATE_NUGET_CACHE"
    printf 'overrode %s NuGet package IDs\n' "${#ids[@]}"
    if [ "$private" != 1 ]; then
        printf 'Optional per-worktree cache (set before restore and clean): export NUGET_PACKAGES=%q\n' "$(git -C "$repo" rev-parse --absolute-git-dir)/cratis-local-nuget"
    fi
}

# A shared lock covers legacy mutations too: registration rollback must never replace
# another invocation's registry writes. An incomplete/unknown lock is preserved.
acquire_session_lock() {
    local lock=$FEED/.composition-lock
    if [ "$DRY" -eq 1 ]; then
        [ ! -e "$lock" ] && [ ! -L "$lock" ] || die 'session locked; recovery requires a non-dry-run mutation'
        return 0
    fi
    mkdir -p "$FEED"
    # Serialize inspection, journal recovery and replacement so a contender cannot
    # recover a stale owner and then delete the next invocation's live lock.
    # An abandoned guard fails closed, just like an incomplete/unknown lock.
    mkdir "$FEED/.composition-recovery" || die 'local package session is locked'
    SESSION_RECOVERY_GUARD=$FEED/.composition-recovery
    if [ -e "$lock" ] || [ -L "$lock" ]; then composition recover; fi
    mkdir "$lock" || die 'local package session is locked'
    SESSION_LOCK=$lock
    printf '%s\n' "$$" > "$lock/owner"
    rmdir -- "$SESSION_RECOVERY_GUARD"
    SESSION_RECOVERY_GUARD=''
}

# Opt-in composition uses Node orchestration and the selected SDK's native NuGet
# APIs. No npm dependency, persistent project, package restore or producer build.
composition() {
    need node; scratch
    local operation=$1 repo='' session=''
    shift
    case "$operation" in
        use-nuget|verify-assets|restore-state)
            [ $# -ge 1 ] || die "$operation requires a consumer"
            repo=$(repo_root "$1"); session=$(session_path "$repo") ;;
    esac
    nuget_props_start 0 > "$TMP/overlay-props"
    nuget_props_start 1 > "$TMP/overlay-props-pin"
    nuget_targets_start > "$TMP/overlay-targets"
    nuget_overlay_end > "$TMP/overlay-end"
    nuget_env "$session/composition" > "$TMP/overlay-env"
    node - "$operation" "$FEED" "$TMP" "$DRY" "$repo" "$session" "$@" <<'COMPOSITION'
const fs = require('node:fs');
const path = require('node:path');
const cp = require('node:child_process');
const crypto = require('node:crypto');
const [mode, feed, scratch, dry, repo, session, ...argv] = process.argv.slice(2);
const lock = path.join(feed, '.composition-lock');
const registry = path.join(feed, 'sessions');
const local = /-(?:local\.[0-9]{12}|train)\.g[0-9a-f]{7}(?:$|[^0-9A-Za-z])/;
const stamp = /^[0-9]+\.[0-9]+\.[0-9]+-(?:local\.[0-9]{12}|train)\.g[0-9a-f]{7}$/;
const idPattern = /^[A-Za-z0-9_.-]+$/;
const lower = s => s.toLowerCase();
const exists = p => fs.existsSync(p) || !!fs.lstatSync(path.dirname(p), {throwIfNoEntry:false}) && fs.readdirSync(path.dirname(p)).includes(path.basename(p));
function fail(message, code = 2) { const error = new Error(message); error.code = code; throw error; }
function plain(p, directory = false) {
    const st = fs.lstatSync(p, {throwIfNoEntry:false});
    if (!st || st.isSymbolicLink() || (directory ? !st.isDirectory() : !st.isFile())) fail('unsafe or missing path: ' + p);
}
function safeParents(p, ownedRoot = feed) {
    if (/[\x00-\x1f]/.test(p)) fail('control character in path');
    const root = path.resolve(ownedRoot), start = path.resolve(p);
    if (start !== root && !start.startsWith(root + path.sep)) fail('outside helper-owned directory: ' + p);
    for (let cur = start; ; cur = path.dirname(cur)) {
        const st = fs.lstatSync(cur, {throwIfNoEntry:false});
        if (st && (st.isSymbolicLink() || !st.isDirectory())) fail('unsafe directory: ' + cur);
        if (cur === root) break;
    }
}
function bytes(p) { if (!exists(p)) return null; plain(p); return fs.readFileSync(p).toString('base64'); }
function write(p, value) { fs.writeFileSync(p, value, {flag:'wx', mode:0o600}); }
function run(command, args) {
    const result = cp.spawnSync(command, args, {encoding:'utf8', maxBuffer:32 * 1024 * 1024});
    if (result.error || result.status !== 0) fail(command + ' failed: ' + (result.error?.message || result.stderr || result.stdout), 2);
    return result.stdout;
}
function manifest(file) {
    plain(file);
    const producers = new Map(), packages = new Map(), rows = [];
    for (const line of fs.readFileSync(file, 'utf8').split('\n')) {
        if (line === '' || line.startsWith('#')) continue;
        const row = line.split('\t');
        if (row.some(s => s === '' || /[\r\x00-\x08\x0b-\x1f]/.test(s))) fail('malformed composition row');
        if (row[0] === 'producer' && row.length === 4) {
            const [, name, version, worktree] = row;
            if (!idPattern.test(name) || producers.has(lower(name)) || !stamp.test(version) || !path.isAbsolute(worktree)) fail('invalid or duplicate producer: ' + name);
            plain(worktree, true);
            if (run('git', ['-C', worktree, 'rev-parse', '--show-toplevel']).trim() !== worktree) fail('producer must name an absolute worktree root: ' + worktree);
            producers.set(lower(name), {name, version, worktree});
        } else if (row[0] === 'package' && row.length === 3) rows.push(row);
        else fail('malformed composition row: ' + line);
    }
    for (const [, owner, id] of rows) {
        const producer = producers.get(lower(owner));
        if (!producer || !idPattern.test(id) || packages.has(lower(id))) fail('unknown producer or duplicate package ownership: ' + id);
        packages.set(lower(id), {id, producer:producer.name, version:producer.version, worktree:producer.worktree});
    }
    if (!producers.size || !packages.size) fail('empty composition');
    for (const p of producers.values()) if (![...packages.values()].some(pkg => pkg.producer === p.name)) fail('empty producer closure: ' + p.name);
    return [...packages.values()].sort((a,b) => lower(a.id).localeCompare(lower(b.id), 'en'));
}
const normalize = packages => packages.map(p => [p.producer,p.id,p.version,p.worktree].join('\t')).join('\n') + '\n';
const xml = s => s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('"','&quot;').replaceAll("'",'&apos;');
const msxml = s => xml(s.replace(/[%$@;?*()]/g, c => '%' + c.charCodeAt(0).toString(16)));
// All version/range comparisons and nuspec XML parsing run inside the SDK.
function native(packages, graphs = []) {
    const inputs = path.join(scratch, 'native-inputs.tsv');
    const checks = path.join(scratch, 'native-graphs.tsv');
    const result = path.join(scratch, 'native-result');
    let input = '';
    packages.forEach((pkg, i) => {
        const artifact = path.join(feed, 'nuget', `${pkg.id}.${pkg.version}.nupkg`);
        safeParents(path.dirname(artifact));
        if (!exists(artifact)) fail('missing: ' + artifact, 1);
        plain(artifact);
        let names;
        try { names = run('unzip', ['-Z1', artifact]).split('\n').filter(n => /\.nuspec$/i.test(n)); }
        catch (e) { fail('corrupt package: ' + artifact, 1); }
        if (names.length !== 1 || !/^[A-Za-z0-9_.-]+\.nuspec$/i.test(names[0])) fail('ambiguous nuspec: ' + artifact, 1);
        const extracted = cp.spawnSync('unzip', ['-p', artifact, names[0]], {maxBuffer:8 * 1024 * 1024});
        if (extracted.error || extracted.status !== 0) fail('unreadable nuspec: ' + artifact, 1);
        const nuspec = path.join(scratch, `package-${i}.xml`);
        fs.writeFileSync(nuspec, extracted.stdout);
        input += [pkg.id, pkg.version, nuspec].join('\t') + '\n';
    });
    fs.writeFileSync(inputs, input); fs.writeFileSync(checks, graphs.join('\n'));
    const project = path.join(scratch, 'native.proj');
    fs.writeFileSync(project, `<Project>
<UsingTask TaskName="CompositionValidation" TaskFactory="RoslynCodeTaskFactory" AssemblyFile="$(MSBuildToolsPath)/Microsoft.Build.Tasks.Core.dll">
<ParameterGroup><Sdk ParameterType="System.String" Required="true"/><Inputs ParameterType="System.String" Required="true"/><Graphs ParameterType="System.String" Required="true"/><Result ParameterType="System.String" Required="true"/></ParameterGroup>
<Task><Code Type="Fragment" Language="cs"><![CDATA[
var verdict = "0";
var advisories = new System.Text.StringBuilder();
try {
    var assembly = System.Reflection.Assembly.LoadFrom(System.IO.Path.Combine(Sdk, "NuGet.Versioning.dll"));
    var vt = assembly.GetType("NuGet.Versioning.NuGetVersion", true);
    var rt = assembly.GetType("NuGet.Versioning.VersionRange", true);
    var pv = vt.GetMethod("TryParse", new[] { typeof(string), vt.MakeByRefType() });
    var pr = rt.GetMethod("TryParse", new[] { typeof(string), rt.MakeByRefType() });
    var sat = rt.GetMethod("Satisfies", new[] { vt });
    if (pv == null || pr == null || sat == null) throw new MissingMethodException("native NuGet validation API unavailable");
    Func<string, object> version = text => {
        var args = new object[] { text, null };
        if (!(bool)pv.Invoke(null, args)) throw new FormatException("invalid version: " + text);
        return args[1];
    };
    Func<string, object> range = text => {
        var args = new object[] { text, null };
        if (String.IsNullOrWhiteSpace(text) || !(bool)pr.Invoke(null, args)) throw new FormatException("invalid range: " + text);
        return args[1];
    };
    Action<string, string, string> satisfies = (v, r, context) => {
        if (!(bool)sat.Invoke(range(r), new[] { version(v) })) throw new FormatException("dependency range: " + context + " " + r + " excludes " + v);
    };
    var fa = System.Reflection.Assembly.LoadFrom(System.IO.Path.Combine(Sdk, "NuGet.Frameworks.dll"));
    var ft = fa.GetType("NuGet.Frameworks.NuGetFramework", true);
    var reducerType = fa.GetType("NuGet.Frameworks.FrameworkReducer", true);
    var fp = ft.GetMethod("Parse", new[] { typeof(string) });
    var nearest = reducerType.GetMethod("GetNearest", new[] { ft, typeof(System.Collections.Generic.IEnumerable<>).MakeGenericType(ft) });
    if (fp == null || nearest == null) throw new MissingMethodException("native NuGet framework API unavailable");
    var reducer = Activator.CreateInstance(reducerType);
    Func<string, object> framework = text => fp.Invoke(null, new object[] { String.IsNullOrEmpty(text) ? "any" : text });
    var dependencies = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<object, System.Collections.Generic.HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
    var owned = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var rows = System.IO.File.ReadAllLines(Inputs);
    foreach (var row in rows) { var p = row.Split('\t'); version(p[1]); owned.Add(p[0], p[1]); }
    foreach (var row in rows) {
        var p = row.Split('\t');
        var document = new System.Xml.XmlDocument(); document.XmlResolver = null;
        var settings = new System.Xml.XmlReaderSettings(); settings.DtdProcessing = System.Xml.DtdProcessing.Prohibit; settings.XmlResolver = null;
        using (var reader = System.Xml.XmlReader.Create(p[2], settings)) document.Load(reader);
        var metadata = document.SelectNodes("/*[local-name()='package']/*[local-name()='metadata']");
        if (metadata.Count != 1) throw new FormatException("ambiguous metadata: " + p[0]);
        var ids = metadata[0].SelectNodes("*[local-name()='id']");
        var versions = metadata[0].SelectNodes("*[local-name()='version']");
        if (ids.Count != 1 || versions.Count != 1 || !String.Equals(ids[0].InnerText, p[0], StringComparison.OrdinalIgnoreCase) || versions[0].InnerText != p[1]) throw new FormatException("nuspec identity/version mismatch: " + p[0]);
        var groups = new System.Collections.Generic.Dictionary<object, System.Collections.Generic.HashSet<string>>();
        dependencies.Add(p[0], groups);
        var sections = metadata[0].SelectNodes("*[local-name()='dependencies']");
        if (sections.Count > 1) throw new FormatException("ambiguous dependency sections: " + p[0]);
        if (sections.Count == 1) {
            var grouped = sections[0].SelectNodes("*[local-name()='group']");
            if (grouped.Count == 0) groups.Add(framework("any"), new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase));
            else {
                if (sections[0].SelectNodes("*[local-name()='dependency']").Count != 0) throw new FormatException("mixed dependency groups: " + p[0]);
                foreach (System.Xml.XmlElement group in grouped) {
                    var target = framework(group.GetAttribute("targetFramework"));
                    if (groups.ContainsKey(target)) throw new FormatException("duplicate dependency group: " + p[0]);
                    groups.Add(target, new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
            }
        }
        foreach (System.Xml.XmlNode dep in document.SelectNodes("//*[local-name()='dependency']")) {
            var id = dep.Attributes["id"]; var r = dep.Attributes["version"];
            if (id == null || String.IsNullOrWhiteSpace(id.Value) || r == null || (dep.ParentNode.LocalName != "group" && dep.ParentNode.LocalName != "dependencies")) throw new FormatException("unreadable dependency: " + p[0]);
            var section = dep.ParentNode.LocalName == "group" ? dep.ParentNode.ParentNode : dep.ParentNode;
            if (sections.Count != 1 || section != sections[0]) throw new FormatException("misplaced dependency: " + p[0]);
            range(r.Value);
            if (owned.ContainsKey(id.Value)) {
                satisfies(owned[id.Value], r.Value, p[0] + " -> " + id.Value);
                var target = dep.ParentNode.LocalName == "group" ? framework(((System.Xml.XmlElement)dep.ParentNode).GetAttribute("targetFramework")) : framework("any");
                groups[target].Add(id.Value);
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(r.Value, @"-(local\\.[0-9]{12}|train)\\.g[0-9a-f]{7}")) throw new FormatException("undeclared local dependency: " + id.Value);
        }
    }
    foreach (var row in System.IO.File.ReadAllLines(Graphs)) {
        if (row.Length == 0) continue;
        var p = row.Split('\t');
        if (p[0] == "~") { if (!(bool)sat.Invoke(range(p[2]), new[] { version(p[1]) })) advisories.Append("\\ndependency range outside the composition: " + p[3] + " " + p[2] + " excludes " + p[1]); continue; }
        if (p[0] != "@") { satisfies(p[0], p[1], p[2]); continue; }
        var groups = dependencies[p[2]];
        if (groups.Count == 0) continue;
        var candidates = Array.CreateInstance(ft, groups.Count); var index = 0;
        foreach (var candidate in groups.Keys) candidates.SetValue(candidate, index++);
        var selected = nearest.Invoke(reducer, new object[] { framework(p[1].Split('/')[0]), candidates });
        if (selected == null) continue;
        var consumed = new System.Collections.Generic.HashSet<string>(p[3].Split(','), StringComparer.OrdinalIgnoreCase);
        var edges = new System.Collections.Generic.HashSet<string>(p[4].Split(','), StringComparer.OrdinalIgnoreCase);
        foreach (var required in groups[selected]) if (!consumed.Contains(required) || !edges.Contains(required)) throw new FormatException("required local dependency not consumed: " + p[2] + " -> " + required + " " + p[1]);
    }
    verdict = "0" + advisories.ToString();
} catch (Exception e) {
    verdict = (e is MissingMethodException || e is System.IO.FileNotFoundException || e is TypeLoadException ? "2" : "1") + "\\n" + e.Message;
}
System.IO.File.WriteAllText(Result, verdict);
]]></Code></Task></UsingTask>
<Target Name="Validate"><CompositionValidation Sdk="$(MSBuildToolsPath)" Inputs="${msxml(inputs)}" Graphs="${msxml(checks)}" Result="${msxml(result)}"/></Target></Project>`);
    const execution = cp.spawnSync('dotnet', ['msbuild', project, '-t:Validate', '-nologo', '-v:quiet', '-m:1', '-nodeReuse:false'], {encoding:'utf8', maxBuffer:8 * 1024 * 1024});
    if (execution.error || execution.status !== 0 || !exists(result)) fail('native SDK validator unavailable: ' + (execution.error?.message || execution.stdout || execution.stderr));
    const [code, ...message] = fs.readFileSync(result,'utf8').split('\n');
    if (code !== '0') fail(message.join('\n'), Number(code));
    return message.filter(Boolean);
}
function assets(packages) {
    const owned = new Map(packages.map(p => [lower(p.id), p]));
    const files = [], resolved = new Map(), checks = [];
    function walk(dir) {
        for (const entry of fs.readdirSync(dir, {withFileTypes:true})) {
            if (entry.isSymbolicLink() || ['.git','node_modules'].includes(entry.name)) continue;
            const file = path.join(dir,entry.name);
            if (entry.isDirectory()) walk(file);
            else if (entry.name === 'project.assets.json' && path.basename(dir) === 'obj') files.push(file);
        }
    }
    walk(repo);
    if (!files.length) fail('no obj/project.assets.json (restore the consumer first)');
    const object = value => value && typeof value === 'object' && !Array.isArray(value);
    for (const file of files) {
        let data;
        try { data = JSON.parse(fs.readFileSync(file,'utf8')); } catch { fail('malformed assets: ' + file,1); }
        if (!object(data.libraries) || !object(data.targets) || !Object.keys(data.targets).length || data.logs?.some(log => log.level === 'Error')) fail('invalid or failed restore assets: ' + file,1);
        const libraries = new Map();
        function keyParts(key, value) {
            const parts = key.split('/');
            if (parts.length !== 2 || !idPattern.test(parts[0]) || !parts[1] || !object(value) || !['package','project'].includes(value.type)) fail('malformed asset entry: ' + key + ' ' + file,1);
            return parts;
        }
        for (const [key, lib] of Object.entries(data.libraries)) {
            const [id, version] = keyParts(key,lib);
            if (lib.type !== 'package') continue;
            // Multitarget restores can hold several versions of one ID, one per framework.
            if (libraries.has(lower(id) + '/' + version)) fail('duplicate assets library: ' + key,1);
            libraries.set(lower(id) + '/' + version, {id,version});
            const pkg = owned.get(lower(id));
            if (pkg && version !== pkg.version || !pkg && local.test(version)) fail('wrong or undeclared local library: ' + key + ' ' + file,1);
        }
        const used = new Set();
        for (const [framework, target] of Object.entries(data.targets)) {
            if (!object(target) || /[\t\r\n]/.test(framework)) fail('malformed assets target: ' + file,1);
            const nodes = new Map();
            for (const [key,node] of Object.entries(target)) {
                const [id,version] = keyParts(key,node);
                if (nodes.has(lower(id))) fail('duplicate target ID: ' + id,1);
                nodes.set(lower(id), {id,version,node});
                if (node.type !== 'package') continue;
                if (!libraries.has(lower(id) + '/' + version)) fail('target/library mismatch: ' + key + ' ' + file,1);
                used.add(lower(id) + '/' + version);
                checks.push([version,`[${version}]`,`${file} ${framework} ${id}`].join('\t'));
                if (owned.has(lower(id))) resolved.set(lower(id),owned.get(lower(id)));
            }
            const packageIds = [...nodes.values()].filter(n => n.node.type === 'package').map(n => n.id).join(',');
            for (const {id,node} of nodes.values()) {
                if (owned.has(lower(id)) && node.type === 'package') checks.push(['@',framework,id,packageIds,Object.keys(node.dependencies || {}).join(',')].join('\t'));
                if (node.dependencies !== undefined && !object(node.dependencies)) fail('malformed target dependencies: ' + id,1);
                for (const [dep,range] of Object.entries(node.dependencies || {})) {
                    if (typeof range !== 'string' || /[\t\r\n]/.test(range)) fail('malformed asset range: ' + dep,1);
                    const selected = nodes.get(lower(dep));
                    if (!selected) fail('unresolved target dependency: ' + id + ' -> ' + dep + ' ' + file,1);
                    // An edge between two IDs outside the composition is NuGet's own NU1608 case and exists
                    // without the overlay too; report it as a warning instead of refusing the composition.
                    if (selected.node.type === 'package' && (owned.has(lower(id)) || owned.has(lower(dep)))) checks.push([selected.version,range,`${file} ${framework} ${id} -> ${dep}`].join('\t'));
                    else if (selected.node.type === 'package') checks.push(['~',selected.version,range,`${file} ${framework} ${id} -> ${dep}`].join('\t'));
                    if (owned.has(lower(dep)) && selected.node.type !== 'package') fail('owned dependency resolved as project: ' + dep,1);
                }
            }
        }
        for (const [key, {id}] of libraries) if (owned.has(lower(id)) && !used.has(key)) fail('owned library absent from targets: ' + id + ' ' + file,1);
    }
    if (!resolved.size) fail('verify-assets: no composition package consumed',1);
    return {checks, packages:[...resolved.values()], files:files.length};
}
// Journal contains only the transaction's exact expected bytes. Recovery refuses
// drift, unknown files and symlinks rather than deleting another task's work.
function directoryBytes(dir) {
    plain(dir,true); const result = {};
    for (const name of fs.readdirSync(dir)) { plain(path.join(dir,name)); result[name] = bytes(path.join(dir,name)); }
    return result;
}
function same(a,b) { return JSON.stringify(Object.entries(a).sort()) === JSON.stringify(Object.entries(b).sort()); }
function removeOwned(dir, expected, partial = false) {
    if (!exists(dir)) return;
    const actual = directoryBytes(dir);
    if (partial ? Object.keys(actual).some(n => actual[n] !== expected[n]) : !same(actual,expected)) fail('transaction files changed; preserved: ' + dir);
    for (const name of Object.keys(actual)) fs.unlinkSync(path.join(dir,name));
    fs.rmdirSync(dir);
}
function journal(value) {
    const file = path.join(lock,'journal.json');
    fs.writeFileSync(file + '.next', JSON.stringify(value), {mode:0o600});
    fs.renameSync(file + '.next', file);
}
function recoverState(j) {
    if (j.schema !== 1 || !path.isAbsolute(j.repo) || !path.isAbsolute(j.session) || j.session === '/' || j.session === j.repo || !/^[a-f0-9]{24}$/.test(j.token) || !j.files || Object.keys(j.files).some(n => !['manifest.tsv','Directory.Packages.props','cratis-local.targets','nuget.config','env','transaction','state.json'].includes(n))) fail('unknown transaction journal');
    safeParents(j.session,j.session);
    const pub = path.join(j.session,'composition'), stage = path.join(j.session, '.composition-' + j.token);
    if (j.operation === 'activate') {
        if (!j.committed) {
            const current = bytes(registry);
            if (current !== j.registryBefore && current !== j.registryAfter) fail('registry changed; transaction preserved');
            if (exists(pub)) removeOwned(pub,j.files);
            if (current === j.registryAfter && current !== j.registryBefore) {
                if (j.registryBefore === null) fs.unlinkSync(registry);
                else { fs.writeFileSync(path.join(lock,'registry.rollback'),Buffer.from(j.registryBefore,'base64')); fs.renameSync(path.join(lock,'registry.rollback'),registry); }
            }
            const marker = path.join(j.session,'repo');
            if (!j.hadRepo && exists(marker)) {
                if (bytes(marker) !== Buffer.from(j.repo + '\n').toString('base64')) fail('session marker changed; preserved');
                fs.unlinkSync(marker);
            }
        }
        removeOwned(stage,j.files,true);
        const pending = path.join(lock,'registry.next');
        if (exists(pending)) {
            if (bytes(pending) !== j.registryAfter) fail('staged registry changed; preserved');
            fs.unlinkSync(pending);
        }
        if (!j.committed && !j.hadSession && exists(j.session) && fs.readdirSync(j.session).length === 0) fs.rmdirSync(j.session);
    } else if (j.operation === 'restore') {
        if (!j.committed && exists(stage)) {
            if (exists(pub) || !same(directoryBytes(stage),j.files)) fail('restoration state changed; preserved');
            fs.renameSync(stage,pub);
        } else if (j.committed) removeOwned(stage,j.files);
    } else fail('unknown transaction operation');
    fs.unlinkSync(path.join(lock,'journal.json'));
}
function recover() {
    safeParents(lock); plain(lock,true); plain(path.join(lock,'owner'));
    const owner = fs.readFileSync(path.join(lock,'owner'),'utf8').trim();
    if (!/^[1-9][0-9]*$/.test(owner)) fail('unknown lock owner; preserved');
    try { process.kill(Number(owner),0); fail('local package session is locked by ' + owner); }
    catch (error) { if (error.code !== 'ESRCH') throw error; }
    const allowed = new Set(['owner','journal.json','journal.json.next','registry.next','registry.rollback']);
    if (fs.readdirSync(lock).some(n => !allowed.has(n))) fail('unknown lock state; preserved');
    for (const n of fs.readdirSync(lock)) plain(path.join(lock,n));
    if (exists(path.join(lock,'journal.json'))) recoverState(JSON.parse(fs.readFileSync(path.join(lock,'journal.json'),'utf8')));
    else if (fs.readdirSync(lock).some(n => n !== 'owner')) fail('incomplete transaction journal; preserved');
    for (const n of fs.readdirSync(lock)) fs.unlinkSync(path.join(lock,n));
    fs.rmdirSync(lock);
}
// Fixture-only fault injection requires the isolated self-test capability file.
function boundary(name) {
    const capability = process.env.CRATIS_LOCAL_COMPOSITION_TEST_ROOT;
    if (!capability || !feed.startsWith(capability + path.sep) || !fs.existsSync(path.join(capability,'.composition-fixture'))) return;
    if (process.env.CRATIS_LOCAL_COMPOSITION_FAIL === name) fail('injected failure: ' + name,1);
    if (process.env.CRATIS_LOCAL_COMPOSITION_INTERRUPT === name) { process.kill(process.ppid,'SIGKILL'); process.kill(process.pid,'SIGKILL'); }
}
function activeFiles() {
    const pub = path.join(session,'composition');
    if (!exists(pub)) return null;
    const files = directoryBytes(pub);
    const allowed = new Set(['manifest.tsv','Directory.Packages.props','cratis-local.targets','nuget.config','env','transaction','state.json']);
    if (Object.keys(files).some(n => !allowed.has(n)) || !files['state.json'] || !files.transaction || !/^[a-f0-9]{24}\n$/.test(Buffer.from(files.transaction,'base64').toString())) fail('unsafe composition session');
    const expected = JSON.parse(Buffer.from(files['state.json'],'base64').toString());
    const actual = {...files}; delete actual.transaction; delete actual['state.json'];
    if (!same(actual,expected)) fail('composition files changed; preserved');
    return files;
}
function activate(packages,pin) {
    safeParents(session,session); safeParents(feed);
    const props = path.join(repo,'Directory.Packages.props'); plain(props);
    if (exists(session)) {
        plain(session,true);
        const allowed = new Set(['repo','composition','nuget-cache']);
        if (fs.readdirSync(session).some(n => !allowed.has(n))) fail('conflicting active overlay/session; restore first');
        if (exists(path.join(session,'repo')) && fs.readFileSync(path.join(session,'repo'),'utf8') !== repo + '\n') fail('session belongs to another checkout');
    }
    const pub = path.join(session,'composition');
    const template = name => fs.readFileSync(path.join(scratch,'overlay-' + name),'utf8');
    const escaped = packages.map(p => `<PackageVersion Remove="${xml(p.id)}"/><PackageVersion Include="${xml(p.id)}" Version="${p.version}"/>`).join('\n');
    const references = packages.map(p => `<PackageReference Update="${xml(p.id)}" Version="${p.version}"/>`).join('\n');
    const files = {
        'manifest.tsv':normalize(packages),
        'Directory.Packages.props':template(pin ? 'props-pin' : 'props') + escaped + '\n' + template('end'),
        'cratis-local.targets':template('targets') + references + '\n' + template('end'),
        env:template('env')
    };
    // Native XML manipulation preserves namespaces/configuration and makes exact
    // local mappings exclusive; remote wildcards remain less specific.
    const configs = run('git',['-C',repo,'ls-files','-z']).split('\0').filter(n => ['nuget.config','NuGet.Config','NuGet.config'].includes(n));
    if (configs.length > 1) fail('ambiguous root NuGet config');
    if (configs.length) files['nuget.config'] = configOverlay(path.join(repo,configs[0]),packages);
    else files['nuget.config'] = configOverlay(null,packages);
    files['state.json'] = JSON.stringify(Object.fromEntries(Object.entries(files).map(([k,v]) => [k,Buffer.from(v).toString('base64')]))) + '\n';
    const previous = activeFiles();
    if (previous) {
        const expected = Object.fromEntries(Object.entries(files).map(([k,v]) => [k,Buffer.from(v).toString('base64')]));
        const withoutToken = {...previous}; delete withoutToken.transaction;
        if (!same(expected,withoutToken) || !fs.readFileSync(registry,'utf8').split('\n').includes(session)) fail('different or unregistered active composition; restore first');
        instructions(packages); return;
    }
    if (dry === '1') { console.log('dry-run: validated ' + packages.length + ' composition packages; no overlay written'); return; }
    const token = crypto.randomBytes(12).toString('hex'); files.transaction = token + '\n';
    const expected = Object.fromEntries(Object.entries(files).map(([k,v]) => [k,Buffer.from(v).toString('base64')]));
    const before = bytes(registry);
    const oldRegistry = before === null ? '' : Buffer.from(before,'base64').toString();
    if (oldRegistry && !oldRegistry.endsWith('\n')) fail('malformed session registry');
    const after = oldRegistry.split('\n').includes(session) ? oldRegistry : oldRegistry + session + '\n';
    const j = {schema:1,operation:'activate',session,repo,token,files:expected,hadSession:exists(session),hadRepo:exists(path.join(session,'repo')),registryBefore:before,registryAfter:Buffer.from(after).toString('base64'),committed:false};
    journal(j);
    try {
        boundary('journal');
        if (!j.hadSession) { fs.mkdirSync(path.dirname(session), {recursive:true}); fs.mkdirSync(session); }
        boundary('session');
        const stage = path.join(session,'.composition-' + token); fs.mkdirSync(stage);
        // A partial stage is recorded before each file write for exact recovery.
        for (const [name,value] of Object.entries(files)) { write(path.join(stage,name),value); boundary('stage-' + name); }
        boundary('staged');
        if (bytes(registry) !== before) fail('registry changed before publication');
        fs.renameSync(stage,pub); boundary('published');
        if (!j.hadRepo) write(path.join(session,'repo'),repo + '\n');
        boundary('repo');
        write(path.join(lock,'registry.next'),after); boundary('registration-staged');
        if (bytes(registry) !== before) fail('registry changed before registration');
        fs.renameSync(path.join(lock,'registry.next'),registry); boundary('registered');
        j.committed = true; journal(j); boundary('committed');
        recoverState(j);
    } catch (error) {
        if (!j.committed) recoverState(j);
        throw error;
    }
    instructions(packages);
}
function shell(s) { return "'" + s.replaceAll("'", "'\\''") + "'"; }
function instructions(packages) {
    const pub = path.join(session,'composition');
    console.log('source ' + shell(path.join(pub,'env')));
    console.log('For restore, also pass: --configfile ' + shell(path.join(pub,'nuget.config')));
    console.log('overrode ' + packages.length + ' NuGet package IDs in composition');
}
// XML config overlay is implemented by a second SDK task in the same temporary
// project, using XmlDocument rather than a regex/token-based XML rewrite.
function configOverlay(config,packages) {
    if (config) plain(config);
    const output = path.join(scratch,'config.xml'), project = path.join(scratch,'config.proj');
    const ids = path.join(scratch,'config-ids'); fs.writeFileSync(ids,packages.map(p => p.id).join('\n'));
    fs.writeFileSync(project, `<Project><UsingTask TaskName="CompositionConfig" TaskFactory="RoslynCodeTaskFactory" AssemblyFile="$(MSBuildToolsPath)/Microsoft.Build.Tasks.Core.dll"><ParameterGroup><Input ParameterType="System.String"/><Output ParameterType="System.String"/><Feed ParameterType="System.String"/><Ids ParameterType="System.String"/></ParameterGroup><Task><Code Type="Fragment" Language="cs"><![CDATA[
var doc = new System.Xml.XmlDocument(); doc.XmlResolver = null;
if (Input.Length == 0) doc.LoadXml("<configuration><packageSources><clear/><add key='nuget.org' value='https://api.nuget.org/v3/index.json'/></packageSources></configuration>");
else { var settings = new System.Xml.XmlReaderSettings(); settings.DtdProcessing = System.Xml.DtdProcessing.Prohibit; settings.XmlResolver = null; using (var reader = System.Xml.XmlReader.Create(Input, settings)) doc.Load(reader); }
var root = doc.DocumentElement;
if (root.Name != "configuration") throw new FormatException("invalid NuGet config");
var sources = root.SelectNodes("packageSources");
if (sources.Count != 1 || root.SelectNodes("packageSourceMapping").Count > 1) throw new FormatException("ambiguous NuGet sources/mapping");
foreach (System.Xml.XmlElement source in sources[0].SelectNodes("add")) {
    if (String.Equals(source.GetAttribute("key"),"cratis-local",StringComparison.OrdinalIgnoreCase)) throw new FormatException("source key cratis-local already exists");
    var value = source.GetAttribute("value"); Uri uri;
    if (Input.Length > 0 && !System.IO.Path.IsPathRooted(value) && !Uri.TryCreate(value, UriKind.Absolute, out uri) && !value.StartsWith("%")) source.SetAttribute("value",System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Input),value)));
}
var localSource = doc.CreateElement("add"); localSource.SetAttribute("key","cratis-local"); localSource.SetAttribute("value",Feed); sources[0].AppendChild(localSource);
var mapping = root.SelectSingleNode("packageSourceMapping");
if (mapping != null) foreach (System.Xml.XmlElement source in mapping.SelectNodes("packageSource")) if (String.Equals(source.GetAttribute("key"),"cratis-local",StringComparison.OrdinalIgnoreCase)) throw new FormatException("mapping key cratis-local already exists");
foreach (System.Xml.XmlElement source in root.SelectNodes("disabledPackageSources/add")) if (String.Equals(source.GetAttribute("key"),"cratis-local",StringComparison.OrdinalIgnoreCase)) throw new FormatException("source key cratis-local is disabled");
if (mapping == null) {
    mapping = doc.CreateElement("packageSourceMapping"); root.AppendChild(mapping);
    foreach (System.Xml.XmlElement source in sources[0].SelectNodes("add")) {
        if (source.GetAttribute("key") == "cratis-local") continue;
        var remote = doc.CreateElement("packageSource"); remote.SetAttribute("key",source.GetAttribute("key"));
        var wildcard = doc.CreateElement("package"); wildcard.SetAttribute("pattern","*"); remote.AppendChild(wildcard); mapping.AppendChild(remote);
    }
}
var localMapping = doc.CreateElement("packageSource"); localMapping.SetAttribute("key","cratis-local");
foreach (var id in System.IO.File.ReadAllLines(Ids)) {
    foreach (System.Xml.XmlElement entry in mapping.SelectNodes("packageSource/package")) if (String.Equals(entry.GetAttribute("pattern"),id,StringComparison.OrdinalIgnoreCase)) entry.ParentNode.RemoveChild(entry);
    var entryLocal = doc.CreateElement("package"); entryLocal.SetAttribute("pattern",id); localMapping.AppendChild(entryLocal);
}
mapping.AppendChild(localMapping); doc.Save(Output);
]]></Code></Task></UsingTask><Target Name="Overlay"><CompositionConfig Input="${msxml(config || '')}" Output="${msxml(output)}" Feed="${msxml(path.join(feed,'nuget'))}" Ids="${msxml(ids)}"/></Target></Project>`);
    run('dotnet',['msbuild',project,'-t:Overlay','-nologo','-v:quiet','-m:1','-nodeReuse:false']);
    return fs.readFileSync(output,'utf8');
}
function restoreState() {
    const files = activeFiles(); if (!files) return;
    if (dry === '1') { console.log('dry-run: remove composition overlay'); return; }
    const token = crypto.randomBytes(12).toString('hex');
    const j = {schema:1,operation:'restore',session,repo,token,files,committed:false};
    journal(j);
    try {
        boundary('restore-journal');
        fs.renameSync(path.join(session,'composition'),path.join(session,'.composition-' + token)); boundary('restore-renamed');
        j.committed = true; journal(j); boundary('restore-committed'); recoverState(j);
    } catch (error) { if (!j.committed) recoverState(j); throw error; }
}
function cleanGuard() {
    const knownSessions = new Set(argv[1] ? [argv[1]] : []);
    if (exists(registry)) { plain(registry); for (const known of fs.readFileSync(registry,'utf8').split('\n').filter(Boolean)) knownSessions.add(known); }
    const selected = argv[0] || '';
    for (const known of knownSessions) {
        if (!path.isAbsolute(known)) fail('invalid registered session');
        safeParents(known,known);
        const composition = path.join(known,'composition');
        if (!exists(composition)) continue;
        plain(composition,true); plain(path.join(composition,'manifest.tsv'));
        const versions = fs.readFileSync(path.join(composition,'manifest.tsv'),'utf8').trim().split('\n').map(row => row.split('\t')[2]);
        if (mode === 'clean-members') { for (const version of versions) console.log(version); }
        else if (!selected || selected === 'all' || versions.includes(selected)) fail('active composition retains member artifacts; restore first: ' + known);
    }
}
try {
    safeParents(feed);
    if (mode === 'recover') recover();
    else if (mode === 'restore-state') restoreState();
    else if (mode === 'clean-guard' || mode === 'clean-members') cleanGuard();
    else {
        const args = [...argv]; if (mode !== 'verify-pack') args.shift();
        if (args.shift() !== '--composition' || !args.length) fail('expected --composition FILE');
        const file = args.shift(); let json = false, pin = false;
        for (const arg of args) {
            if (arg === '--json' && mode !== 'use-nuget' && !json) json = true;
            else if (arg === '--pin-transitive' && mode === 'use-nuget' && !pin) pin = true;
            else fail('cannot mix composition with stamp, IDs or other options: ' + arg);
        }
        const packages = manifest(file);
        const consumed = mode === 'verify-assets' ? assets(packages) : null;
        const warnings = native(packages,consumed?.checks);
        for (const warning of warnings) console.error('WARNING: ' + warning);
        if (mode === 'use-nuget') activate(packages,pin);
        else if (json) console.log(JSON.stringify({packages:(consumed?.packages || packages).map(({id,version,producer}) => ({id,version,producer})), warnings}));
        else console.log(mode + ': ' + (consumed?.packages.length || packages.length) + ' composition packages' + (consumed ? ', ' + consumed.files + ' assets files' : '') + ', native NuGet validation passed');
    }
} catch (error) { console.error('ERROR: ' + error.message); process.exitCode = Number.isInteger(error.code) ? error.code : 2; }
COMPOSITION
}

# Dependency map: tab-separated rows, '#' comments (see references/dependency-map.md).
# Everything below is derived from these rows; no function reads a feed listing to decide what to expect.
MAP=${CRATIS_LOCAL_MAP:-$(dirname "$SELF")/dependency-map.tsv}
map_require() {
    [ -f "$MAP" ] || die "dependency map not found: $MAP"
    awk -F'\t' '
        /^#/ || NF == 0 { next }
        $2 == "" { bad = 1 }
        $1 == "pack" && (NF == 4 || NF == 5) && ($3 == "nuget" || $3 == "npm") && $4 != "" && $2 != "" { next }
        $1 == "ids" && NF == 4 && ($3 == "nuget" || $3 == "npm") && $4 != "" && $2 != "" { next }
        $1 == "uses" && NF == 5 && ($4 == "nuget" || $4 == "npm" || $4 == "image") && $3 != "" && $5 != "" && $2 != "" { next }
        $1 == "image" && NF == 4 && $2 != "" { next }
        { printf "%s:%d: malformed dependency map row: %s\n", FILENAME, NR, $0 > "/dev/stderr"; bad = 1 }
        END { exit bad ? 1 : 0 }' "$MAP" || exit 2
}
map_rows() { awk -F'\t' -v kind="$1" '/^#/ || NF == 0 { next } $1 == kind' "$MAP"; }
map_known() { awk -F'\t' -v n="$1" '/^#/ || NF == 0 { next } $2 == n || ($1 == "uses" && $3 == n) { found = 1 } END { exit !found }' "$MAP"; }
# Edge rows "eco<TAB>manifest" for consumer $1 using producer $2.
map_edge() { map_rows uses | awk -F'\t' -v c="$1" -v p="$2" '$2 == c && $3 == p { print $4 "\t" $5 }'; }
# Expected package IDs of producer $1 in ecosystem $2, one per line; return 1 when the map has no ids row.
map_ids() {
    local rows
    rows=$(map_rows ids | awk -F'\t' -v r="$1" -v e="$2" '$2 == r && $3 == e { n = split($4, a, ","); for (i = 1; i <= n; i++) if (a[i] != "") print a[i] }')
    [ -n "$rows" ] || return 1
    printf '%s\n' "$rows"
}
# Pack rows of producer $1 in ecosystem $2 as "target<TAB>PROP=VAL,..." (properties may be empty).
map_pack_rows() { map_rows pack | awk -F'\t' -v r="$1" -v e="$2" '$2 == r && $3 == e { print $4 "\t" $5 }'; }
# True when the manifest rows of uses edge "$1 uses $2" include ecosystem $3.
map_uses() { map_edge "$1" "$2" | awk -F'\t' -v e="$3" '$1 == e { found = 1 } END { exit !found }'; }

# plan_derive OUTFILE CONSUMER PRODUCER...: the one plan every other command works from. Producers are in
# dependency order. Rows (TSV) are written to OUTFILE only when the whole plan is valid; any problem is die (exit 2).
# Call it directly, never inside $(...) or a pipeline, so the exit cannot be lost in a subshell.
#   pack <producer> <eco>            eco nuget|npm: a named downstream repository uses it in that ecosystem
#   overlay <producer> <upstream>    producer consumes an earlier-named upstream through NuGet
#   consume <producer> <eco> <path>  the consumer's own edge (eco nuget|npm|image) and its manifest
plan_derive() {
    local out=$1 consumer=$2 i j eco name edge manifests
    shift 2
    [ $# -ge 1 ] || die 'a plan needs a consumer and at least one producer'
    local names=("$consumer" "$@")
    map_require
    for ((i=0; i<${#names[@]}; i++)); do
        map_known "${names[$i]}" || die "repository not in the dependency map: ${names[$i]}"
        for ((j=i+1; j<${#names[@]}; j++)); do
            [ "${names[$i]}" != "${names[$j]}" ] || die "repository named twice (a consumer cannot also be a producer): ${names[$i]}"
        done
    done
    for ((i=1; i<${#names[@]}; i++)); do
        for ((j=i+1; j<${#names[@]}; j++)); do
            [ -z "$(map_edge "${names[$i]}" "${names[$j]}")" ] || die "order: ${names[$i]} uses ${names[$j]}; list ${names[$j]} first"
        done
    done
    : > "$out"
    for ((i=1; i<${#names[@]}; i++)); do
        name=${names[$i]}
        edge=0
        for eco in nuget npm image; do
            for ((j=0; j<${#names[@]}; j++)); do
                [ "$j" -eq 0 ] || [ "$j" -gt "$i" ] || continue
                if map_uses "${names[$j]}" "$name" "$eco"; then
                    edge=1
                    if [ "$eco" != image ]; then plan_add_pack "$out" "$name" "$eco"; fi
                    break
                fi
            done
        done
        [ "$edge" -eq 1 ] || die "no edge: nothing named after $name uses it (check the order and the map)"
    done
    for ((i=2; i<${#names[@]}; i++)); do
        for ((j=1; j<i; j++)); do
            if map_uses "${names[$i]}" "${names[$j]}" nuget; then printf 'overlay\t%s\t%s\n' "${names[$i]}" "${names[$j]}" >> "$out"; fi
        done
    done
    for ((i=1; i<${#names[@]}; i++)); do
        map_edge "$consumer" "${names[$i]}" | awk -F'\t' -v p="${names[$i]}" '{ print "consume\t" p "\t" $1 "\t" $2 }' >> "$out"
    done
    manifests=$(awk -F'\t' '$1 == "consume" && $3 == "npm" { print $4 }' "$out" | sort -u | wc -l | tr -d ' ')
    [ "$manifests" -le 1 ] || die "$consumer uses npm producers from different directories; only one npm overlay per session is supported"
    plan_warn_unnamed_hops "${names[@]}"
}
# Append "pack <producer> <eco>" after checking the map can verify it (an ids row) and build it (a pack row).
plan_add_pack() {
    map_ids "$2" "$3" > /dev/null || die "no ids row for $2 ($3): cannot verify the planned pack"
    [ -n "$(map_pack_rows "$2" "$3")" ] || die "no pack row for $2 ($3) in the map"
    printf 'pack\t%s\t%s\n' "$2" "$3" >> "$1"
}
# A producer that reaches the consumer through a repository that is not named keeps the released copy of it.
plan_warn_unnamed_hops() {
    local consumer=$1 kind x p eco manifest named
    shift
    named=$(printf '%s\n' "$@")
    map_rows uses | while IFS=$'\t' read -r kind x p eco manifest; do
        [ "$kind" = uses ] && [ "$eco" != '' ] || continue
        printf '%s\n' "$named" | grep -Fxq "$x" && continue
        printf '%s\n' "$named" | grep -Fxq "$p" || continue
        [ "$p" != "$consumer" ] || continue
        [ -n "$(map_edge "$consumer" "$x")" ] || continue
        printf 'WARNING: %s -> %s -> %s: %s is not named, so the released %s is kept; name it to rebuild it (a binary break is a finding)\n' "$p" "$x" "$consumer" "$x" "$x" >&2
    done
}

# closure [--map FILE] [--train] CONSUMER=PATH PRODUCER=PATH...
# A printer over plan_derive: it decides nothing the plan does not already say, and prints nothing until the
# whole plan is valid. Every verify command it prints names the consumer and ALL producers in the given order,
# so each one re-derives the identical plan (no state is stored between commands).
# shellcheck disable=SC2016  # printed commands keep a literal $STAMP
closure() {
    local arg name path i j eco row target props prop manifest mapopt='' train='' overlaid scope='' upstreams dirarg=''
    local names=() paths=()
    while [ $# -gt 0 ]; do
        arg=$1; shift
        case "$arg" in
            --map) [ $# -gt 0 ] || die '--map requires a file'; MAP=$1; printf -v mapopt ' --map %q' "$1"; shift ;;
            --train) train=' --train' ;;
            *=*) name=${arg%%=*}; path=${arg#*=}; names+=("$name"); paths+=("$(repo_root "$path")") ;;
            *) die "closure arguments are NAME=PATH; got: $arg" ;;
        esac
    done
    [ ${#names[@]} -ge 2 ] || die 'closure requires CONSUMER=PATH and at least one PRODUCER=PATH'
    scratch
    plan_derive "$TMP/closure-plan" "${names[@]}"
    for ((i=1; i<${#names[@]}; i++)); do printf -v arg ' --producer %q' "${names[$i]}"; scope="$scope$arg"; done
    scope=" --consumer $(printf '%q' "${names[0]}")$scope$mapopt"
    printf '# closure for %s (printed, not executed; run each step and check its exit code)\n' "${names[0]}"
    printf 'STAMP=$(bash %q stamp%s' "$SELF" "$train"
    for ((i=1; i<${#paths[@]}; i++)); do printf ' %q' "${paths[$i]}"; done
    printf ')\n'
    for ((i=1; i<${#names[@]}; i++)); do
        name=${names[$i]}; overlaid=0
        upstreams=$(awk -F'\t' -v p="$name" '$1 == "overlay" && $2 == p { printf "%s ", $3 }' "$TMP/closure-plan")
        if [ -n "$upstreams" ]; then
            overlaid=1
            printf '# %s consumes %s: overlay it, then pack in a shell that sourced the printed env file\n' "$name" "${upstreams% }"
            printf 'bash %q use-nuget %q "$STAMP"\n' "$SELF" "${paths[$i]}"
        fi
        for eco in nuget npm; do
            grep -Fxq "$(printf 'pack\t%s\t%s' "$name" "$eco")" "$TMP/closure-plan" || continue
            if [ "$eco" = npm ]; then
                printf '( cd %q && bash %q pack-npm . "$STAMP" )   # needs yarn install --immutable and the repository build first; pack-npm does not build\n' "${paths[$i]}" "$SELF"
                continue
            fi
            while IFS=$'\t' read -r target props; do
                printf '( cd %q && ' "${paths[$i]}"
                if [ "$overlaid" -eq 1 ]; then printf '. %q && ' "$(session_path "${paths[$i]}")/env"; fi
                printf 'bash %q pack-nuget . "$STAMP"' "$SELF"
                for prop in ${props//,/ }; do printf ' --property %q' "$prop"; done
                printf ' %q )\n' "$target"
            done < <(map_pack_rows "$name" nuget)
        done
        if [ "$overlaid" -eq 1 ]; then printf 'bash %q restore %q\n' "$SELF" "${paths[$i]}"; fi
        for ((j=i+1; j<${#names[@]}; j++)); do
            while IFS=$'\t' read -r eco manifest; do
                case "$eco" in npm|image) printf '# note: %s uses %s (%s, %s): not overlaid here; wire it by hand if the %s build needs the local %s\n' "${names[$j]}" "$name" "$eco" "$manifest" "${names[$j]}" "$name" ;; esac
            done < <(map_edge "${names[$j]}" "$name")
        done
    done
    if grep -q '^pack' "$TMP/closure-plan"; then
        printf 'bash %q verify-pack "$STAMP"%s\n' "$SELF" "$scope"
    else
        printf '# no pack rows in this plan (image-only edges): nothing to verify in the feed\n'
    fi
    for eco in nuget npm image; do
        row=$(awk -F'\t' -v e="$eco" '$1 == "consume" && $3 == e { print $4; exit }' "$TMP/closure-plan")
        [ -n "$row" ] || continue
        if [ "$eco" = npm ] && [ "$row" != . ]; then printf -v dirarg ' --dir %q' "$row"; fi
        case "$eco" in
            nuget) printf 'bash %q use-nuget %q "$STAMP"\n# source the printed env file, restore and build the consumer, then:\nbash %q verify-assets %q "$STAMP"%s\n' "$SELF" "${paths[0]}" "$SELF" "${paths[0]}" "$scope" ;;
            npm) printf 'bash %q use-npm %q "$STAMP"%s   # then yarn install (not --immutable)\nbash %q verify-assets %q "$STAMP" --npm%s%s\n' "$SELF" "${paths[0]}" "$dirarg" "$SELF" "${paths[0]}" "$dirarg" "$scope" ;;
            image) printf '# image edge: build the producer image per references/docker-images.md, tag it with "$STAMP"\n' ;;
        esac
    done
    printf 'bash %q restore %q\nbash %q check %q\nbash %q clean %q --stamp "$STAMP"%s   # review, then repeat with --apply\n' "$SELF" "${paths[0]}" "$SELF" "${paths[0]}" "$SELF" "${paths[0]}" "$dirarg"
}

# plan [--map FILE] CONSUMER PRODUCER...: print the plan rows (read-only).
plan_command() {
    local args=()
    while [ $# -gt 0 ]; do
        case "$1" in
            --map) [ $# -ge 2 ] || die '--map requires a file'; MAP=$2; shift 2 ;;
            --*) die "unknown plan option: $1" ;;
            *) args+=("$1"); shift ;;
        esac
    done
    [ ${#args[@]} -ge 2 ] || die 'plan requires CONSUMER and at least one PRODUCER (repository names from the map)'
    scratch
    plan_derive "$TMP/plan" "${args[@]}"
    cat "$TMP/plan"
}

# ---- Verification primitives. Each one reads only what it is passed and writes only to the paths it is given. ----
# One private scratch directory per command; stateless: nothing survives between two verify invocations.
SCR=''
fn_scratch() { scratch; SCR=$(mktemp -d "$TMP/fn.XXXXXX") || die 'cannot create a scratch directory'; }

# One line per planned artifact: "id<TAB>eco<TAB>producer". Derived from the plan rows and the map's ids rows only.
expected_artifacts() {
    local plan=$1 out=$2 kind p eco id
    : > "$out"
    while IFS=$'\t' read -r kind p eco; do
        [ "$kind" = pack ] || continue
        while IFS= read -r id; do printf '%s\t%s\t%s\n' "$id" "$eco" "$p" >> "$out"; done < <(map_ids "$p" "$eco")
    done < "$plan"
}

# The exact feed file of one artifact. Never a glob and never another stamp.
feed_path() {
    case "$2" in
        nuget) printf '%s/nuget/%s.%s.nupkg\n' "$FEED" "$3" "$1" ;;
        npm) printf '%s/npm/%s-%s.tgz\n' "$FEED" "${3//\//-}" "$1" ;;
    esac
}

# Split EXPECTED into PRESENT_OUT (same rows) and FINDINGS_OUT ("missing: ID STAMP (producer eco)").
feed_check() {
    local stamp=$1 expected=$2 present=$3 findings=$4 id eco p
    : > "$present"; : > "$findings"
    while IFS=$'\t' read -r id eco p; do
        if [ -f "$(feed_path "$stamp" "$eco" "$id")" ]; then printf '%s\t%s\t%s\n' "$id" "$eco" "$p" >> "$present"
        else printf 'missing: %s %s (%s %s)\n' "$id" "$stamp" "$p" "$eco" >> "$findings"; fi
    done < "$expected"
}

# A dependency matches only when it is exactly the stamp: STAMP, [STAMP] or [STAMP, ) (an inclusive lower bound
# with no upper bound, which is how a floor is written). Exclusive bounds, upper bounds and anything else fail.
dep_matches() {
    local range=${2//[[:space:]]/}
    [ "$range" = "$1" ] || [ "$range" = "[$1]" ] || [ "$range" = "[$1,)" ]
}

# "dep<TAB>id<TAB>version" for every <dependency> element of a nuspec, whatever the attribute order, quote
# style or line breaks; "bad<TAB>element" for one whose id or version cannot be read (the caller fails on it).
nuspec_dependencies() {
    tr '\r\n\t' '   ' < "$1" | awk '
        function attr(el, n,   v) {
            if (match(el, "[ ]" n "[ ]*=[ ]*\"[^\"]*\"")) { v = substr(el, RSTART, RLENGTH); sub(/^[^"]*"/, "", v); sub(/"$/, "", v); return v }
            if (match(el, "[ ]" n "[ ]*=[ ]*\047[^\047]*\047")) { v = substr(el, RSTART, RLENGTH); sub(/^[^\047]*\047/, "", v); sub(/\047$/, "", v); return v }
            return ""
        }
        { s = $0
          while (match(s, /<dependency([^A-Za-z]|$)/)) {
              rest = substr(s, RSTART); e = index(rest, ">")
              if (e == 0) { print "bad\t" substr(rest, 1, 80); break }
              el = substr(rest, 1, e); s = substr(rest, e + 1)
              id = attr(el, "id"); ver = attr(el, "version")
              if (id == "" || ver == "" || index(substr(el, 2), "<") > 0) print "bad\t" el
              else print "dep\t" id "\t" ver
          } }'
}

# Every nuspec dependency of a present planned .nupkg whose ID is in DEPSET must point at the stamp.
# Appends to FINDINGS_OUT and sets NUSPEC_EDGES to the number of dependency edges checked.
NUSPEC_EDGES=0
nuspec_findings() {
    local stamp=$1 depset=$2 present=$3 findings=$4 id eco p kind dep range nuspec
    NUSPEC_EDGES=0
    nuspec=$findings.nuspec
    while IFS=$'\t' read -r id eco p; do
        [ "$eco" = nuget ] || continue
        if ! unzip -p "$(feed_path "$stamp" nuget "$id")" '*.nuspec' > "$nuspec" 2>/dev/null; then
            printf 'unreadable nuspec: %s\n' "$id" >> "$findings"; continue
        fi
        while IFS=$'\t' read -r kind dep range; do
            if [ "$kind" = bad ]; then printf 'unparseable dependency: %s: %s\n' "$id" "$dep" >> "$findings"; continue; fi
            grep -Fxiq -- "$dep" "$depset" || continue
            NUSPEC_EDGES=$((NUSPEC_EDGES + 1))
            dep_matches "$stamp" "$range" || printf 'dependency: %s -> %s %s (expected %s)\n' "$id" "$dep" "$range" "$stamp" >> "$findings"
        done < <(nuspec_dependencies "$nuspec")
    done < "$present"
    rm -f -- "$nuspec"
}

# "id<TAB>version<TAB>assets file" for every package in IDSFILE that the consumer's restore resolved.
# Library keys are "Id/Version"; project references are not packages. Exit 2 without an assets file.
nuget_resolutions() {
    local consumer=$1 idsfile=$2 out=$3 path files=()
    while IFS= read -r -d '' path; do files+=("$path"); done < <(find "$consumer" -name node_modules -prune -o -path '*/obj/project.assets.json' -type f -print0)
    [ ${#files[@]} -gt 0 ] || { printf 'ERROR: no obj/project.assets.json under %s (restore the consumer first)\n' "$consumer" >&2; return 2; }
    need node
    node - "$idsfile" "${files[@]}" > "$out" <<'NODE' || { printf 'ERROR: cannot read the consumer assets\n' >&2; return 2; }
const fs = require('fs');
const [idsfile, ...files] = process.argv.slice(2);
const ids = new Set(fs.readFileSync(idsfile, 'utf8').split('\n').filter(Boolean).map(s => s.toLowerCase()));
for (const file of files) {
    let json;
    try { json = JSON.parse(fs.readFileSync(file, 'utf8')); } catch (e) { console.error('invalid JSON: ' + file); process.exit(1); }
    if (!json || typeof json.libraries !== 'object' || json.libraries === null) { console.error('no libraries object: ' + file); process.exit(1); }
    for (const [key, lib] of Object.entries(json.libraries)) {
        const cut = key.indexOf('/');
        if (cut < 1 || cut === key.length - 1 || !lib || typeof lib.type !== 'string') { console.error('unreadable library ' + key + ': ' + file); process.exit(1); }
        if (lib.type !== 'project' && ids.has(key.slice(0, cut).toLowerCase())) console.log(key.slice(0, cut) + '\t' + key.slice(cut + 1) + '\t' + file);
    }
}
NODE
}

# "name<TAB>version<TAB>package.json" for every installed copy of a package in IDSFILE under NM.
npm_installed() {
    local nm=$1 idsfile=$2 out=$3 name file version
    need node
    [ -d "$nm" ] || { printf 'ERROR: no node_modules: %s (run yarn install first)\n' "$nm" >&2; return 2; }
    : > "$out"
    while IFS= read -r name; do
        while IFS= read -r -d '' file; do
            version=$(node -e 'const v = JSON.parse(require("fs").readFileSync(process.argv[1], "utf8")).version; if (typeof v !== "string" || v === "") process.exit(1); process.stdout.write(v)' "$file") \
                || { printf 'ERROR: unreadable package.json (no version): %s\n' "$file" >&2; return 2; }
            printf '%s\t%s\t%s\n' "$name" "$version" "$file" >> "$out"
        done < <(find "$nm" -path "*/node_modules/$name/package.json" -type f -print0)
    done < "$idsfile"
}

# Every producer the consumer uses in ECO must have at least one of its planned IDs resolved.
# A consumer may use only part of a producer, so one ID is enough.
consumption_findings() {
    local plan=$1 eco=$2 resolved=$3 findings=$4 kind p e manifest ids
    while IFS=$'\t' read -r kind p e manifest; do
        [ "$kind" = consume ] && [ "$e" = "$eco" ] || continue
        ids=$(map_ids "$p" "$eco") || ids=''
        if ! awk -F'\t' 'NR == FNR { want[tolower($0)] = 1; next } tolower($1) in want { found = 1 } END { exit !found }' <(printf '%s\n' "$ids") "$resolved"; then
            printf 'unconsumed: %s (%s): none of %s resolved in the consumer\n' "$p" "$eco" "$(printf '%s' "$ids" | tr '\n' ',' | sed 's/,$//; s/,/, /g')" >> "$findings"
        fi
    done < "$plan"
}

# One JSON line from tab-separated "id<TAB>version" rows in $2, for machine consumers (pi-train).
json_report() {
    awk -F'\t' -v stamp="$1" 'BEGIN { printf "{\"stamp\":\"%s\",\"packages\":[", stamp }
        { printf "%s{\"id\":\"%s\",\"version\":\"%s\"}", (NR > 1 ? "," : ""), $1, $2 }
        END { print "]}" }' "$2"
}

# Print findings to stdout, or stderr when stdout carries JSON, then fail. Returns only when there are none.
report_findings() {
    local command=$1 findings=$2 out=$3 count
    count=$(wc -l < "$findings" | tr -d ' ')
    [ "$count" -eq 0 ] && return 0
    cat "$findings" >&"$out"
    failed "$command: $count findings"
}

# verify-pack STAMP (--consumer C --producer P... | NAME... [--ecosystem E]) [--json] [--map FILE]
# Plan mode checks exactly the plan the closure printed. Name mode (manual, weaker) builds the plan rows from
# the named repositories' ids rows instead of from edges.
verify_pack() {
    case " $* " in *' --composition '*) composition verify-pack "$@"; return ;; esac
    [ $# -ge 2 ] || die 'verify-pack requires STAMP and either --consumer C --producer P... or repository NAMEs'
    local stamp=$1 json=0 out=1 ecosystem='' consumer='' name eco d found
    local producers=() names=()
    shift
    while [ $# -gt 0 ]; do
        case "$1" in
            --json) json=1; out=2; shift ;;
            --map) [ $# -ge 2 ] || die '--map requires a file'; MAP=$2; shift 2 ;;
            --consumer) [ $# -ge 2 ] || die '--consumer requires a repository name'; consumer=$2; shift 2 ;;
            --producer) [ $# -ge 2 ] || die '--producer requires a repository name'; producers+=("$2"); shift 2 ;;
            --ecosystem) [ $# -ge 2 ] || die '--ecosystem requires nuget or npm'; ecosystem=$2; shift 2
                case "$ecosystem" in nuget|npm) ;; *) die 'invalid ecosystem' ;; esac ;;
            --*) die "unknown verify-pack option: $1" ;;
            *) names+=("$1"); shift ;;
        esac
    done
    valid_stamp "$stamp"; fn_scratch; d=$SCR
    if [ -n "$consumer" ] || [ ${#producers[@]} -gt 0 ]; then
        [ -n "$consumer" ] && [ ${#producers[@]} -gt 0 ] || die 'verify-pack plan mode needs both --consumer and --producer'
        [ ${#names[@]} -eq 0 ] && [ -z "$ecosystem" ] || die 'verify-pack takes either --consumer/--producer or repository names, not both'
        plan_derive "$d/plan" "$consumer" "${producers[@]}"
    else
        [ ${#names[@]} -ge 1 ] || die 'verify-pack requires --consumer/--producer or at least one repository name from the map'
        map_require
        : > "$d/plan"
        for name in "${names[@]}"; do
            map_known "$name" || die "repository not in the dependency map: $name"
            found=0
            for eco in nuget npm; do
                [ -z "$ecosystem" ] || [ "$eco" = "$ecosystem" ] || continue
                if map_ids "$name" "$eco" > /dev/null; then printf 'pack\t%s\t%s\n' "$name" "$eco" >> "$d/plan"; found=1; fi
            done
            [ "$found" -eq 1 ] || die "no ids row for $name (${ecosystem:-all ecosystems}) in the map"
        done
    fi
    grep -q '^pack' "$d/plan" || die 'the plan has no pack rows (image-only edges): nothing to verify'
    expected_artifacts "$d/plan" "$d/expected"
    awk -F'\t' '$2 == "nuget" { print $1 }' "$d/expected" > "$d/depset"
    [ ! -s "$d/depset" ] || need unzip
    feed_check "$stamp" "$d/expected" "$d/present" "$d/findings"
    nuspec_findings "$stamp" "$d/depset" "$d/present" "$d/findings"
    report_findings verify-pack "$d/findings" "$out"
    if [ "$json" -eq 1 ]; then
        awk -F'\t' -v s="$stamp" '{ print $1 "\t" s }' "$d/present" > "$d/json"
        json_report "$stamp" "$d/json"
        return 0
    fi
    printf 'verify-pack: %s planned artifacts present at %s (%s dependency edges checked)\n' "$(wc -l < "$d/present" | tr -d ' ')" "$stamp" "$NUSPEC_EDGES"
}

# verify-assets CONSUMER-PATH STAMP [--npm] [--dir REL] [--json] [--consumer C --producer P...] [--map FILE]
# Plan mode scans for exactly the planned IDs of one ecosystem; feed-wide mode (no --consumer) scans every
# package at the stamp in the feed, which is manual and weaker.
verify_assets() {
    case " $* " in *' --composition '*) composition verify-assets "$@"; return ;; esac
    [ $# -ge 2 ] || die 'verify-assets requires CONSUMER-PATH, STAMP, [--npm] [--dir REL]'
    local path=$1 stamp=$2 npm=0 dir='' json=0 out=1 consumer='' eco=nuget d name file matched nm unit
    local producers=()
    shift 2
    while [ $# -gt 0 ]; do
        case "$1" in
            --npm) npm=1; eco=npm; shift ;;
            --json) json=1; out=2; shift ;;
            --dir) [ $# -ge 2 ] || die '--dir requires a relative directory'; dir=${2%/}; shift 2 ;;
            --map) [ $# -ge 2 ] || die '--map requires a file'; MAP=$2; shift 2 ;;
            --consumer) [ $# -ge 2 ] || die '--consumer requires a repository name'; consumer=$2; shift 2 ;;
            --producer) [ $# -ge 2 ] || die '--producer requires a repository name'; producers+=("$2"); shift 2 ;;
            *) die "unknown verify-assets option: $1" ;;
        esac
    done
    valid_stamp "$stamp"; [ -d "$path" ] || die "not a directory: $path"
    case "$dir" in /*|..|../*|*/..|*/../*) die "--dir must be a relative path inside the consumer: $dir" ;; esac
    if [ -n "$consumer" ] || [ ${#producers[@]} -gt 0 ]; then
        [ -n "$consumer" ] && [ ${#producers[@]} -gt 0 ] || die 'verify-assets needs --consumer and --producer together'
    fi
    fn_scratch; d=$SCR
    : > "$d/findings"
    if [ -n "$consumer" ]; then
        plan_derive "$d/plan" "$consumer" "${producers[@]}"
        awk -F'\t' -v e="$eco" '$1 == "consume" && $3 == e { found = 1 } END { exit !found }' "$d/plan" || die "$consumer has no $eco edge to the named producers"
        expected_artifacts "$d/plan" "$d/all-expected"
        awk -F'\t' -v e="$eco" '$2 == e' "$d/all-expected" > "$d/expected"
        feed_check "$stamp" "$d/expected" "$d/present" "$d/findings"
        cut -f1 "$d/expected" | sort -u > "$d/ids"
    else
        : > "$d/ids"
        if [ "$npm" -eq 1 ]; then
            for file in "$FEED/npm/"*"-$stamp.tgz"; do
                [ -f "$file" ] || continue
                name=$(tar -xOf "$file" package/package.json | node -e 'const n = JSON.parse(require("fs").readFileSync(0, "utf8")).name; if (typeof n !== "string" || n === "") process.exit(1); process.stdout.write(n)') \
                    || die "cannot read the package name: $file"
                printf '%s\n' "$name" >> "$d/ids"
            done
        else
            for file in "$FEED/nuget/"*".$stamp.nupkg"; do
                [ -f "$file" ] || continue
                name=${file##*/}; printf '%s\n' "${name%."$stamp".nupkg}" >> "$d/ids"
            done
            [ -s "$d/ids" ] || die "no packages at $stamp in the feed"
        fi
    fi
    if [ "$npm" -eq 1 ]; then
        nm=$path${dir:+/$dir}/node_modules
        npm_installed "$nm" "$d/ids" "$d/resolved" || return $?
    else
        nuget_resolutions "$path" "$d/ids" "$d/resolved" || return $?
    fi
    matched=$(wc -l < "$d/resolved" | tr -d ' ')
    awk -F'\t' -v s="$stamp" '$2 != s { print $1 " " $2 " " $3 }' "$d/resolved" >> "$d/findings"
    if [ -n "$consumer" ]; then
        consumption_findings "$d/plan" "$eco" "$d/resolved" "$d/findings"
    elif [ "$matched" -eq 0 ]; then
        failed "verify-assets: no stamped $eco package is resolved in the consumer"
    fi
    report_findings verify-assets "$d/findings" "$out"
    if [ "$json" -eq 1 ]; then
        awk -F'\t' '{ print $1 "\t" $2 }' "$d/resolved" | sort -u > "$d/json"
        json_report "$stamp" "$d/json"
        return 0
    fi
    unit=resolutions
    if [ "$npm" -eq 1 ]; then unit='installed copies'; fi
    printf 'verify-assets: %s %s, all at %s\n' "$matched" "$unit" "$stamp"
}

use_npm() {
    [ $# -eq 2 ] || { [ $# -eq 4 ] && [ "$3" = --dir ]; } || die 'use-npm requires consumer, stamp and optional --dir REL'
    local repo version session backup rel='' dir
    repo=$(repo_root "$1"); version=$2
    if [ $# -eq 4 ]; then rel=${4%/}; fi
    case "$rel" in /*|..|../*|*/..|*/../*) die "--dir must be a relative path inside the consumer: $rel" ;; esac
    dir=$repo${rel:+/$rel}
    valid_stamp "$version"; need node; need tar; scratch
    session=$(session_path "$repo"); backup=$session/npm-backup
    [ ! -d "$session/composition" ] || die 'active composition; restore first'
    [ -f "$dir/package.json" ] || die "consumer has no package.json in ${rel:-the root}; use --dir REL for a Yarn project in a subdirectory"
    [ ! -e "$backup" ] || die "active npm backup exists: $backup; restore first"
    node - "$dir" "$FEED/npm" "$version" "$TMP/package.json" <<'NODE'
const fs = require('fs'), path = require('path'), {execFileSync} = require('child_process');
const [repo, feed, stamp, output] = process.argv.slice(2);
const root = JSON.parse(fs.readFileSync(path.join(repo, 'package.json'), 'utf8'));
let count = 0;
for (const file of fs.existsSync(feed) ? fs.readdirSync(feed) : []) {
    if (!file.endsWith('-' + stamp + '.tgz')) continue;
    const tarball = path.join(feed, file);
    // Read the packed name: slugified scoped names are not reversibly encoded.
    const {name} = JSON.parse(execFileSync('tar', ['-xOf', tarball, 'package/package.json'], {encoding: 'utf8'}));
    if (typeof name !== 'string' || !/^(@[^/\s]+\/)?[^/\s]+$/.test(name))
        throw new Error('Invalid packed package name: ' + tarball);
    root.resolutions = root.resolutions || {};
    root.resolutions[name] = 'file:' + tarball;
    count++;
}
if (!count) { console.error('ERROR: no matching npm tarballs'); process.exit(2); }
fs.writeFileSync(output, JSON.stringify(root, null, 2) + '\n');
console.log(`overrode ${count} npm packages`);
NODE
    if [ "$DRY" -eq 1 ]; then
        quote_command mkdir -p "$backup"
        quote_command cp "$TMP/package.json" "$dir/package.json"
        return 0
    fi
    register_session "$repo" "$session" "$version"
    mkdir "$backup"
    if [ -n "$rel" ]; then printf '%s\n' "$rel" > "$session/npm-dir"; fi
    cp -p "$dir/package.json" "$backup/package.json"
    if [ -f "$dir/yarn.lock" ]; then cp -p "$dir/yarn.lock" "$backup/yarn.lock"; fi
    git -C "$dir" diff HEAD -- package.json yarn.lock > "$backup/before.diff"
    cp "$TMP/package.json" "$backup/expected-package.json"
    cp "$TMP/package.json" "$dir/package.json"
    registry_consumer "$version" npm "$repo" "$session" ''
    printf 'Next: cd %q && yarn install\n' "$dir"
    printf 'Vite: clear stale prebundles with rm -rf node_modules/.vite\n'
}

restore() {
    [ $# -eq 1 ] || die 'restore requires a consumer'
    local repo session backup dir stamp
    local restore_stamps=()
    repo=$(repo_root "$1"); session=$(session_path "$repo"); backup=$session/npm-backup
    validate_session "$session"
    scratch
    # Restoring the overlay never depends on node; only the registry bookkeeping does.
    local registry_node=0
    if [ "$DRY" -eq 0 ] && command -v node > /dev/null 2>&1; then
        registry_node=1
        registry restore-stamps "$session" > "$TMP/restore-stamps"
        while IFS= read -r stamp; do restore_stamps+=("$stamp"); done < "$TMP/restore-stamps"
    elif [ "$DRY" -eq 0 ]; then
        printf 'WARNING: node not found; restoring without updating the stamp registry\n' >&2
    fi
    if [ -d "$session/composition" ]; then
        composition restore-state "$repo"
        if [ "$registry_node" -eq 1 ]; then registry_update restore "$session" ${restore_stamps[@]+"${restore_stamps[@]}"}; fi
        if [ "$DRY" -eq 0 ]; then check "$repo"; fi
        return
    fi
    dir=$repo
    if [ -f "$session/npm-dir" ]; then dir=$repo/$(cat "$session/npm-dir"); fi
    if [ -f "$session/repo" ] && [ "$(cat "$session/repo")" != "$repo" ]; then die 'session belongs to another checkout'; fi
    if [ -d "$backup" ]; then
        if ! cmp -s "$dir/package.json" "$backup/expected-package.json" ||
            { [ -f "$backup/yarn.lock" ] && ! cmp -s "$dir/yarn.lock" "$backup/yarn.lock"; } ||
            { [ ! -f "$backup/yarn.lock" ] && [ -e "$dir/yarn.lock" ]; }; then
            need node
            node "$(dirname "$SELF")/npm-restore-check.cjs" "$dir" "$backup" ||
                die "npm files changed beyond the overlay; review $backup (nothing restored)"
        fi
        mutate cp -p "$backup/package.json" "$dir/package.json"
        if [ -f "$backup/yarn.lock" ]; then mutate cp -p "$backup/yarn.lock" "$dir/yarn.lock"; else mutate rm -f -- "$dir/yarn.lock"; fi
        mutate rm -rf -- "$backup"
    fi
    mutate rm -f -- "$session/Directory.Packages.props" "$session/cratis-local.targets" "$session/nuget.config" "$session/env" "$session/stamp" "$session/npm-dir"
    if [ "$registry_node" -eq 1 ]; then registry_update restore "$session" ${restore_stamps[@]+"${restore_stamps[@]}"}; fi
    if [ "$DRY" -eq 0 ]; then check "$repo"; fi
}

# Check only changed lines, not unchanged context or removed local overrides.
# NUL-delimited Git paths preserve spaces; diagnostics are relative to the root.
check() {
    [ $# -ge 1 ] && [ $# -le 2 ] || die 'check requires repo and optional --staged'
    local repo path count=0 hits=0 staged=0
    repo=$(repo_root "$1"); shift
    if [ $# -gt 0 ]; then [ "$1" = --staged ] || die "unknown check option: $1"; staged=1; fi
    scratch
    if [ "$staged" -eq 1 ]; then
        git -C "$repo" diff --cached --name-only -z --diff-filter=ACMRT > "$TMP/tracked"
    else
        git -C "$repo" diff HEAD --name-only -z --diff-filter=ACMRT > "$TMP/tracked"
    fi
    git -C "$repo" ls-files --others --exclude-standard -z > "$TMP/untracked"
    while IFS= read -r -d '' path; do
        count=$((count + 1))
        if [ "$staged" -eq 1 ]; then
            git -C "$repo" diff --cached --no-ext-diff --no-textconv --unified=0 -- "$path" > "$TMP/diff"
        else
            git -C "$repo" diff HEAD --no-ext-diff --no-textconv --unified=0 -- "$path" > "$TMP/diff"
        fi
        awk '/^@@ / { split($3,a,","); n=substr(a[1],2); next } /^\+\+\+/ { next } /^\+/ { print n ":" substr($0,2); n++; next } /^ / { n++ }' "$TMP/diff" > "$TMP/lines"
        scan_lines "$path" "$TMP/lines" > "$TMP/hits"
        if [ -s "$TMP/hits" ]; then cat "$TMP/hits"; hits=$((hits + 1)); fi
    done < "$TMP/tracked"
    while IFS= read -r -d '' path; do
        [ ! -L "$repo/$path" ] || die "cannot scan untracked symlink without following outside repo: $path"
        [ -f "$repo/$path" ] || continue
        count=$((count + 1))
        awk '{print NR ":" $0}' "$repo/$path" > "$TMP/lines"
        scan_lines "$path" "$TMP/lines" > "$TMP/hits"
        if [ -s "$TMP/hits" ]; then cat "$TMP/hits"; hits=$((hits + 1)); fi
    done < "$TMP/untracked"
    printf 'checked %s files, %s leaks\n' "$count" "$hits"
    [ "$hits" -eq 0 ]
}
scan_lines() {
    local path=$1 input=$2
    awk -v path="$path" -v feed="$FEED" -v alias="$FEED_ALIAS" '
        function hit(pattern) { print path ":" number ":" pattern }
        {
            number=$0; sub(/:.*/,"",number); line=$0; sub(/^[^:]*:/,"",line)
            if (line ~ /-local\.[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]\.g[0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f]/) hit("local stamp")
            if (line ~ /-train\.g[0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f]/) hit("train stamp")
            if (index(line,feed) || (alias != feed && index(line,alias))) hit("local feed path")
            if (index(line,"DirectoryPackages" "PropsPath")) hit("CPM overlay property")
            if (index(line,"RestoreAdditional" "ProjectSources")) hit("additional restore sources")
            if (index(line,"CustomAfterMicrosoftCommon" "Targets")) hit("local targets property")
            if (index(line,"_CratisLocalPreviousAfterMicrosoftCommon" "Targets")) hit("previous targets property")
            if (line ~ /<PackageVersion[[:space:]]+Update[[:space:]]*=/) hit("package version override")
            if (path ~ /(^|\/)(package.json|yarn.lock)$/ && line ~ /(file|portal|link):/ &&
                (line ~ /\.tgz/ || line ~ /(file|portal|link):(\/|\.\.?\/|~\/|[A-Za-z]:)/)) hit("local npm reference")
        }
    ' "$input"
}

# Portable deadline for a read-only Docker probe; no dependency on GNU timeout.
bounded_docker() {
    local pid second status=0
    docker "$@" > "$TMP/docker-output" 2> "$TMP/docker-error" & pid=$!
    for ((second=0; second<10; second++)); do
        if ! kill -0 "$pid" 2>/dev/null; then break; fi
        sleep 1
    done
    if kill -0 "$pid" 2>/dev/null; then
        kill -TERM "$pid" 2>/dev/null || :
        kill -KILL "$pid" 2>/dev/null || :
        wait "$pid" 2>/dev/null || :
        return 1
    fi
    wait "$pid" || status=$?
    return "$status"
}
local_artifact() {
    [[ "$1" =~ -(local\.[0-9]{12}|train)\.g[0-9a-f]{7}($|[^0-9A-Za-z]) ]]
}
# Print stamp<TAB>file. Follow manifest symlinks; traversal/read errors refuse cleanup.
live_stamps() {
    local repo session file found stamp status
    for repo in "$@"; do
        session=${LIVE_SESSION:-$(session_path "$repo")}
        find -L "$repo" \( -name .git -o -name node_modules \) -prune -o \( -iname nuget.config -o -iname Directory.Packages.props -o -name packages.lock.json -o -name package.json -o -name yarn.lock \) \( -type f -o -type l \) -print0 > "$TMP/live-files" || die "cannot inspect registered consumer: $repo"
        for file in "$session/Directory.Packages.props" "$session/cratis-local.targets" "$session/nuget.config"; do
            if [ -e "$file" ]; then printf '%s\0' "$file" >> "$TMP/live-files"; fi
        done
        while IFS= read -r -d '' file; do
            [ -f "$file" ] && [ -r "$file" ] || die "cannot read live manifest: $file"
            status=0
            found=$(grep -Eio '[0-9]+\.[0-9]+\.[0-9]+-(local\.[0-9]{12}|train)\.g[0-9a-f]{7}' "$file") || status=$?
            [ "$status" -le 1 ] || die "cannot inspect live manifest: $file"
            [ -n "$found" ] || continue
            while IFS= read -r stamp; do
                stamp=$(printf '%s' "$stamp" | tr '[:upper:]' '[:lower:]')
                printf '%s\t%s\n' "$stamp" "$file"
            done <<< "$found"
        done < "$TMP/live-files"
    done
}
# Legacy clean still refuses the whole request, with its original messages.
clean_live_references() {
    local selected=$1 session repo file stamp blocked=0 session_selected
    while IFS= read -r session; do
        [ -d "$session" ] && [ -f "$session/repo" ] || continue
        validate_session "$session"
        repo=$(cat "$session/repo")
        [ -d "$repo" ] || continue
        [ "$(repo_root "$repo")" = "$repo" ] || die "registered consumer no longer matches repository: $repo"
        session_selected=0
        if [ -f "$session/stamp" ] && [ "$(cat "$session/stamp")" = "$selected" ]; then session_selected=1; fi
        LIVE_SESSION=$session live_stamps "$repo" > "$TMP/legacy-live"
        while IFS=$'\t' read -r stamp file; do
            if [ -z "$selected" ] || [ "$selected" = all ] || [ "$session_selected" -eq 1 ] || [ "$stamp" = "$selected" ]; then
                printf 'kept stamp %s: registered live reference in %s\n' "$stamp" "$file"
                blocked=1
            fi
        done < "$TMP/legacy-live"
    done < "$TMP/unique-sessions"
    [ "$blocked" -eq 0 ] || die 'live references retained; restore registered consumers before cleaning (no subjects removed)'
}
# The registry supplies recorded consumers as well as registered sessions. Both list
# and expired clean use exactly this offline context and the same decision function.
expiry_context() {
    local repo session cache actual
    scratch
    registry expiry-inputs "$@" > "$TMP/expiry-inputs"
    if [ -n "${EXPIRY_CONSUMER:-}" ]; then printf '%s\t%s\n' "$EXPIRY_CONSUMER" "$(session_path "$EXPIRY_CONSUMER")" >> "$TMP/expiry-inputs"; fi
    : > "$TMP/expiry-live"; : > "$TMP/expiry-compositions"
    printf '%s\n' "$NUGET_CACHE" "$HOME/.nuget/packages" > "$TMP/expiry-caches"
    while IFS= read -r actual; do
        session=${actual#*$'\t'}; repo=${actual%%$'\t'*}
        validate_session "$session"
        if [ -e "$session/nuget-cache" ] || [ -L "$session/nuget-cache" ]; then
            cache=$(registered_private_cache "$session")
            printf '%s\n' "$cache" >> "$TMP/expiry-caches"
        fi
        if [ -e "$session/composition" ] || [ -L "$session/composition" ]; then
            composition clean-members all "$session" >> "$TMP/expiry-compositions"
        fi
        [ -n "$repo" ] && [ -d "$repo" ] || continue
        actual=$(git -C "$repo" rev-parse --show-toplevel 2>/dev/null) || continue
        [ "$actual" = "$repo" ] || continue
        LIVE_SESSION=$session live_stamps "$repo" >> "$TMP/expiry-live"
    done < "$TMP/expiry-inputs"
}
expired_guard() {
    local stamp=$1 found file
    expiry_context expiry
    if grep -Fxq -- "$stamp" "$TMP/expiry-compositions"; then die "expired stamp changed: active composition member: $stamp"; fi
    while IFS=$'\t' read -r found file; do
        [ "$found" != "$stamp" ] || die "expired stamp changed: live reference in $file"
    done < "$TMP/expiry-live"
}
subject_stamp() {
    local name=${1##*/}
    if [[ "$name" =~ ([0-9]+\.[0-9]+\.[0-9]+-(local\.[0-9]{12}|train)\.g[0-9a-f]{7}) ]]; then printf '%s\n' "${BASH_REMATCH[1]}"; fi
}
clean() {
    local version='' apply=0 consumer='' arg path cache image session maximum count=0 rel='' private=0 whole_cache='' whole_session=''
    local expired=0 online=0 selected_stamp decision reason EXPIRY_CONSUMER=''
    local subjects=() kinds=() caches=("$NUGET_CACHE") cache_sessions=() filtered_subjects=() filtered_kinds=()
    while [ $# -gt 0 ]; do
        arg=$1; shift
        case "$arg" in
            --stamp) [ $# -gt 0 ] || die '--stamp requires a stamp'; [ -z "$version" ] || die 'choose --stamp or --all'; version=$1; shift; valid_stamp "$version" ;;
            --all) [ -z "$version" ] || die 'choose --stamp or --all'; version=all ;;
            --expired) expired=1 ;;
            --online) online=1 ;;
            --apply) apply=1 ;;
            --private-cache) private=1 ;;
            --dir) [ $# -gt 0 ] || die '--dir requires a relative directory'; rel=${1%/}; shift ;;
            -*) die "unknown clean option: $arg" ;;
            *) [ -z "$consumer" ] || die 'clean takes at most one consumer'; consumer=$(repo_root "$arg") ;;
        esac
    done
    [ "$expired" -eq 0 ] || [ -z "$version" ] || die '--expired cannot be combined with --stamp or --all'
    [ "$online" -eq 0 ] || [ "$expired" -eq 1 ] || die '--online requires --expired'
    [ "$expired" -eq 0 ] || [ "$private" -eq 0 ] || die '--expired cannot remove a whole private cache'
    local registered
    if [ "$expired" -eq 0 ] && [ -n "$consumer" ]; then
        session=$(session_path "$consumer")
        if [ -e "$session/composition" ] || [ -L "$session/composition" ]; then composition clean-guard "${version:-all}" "$session"; fi
    fi
    if [ "$expired" -eq 0 ] && [ -f "$FEED/sessions" ]; then
        while IFS= read -r registered; do
            if [ -e "$registered/composition" ] || [ -L "$registered/composition" ]; then
                composition clean-guard "${version:-all}"
                break
            fi
        done < "$FEED/sessions"
    fi
    maximum=${CRATIS_LOCAL_CLEAN_MAX:-500}
    [[ "$maximum" =~ ^[0-9]+$ ]] || die 'CRATIS_LOCAL_CLEAN_MAX must be a whole number'
    [ ${#maximum} -le 6 ] || die 'CRATIS_LOCAL_CLEAN_MAX is too large'
    maximum=$((10#$maximum))
    scratch
    if [ -f "$FEED/sessions" ]; then cat "$FEED/sessions" > "$TMP/sessions"; else : > "$TMP/sessions"; fi
    if [ -n "$consumer" ]; then session_path "$consumer" >> "$TMP/sessions"; fi
    sort -u "$TMP/sessions" > "$TMP/unique-sessions"
    if [ "$expired" -eq 1 ]; then
        EXPIRY_CONSUMER=$consumer
        if [ "$online" -eq 1 ]; then registry expiry --online > "$TMP/expiry-decisions"; else registry expiry > "$TMP/expiry-decisions"; fi
        : > "$TMP/expired-stamps"
        while IFS=$'\t' read -r decision selected_stamp reason; do
            printf '%s %s: %s\n' "$decision" "$selected_stamp" "$reason"
            if [ "$decision" = expired ]; then printf '%s\n' "$selected_stamp" >> "$TMP/expired-stamps"; fi
        done < "$TMP/expiry-decisions"
        while IFS= read -r path; do printf '%s\n' "${path#*$'\t'}" >> "$TMP/sessions"; done < "$TMP/expiry-inputs"
        sort -u "$TMP/sessions" > "$TMP/unique-sessions"
        caches+=("$HOME/.nuget/packages")
    fi
    while IFS= read -r session; do
        if [ -e "$session/nuget-cache" ] || [ -L "$session/nuget-cache" ]; then
            cache=$(registered_private_cache "$session")
            cache_sessions+=("$session"); caches+=("$cache")
        fi
    done < "$TMP/unique-sessions"
    if [ "$private" -eq 1 ]; then
        [ -n "$consumer" ] || die 'clean --private-cache requires a consumer'
        whole_session=$(session_path "$consumer")
        [ -f "$whole_session/nuget-cache" ] || die "no registered private cache: $consumer"
        whole_cache=$(registered_private_cache "$whole_session")
        private_cache_restored "$whole_session"
        clean_live_references ''
        subjects+=("$whole_cache"); kinds+=(private-cache)
    fi
    printf '%s\n' "${caches[@]}" | sort -u > "$TMP/nuget-caches"
    for cache in "$FEED/nuget" "$FEED/npm"; do
        [ -d "$cache" ] || continue
        while IFS= read -r -d '' path; do
            local_artifact "${path##*/}" || continue
            if [ -n "$version" ] && [ "$version" != all ]; then case "$path" in *"$version"*) ;; *) continue ;; esac; fi
            subjects+=("$path"); kinds+=(file)
        done < <(find "$cache" -type f \( -name '*.nupkg' -o -name '*.snupkg' -o -name '*.tgz' \) -print0)
    done
    while IFS= read -r path; do
        [ "$path" != "$whole_cache" ] || continue
        [ -d "$path" ] || continue
        for cache in "$path"/*; do
            [ -d "$cache" ] && [ ! -L "$cache" ] || continue
            for path in "$cache"/*-local.* "$cache"/*-train.*; do
                [ -d "$path" ] && [ ! -L "$path" ] || continue
                local_artifact "${path##*/}" || continue
                if [ -n "$version" ] && [ "$version" != all ] && [ "${path##*/}" != "$version" ]; then continue; fi
                subjects+=("$path"); kinds+=(directory)
            done
        done
    done < "$TMP/nuget-caches"
    if [ -n "$consumer" ] && [ ! -f "$consumer${rel:+/$rel}/package.json" ]; then
        printf 'SKIP: no package.json in %s; pass --dir REL for a Yarn project in a subdirectory\n' "$consumer${rel:+/$rel}" >&2
    elif command -v yarn >/dev/null 2>&1 && [ -n "$consumer" ]; then
        cache=$(cd "$consumer${rel:+/$rel}" && yarn config get cacheFolder) || die 'cannot discover Yarn cache'
        if [ -d "$cache" ]; then
            while IFS= read -r -d '' path; do
                local_artifact "${path##*/}" || continue
                if [ -n "$version" ] && [ "$version" != all ]; then case "$path" in *"$version"*) ;; *) continue ;; esac; fi
                subjects+=("$path"); kinds+=(file)
            done < <(find "$cache" -type f \( -name '*-local.*' -o -name '*-train.*' \) -print0)
        fi
    else
        printf 'SKIP: Yarn cache discovery needs yarn and an explicit consumer\n' >&2
    fi
    if command -v docker >/dev/null 2>&1; then
        if bounded_docker image ls --format '{{.Repository}}:{{.Tag}}'; then
            while IFS= read -r image; do
                local_artifact "${image##*:}" || continue
                if [ -n "$version" ] && [ "$version" != all ]; then case "${image##*:}" in *"$version"*) ;; *) continue ;; esac; fi
                subjects+=("$image"); kinds+=(image)
            done < "$TMP/docker-output"
        else
            printf 'SKIP: Docker unavailable or did not respond within 10 seconds\n' >&2
        fi
    else printf 'SKIP: docker is not on PATH\n' >&2; fi
    while IFS= read -r session; do
        [ -d "$session" ] && [ -f "$session/repo" ] && [ -f "$session/stamp" ] || continue
        local_artifact "$(cat "$session/stamp")" || continue
        if [ "$expired" -eq 1 ] && ! grep -Fxq -- "$(cat "$session/stamp")" "$TMP/expired-stamps"; then continue; fi
        if [ -n "$version" ] && [ "$version" != all ] && [ "$(cat "$session/stamp")" != "$version" ]; then continue; fi
        [ ! -L "$session" ] && [ "$session" != / ] || die "unsafe session: $session"
        validate_session "$session"
        path=$(cat "$session/repo")
        [ "$(session_path "$path")" = "$session" ] || die "session directory no longer matches repository: $session"
        [ ! -d "$session/npm-backup" ] || die "unresolved npm backup: $session; restore before cleaning"
        subjects+=("$session"); kinds+=(session)
    done < "$TMP/unique-sessions"
    # Stamp records exist only after a registry-aware pack or use; without any, clean needs no node.
    : > "$TMP/stamp-records"
    if [ -e "$FEED/stamps" ] || [ -L "$FEED/stamps" ]; then registry clean "$version" > "$TMP/stamp-records"; fi
    while IFS= read -r path; do subjects+=("$path"); kinds+=(stamp-record); done < "$TMP/stamp-records"
    if [ "$expired" -eq 1 ]; then
        for ((arg=0; arg<${#subjects[@]}; arg++)); do
            if [ "${kinds[$arg]}" = session ]; then selected_stamp=$(cat "${subjects[$arg]}/stamp"); else selected_stamp=$(subject_stamp "${subjects[$arg]}"); fi
            if grep -Fxq -- "$selected_stamp" "$TMP/expired-stamps"; then
                filtered_subjects+=("${subjects[$arg]}"); filtered_kinds+=("${kinds[$arg]}")
            fi
        done
        subjects=(${filtered_subjects[@]+"${filtered_subjects[@]}"}); kinds=(${filtered_kinds[@]+"${filtered_kinds[@]}"})
    else clean_live_references "$version"; fi
    count=${#subjects[@]}
    [ "$count" -le "$maximum" ] || die "clean fuse: $count subjects exceeds $maximum"
    for ((arg=0; arg<count; arg++)); do printf '%s: %s\n' "${kinds[$arg]}" "${subjects[$arg]}"; done
    if [ "$apply" -eq 1 ] && [ "$DRY" -eq 0 ]; then
        for ((arg=0; arg<count; arg++)); do
            if [ "$expired" -eq 1 ]; then
                if [ "${kinds[$arg]}" = session ]; then selected_stamp=$(cat "${subjects[$arg]}/stamp"); else selected_stamp=$(subject_stamp "${subjects[$arg]}"); fi
                expired_guard "$selected_stamp"
            else clean_live_references "$version"; fi
            for session in ${cache_sessions[@]+"${cache_sessions[@]}"}; do registered_private_cache "$session" > /dev/null; done
            path=${subjects[$arg]}
            case "${kinds[$arg]}" in
                private-cache)
                    [ "$(registered_private_cache "$whole_session")" = "$path" ] || die "private cache changed: $path"
                    private_cache_restored "$whole_session"
                    clean_live_references ''
                    rm -rf -- "$path"
                    rm -- "$whole_session/nuget-cache"
                    # Later subjects still validate all remaining registrations.
                    cache_sessions=()
                    while IFS= read -r session; do
                        if [ -f "$session/nuget-cache" ]; then cache_sessions+=("$session"); fi
                    done < "$TMP/unique-sessions"
                    ;;
                stamp-record) registry_update remove "${path##*/}" ;;
                image) docker rmi "$path" ;;
                file) [ -f "$path" ] && [ ! -L "$path" ] || die "clean subject changed: $path"; rm -- "$path" ;;
                directory) [ -d "$path" ] && [ ! -L "$path" ] || die "clean subject changed: $path"; rm -rf -- "$path" ;;
                session)
                    [ -f "$path/stamp" ] && [ ! -L "$path" ] && [ ! -e "$path/npm-backup" ] || die "session changed: $path"
                    validate_session "$path"
                    rm -f -- "$path/stamp" "$path/env" "$path/npm-dir" "$path/Directory.Packages.props" "$path/cratis-local.targets" "$path/nuget.config"
                    if [ ! -f "$path/nuget-cache" ]; then rm -f -- "$path/repo"; rmdir -- "$path"; fi ;;
            esac
        done
        if [ "$expired" -eq 1 ]; then registry_update prune; fi
        printf 'removed %s subjects\n' "$count"
    else printf 'dry-run: %s subjects (use --apply to remove)\n' "$count"; fi
}

# shellcheck disable=SC2016  # fixtures assert literal $STAMP in printed commands
self_test_nuget_overlay() {
    local output=$1 id=$2 version=$3
    local cpm_on="'\$(ManagePackageVersionsCentrally)' == 'true'" cpm_off="'\$(ManagePackageVersionsCentrally)' != 'true'"
    grep -Fq "GetPathOfFileAbove('Directory.Packages.props', '\$(MSBuildProjectDirectory)')" "$output/Directory.Packages.props" || failed 'self-test: nearest CPM import'
    grep -Fq 'Import Project="$(_CratisLocalPackagesProps)" Condition="' "$output/Directory.Packages.props" || failed 'self-test: conditional nearest import'
    grep -Fq "ItemGroup Condition=\"$cpm_on\"" "$output/Directory.Packages.props" || failed 'self-test: CPM-only versions'
    grep -Fq "PropertyGroup Condition=\"$cpm_on\"" "$output/Directory.Packages.props" || failed 'self-test: CPM-only transitive pinning'
    grep -Fq "ItemGroup Condition=\"$cpm_off\"" "$output/cratis-local.targets" || failed 'self-test: opt-out-only references'
    grep -Fq "PackageReference Update=\"$id\" Version=\"$version\"" "$output/cratis-local.targets" || failed 'self-test: opt-out version override'
    grep -Fq 'Import Project="$(_CratisLocalPreviousAfterMicrosoftCommonTargets)"' "$output/cratis-local.targets" || failed 'self-test: chained targets import'
    grep -Fq "!= '\$(MSBuildThisFileFullPath)' and Exists('\$(_CratisLocalPreviousAfterMicrosoftCommonTargets)')" "$output/cratis-local.targets" || failed 'self-test: chained targets import is guarded'
    env -i PATH=/usr/bin:/bin CustomAfterMicrosoftCommonTargets='/previous targets/file.targets' /bin/bash -c '
        source "$1"; source "$1"
        [ "$_CratisLocalPreviousAfterMicrosoftCommonTargets" = "/previous targets/file.targets" ] &&
        [ "$CustomAfterMicrosoftCommonTargets" = "$2/cratis-local.targets" ]
    ' _ "$output/env" "$output" || failed 'self-test: source twice preserves previous targets'
}

self_test() {
    [ $# -eq 0 ] || die '--self-test takes no arguments'
    local root home repo feed version output status passed=0 skipped=0 test_key arg map system_path
    # Fixtures use the physical path (macOS TMPDIR is /var -> /private/var) because the helper
    # reports repositories by their git toplevel; symlinked feeds have their own test below.
    scratch; root=$(cd -P "$TMP" && pwd -P)/self-test; mkdir -p "$root/home" "$root/repo"
    map=$root/map.tsv; home=$root/home; repo=$root/repo; feed=$home/.cache/cratis/local-feed
    # Isolate tool discovery, adding only node for the stamp registry. npm tests
    # inherit PATH. Both isolate HOME/config and omit the host adapter.
    need node; mkdir -p "$root/registry-bin"
    ln -s "$(command -v node)" "$root/registry-bin/node"
    system_path=$root/registry-bin:/usr/bin:/bin:/usr/sbin:/sbin
    st() { env -i HOME="$home" PATH="$system_path" TMPDIR="$TMP" /bin/bash "$SELF" "$@"; }
    stm() { env -i HOME="$home" PATH="$system_path" TMPDIR="$TMP" CRATIS_LOCAL_MAP="$map" /bin/bash "$SELF" "$@"; }
    has() { grep -Fq -- "$1" "$2"; }
    st_npm() { env -i HOME="$home" PATH="$PATH" TMPDIR="$TMP" /bin/bash "$SELF" "$@"; }
    git -C "$repo" init -q
    repo=$(repo_root "$repo")
    git -C "$repo" config user.email test@example.invalid
    git -C "$repo" config user.name 'Local package self-test'
    printf '<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup><PackageVersion Include="Cratis.Test" Version="1.2.3" /></ItemGroup></Project>\n' > "$repo/Directory.Packages.props"
    mkdir -p "$repo/nested"
    printf '<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>\n' > "$repo/nested/Directory.Packages.props"
    printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Cratis.Test" Version="1.2.3" /></ItemGroup></Project>\n' > "$repo/nested/OptOut.csproj"
    printf '{"private":true,"dependencies":{"@cratis/test":"1.2.3"}}\n' > "$repo/package.json"
    cat > "$repo/yarn.lock" <<'LOCK'
# This file is generated by running "yarn install" inside your project.

__metadata:
  version: 8
  cacheKey: 10c0

"@cratis/test@npm:1.2.3":
  version: 1.2.3
  resolution: "@cratis/test@npm:1.2.3"
  checksum: original
  languageName: node
  linkType: hard

"unrelated@npm:1.0.0":
  version: 1.0.0
  resolution: "unrelated@npm:1.0.0"
  checksum: keep
  languageName: node
  linkType: hard
LOCK
    printf 'clean\n' > "$repo/probe"
    git -C "$repo" add .; git -C "$repo" commit -qm initial; git -C "$repo" tag v1.2.3
    status=0; st > "$root/output" || status=$?
    if [ "$status" -ne 2 ] || ! grep -q '^Usage:' "$root/output"; then
        failed 'self-test: no arguments must show usage and exit 2'
    fi
    passed=$((passed + 1))
    version=$(st stamp "$repo")
    [[ "$version" =~ ^1\.2\.4-local\.[0-9]{12}\.g[0-9a-f]{7}$ ]] || failed 'self-test: stamp format'
    passed=$((passed + 1))
    self_test_base_version
    mkdir -p "$feed/nuget" "$feed/npm"
    : > "$feed/nuget/Cratis.Test.1.2.4-local.200001010000.g1234567.nupkg"
    st --dry-run use-nuget "$repo" 1.2.4-local.200001010000.g1234567 Cratis.Test > "$root/output"
    status=0; st --dry-run use-nuget "$repo" 1.2.4-local.200001010000.1234567 Cratis.Test > "$root/output" 2>&1 || status=$?
    [ "$status" -eq 2 ] || failed 'self-test: unprefixed numeric SHA accepted'
    rm "$feed/nuget/Cratis.Test.1.2.4-local.200001010000.g1234567.nupkg"
    passed=$((passed + 2))
    : > "$feed/nuget/Cratis.Test.$version.nupkg"
    : > "$feed/nuget/Cratis.Transitive.$version.nupkg"
    mkdir -p "$root/tarball/package"
    for arg in test transitive; do
        printf '{"name":"@cratis/%s","version":"%s"}\n' "$arg" "$version" > "$root/tarball/package/package.json"
        tar -czf "$feed/npm/@cratis-$arg-$version.tgz" -C "$root/tarball" package
    done
    st use-nuget "$repo" "$version" > "$root/output"
    output=$(git -C "$repo" rev-parse --absolute-git-dir)/cratis-local
    grep -Fq "PackageVersion Include=\"Cratis.Test\" Version=\"$version\"" "$output/Directory.Packages.props" || failed 'self-test: pinned package Include'
    grep -Fq "PackageVersion Include=\"Cratis.Transitive\" Version=\"$version\"" "$output/Directory.Packages.props" || failed 'self-test: transitive package Include'
    if grep -Fq 'CentralPackageTransitivePinningEnabled' "$output/Directory.Packages.props"; then failed 'self-test: transitive pinning must be opt-in'; fi
    st use-nuget "$repo" "$version" --pin-transitive > "$root/output"
    grep -Fq 'CentralPackageTransitivePinningEnabled>true' "$output/Directory.Packages.props" || failed 'self-test: --pin-transitive enables pinning'
    self_test_nuget_overlay "$output" Cratis.Test "$version"
    passed=$((passed + 8))
    grep -Fq "PackageVersion Include=\"Cratis.Transitive\" Version=\"$version\"" "$output/Directory.Packages.props" || failed 'self-test: --pin-transitive keeps the IDs'
    st use-nuget "$repo" "$version" Cratis.Test --pin-transitive > "$root/output" || failed 'self-test: --pin-transitive with explicit IDs'
    st use-nuget "$repo" "$version" > "$root/output"
    passed=$((passed + 3))
    test_key=DirectoryPackages"PropsPath"
    grep -Fq "export $test_key=" "$output/env" || failed 'self-test: env property'
    passed=$((passed + 5))
    st check "$repo" > "$root/output" || failed 'self-test: clean repo'
    passed=$((passed + 1))
    # Legacy clean of a feed without stamp records must not need node.
    mkdir -p "$root/no-registry-feed/nuget"
    if PATH=/usr/bin:/bin:/usr/sbin:/sbin command -v node > /dev/null 2>&1; then
        printf 'SKIP: clean without node needs a system PATH without node\n' >&2; skipped=$((skipped + 1))
    else
        expect_status 0 'legacy clean without node or stamp records' env -i HOME="$home" PATH=/usr/bin:/bin:/usr/sbin:/sbin TMPDIR="$TMP" CRATIS_LOCAL_FEED="$root/no-registry-feed" /bin/bash "$SELF" clean
        passed=$((passed + 1))
    fi
    for test_key in stamp npm props sources targets previous override feed; do
        case "$test_key" in
            stamp) printf '%s\n' "$version" > "$repo/probe" ;;
            npm) printf '{"resolutions":{"@cratis/test":"%s%s"}}\n' 'file:/tmp/' 'package.tgz' > "$repo/package.json" ;;
            props) printf '%s=/tmp/overlay\n' DirectoryPackages"PropsPath" > "$repo/probe" ;;
            sources) printf '%s=/tmp/feed\n' RestoreAdditional"ProjectSources" > "$repo/probe" ;;
            targets) printf '%s=/tmp/overlay.targets\n' CustomAfterMicrosoftCommon"Targets" > "$repo/probe" ;;
            previous) printf '%s=/tmp/previous.targets\n' _CratisLocalPreviousAfterMicrosoftCommon"Targets" > "$repo/probe" ;;
            override) printf '<PackageVersion %s="Cratis.Test" />\n' Update > "$repo/probe" ;;
            feed) printf '%s\n' "$feed" > "$repo/probe" ;;
        esac
        status=0; st check "$repo" > "$root/output" || status=$?
        [ "$status" -eq 1 ] || failed "self-test: did not find $test_key leak"
        passed=$((passed + 1))
        git -C "$repo" checkout -- probe package.json
    done
    # A feed configured through a symlink is found under both its configured and its physical spelling.
    mkdir -p "$feed" && ln -s "$(dirname "$feed")" "$root/feed-alias"
    for test_key in "$root/feed-alias/$(basename "$feed")" "$(cd -P "$feed" && pwd -P)"; do
        printf '%s/nuget\n' "$test_key" > "$repo/probe"
        status=0; env -i HOME="$home" PATH=/usr/bin:/bin:/usr/sbin:/sbin TMPDIR="$TMP" CRATIS_LOCAL_FEED="$root/feed-alias/$(basename "$feed")" /bin/bash "$SELF" check "$repo" > "$root/output" || status=$?
        [ "$status" -eq 1 ] || failed "self-test: did not find aliased feed leak: $test_key"
        passed=$((passed + 1))
        git -C "$repo" checkout -- probe
    done
    rm "$root/feed-alias"
    # A consumer's own published-version reference update is legitimate, not an overlay leak.
    printf '<PackageReference %s="Cratis.Test" Version="1.2.3" />\n' Update > "$repo/probe"
    st check "$repo" > "$root/output" || failed 'self-test: published reference update reported as leak'
    git -C "$repo" checkout -- probe; passed=$((passed + 1))
    printf '%s\n' "$version" > "$repo/untracked"
    status=0; st check "$repo" > "$root/output" || status=$?
    [ "$status" -eq 1 ] || failed 'self-test: untracked leak'
    rm "$repo/untracked"; passed=$((passed + 1))
    printf '%s\n' "$version" > "$repo/probe"; git -C "$repo" add probe
    printf 'clean again\n' > "$repo/probe"
    status=0; st check "$repo" --staged > "$root/output" || status=$?
    [ "$status" -eq 1 ] || failed 'self-test: staged content'
    git -C "$repo" restore --staged probe; git -C "$repo" checkout -- probe
    passed=$((passed + 1))
    st restore "$repo" > "$root/output" || failed 'self-test: restore legacy overlay'
    [ ! -e "$output/cratis-local.targets" ] && [ ! -e "$output/env" ] || failed 'self-test: legacy targets and env survive restore'
    passed=$((passed + 1))
    if command -v node >/dev/null 2>&1; then
        # Preserve pre-existing user edits as well as the committed contents.
        st_npm use-npm "$repo" "$version"
        grep -Fq '"@cratis/transitive": "file:' "$repo/package.json" || failed 'self-test: transitive npm resolution'
        grep -Fq '"@cratis/test": "file:' "$repo/package.json" || failed 'self-test: direct npm resolution'
        # Execute the consumer tail printed by closure, with a fixture installation between
        # overlay and verification. In particular, check must not run before restore.
        {
            tsv pack Lib npm .
            tsv ids Lib npm @cratis/test,@cratis/transitive
            tsv uses App Lib npm .
        } > "$root/npm-closure.tsv"
        st_npm closure --map "$root/npm-closure.tsv" App="$repo" Lib="$repo" > "$root/npm-closure.out"
        printf 'node_modules/\n' >> "$repo/.git/info/exclude"
        for arg in test transitive; do
            mkdir -p "$repo/node_modules/@cratis/$arg"
            tar -xzf "$feed/npm/@cratis-$arg-$version.tgz" -C "$repo/node_modules/@cratis/$arg" --strip-components=1
        done
        status=0; st_npm check "$repo" > "$root/output" || status=$?
        [ "$status" -eq 1 ] || failed 'self-test: npm overlay needs restore before check'
        awk '/ verify-assets / { consumer=1 } consumer' "$root/npm-closure.out" > "$root/npm-closure-tail.sh"
        env -i HOME="$home" PATH="$PATH" TMPDIR="$TMP" STAMP="$version" \
            /bin/bash -e "$root/npm-closure-tail.sh" > "$root/output" || failed 'self-test: npm closure verification, restore, check and clean'
        cmp -s "$repo/package.json" <(git -C "$repo" show HEAD:package.json) || failed 'self-test: npm closure restores tracked manifest'
        grep -Fq 'dry-run:' "$root/output" || failed 'self-test: npm closure reaches clean'
        passed=$((passed + 6))
        printf '{"private":true,"dependencies":{"@cratis/test":"1.2.3"},"userSetting":true}\n' > "$repo/package.json"
        cp "$repo/package.json" "$root/original-package.json"
        cp "$repo/yarn.lock" "$root/original-yarn.lock"
        st_npm use-npm "$repo" "$version"
        # Model Yarn's replacement of a remote entry with a file-tarball entry. Yarn writes the
        # path the overlay put in package.json, which uses the physical feed path.
        node - "$repo/yarn.lock" "$(cd -P "$feed/npm" && pwd -P)/@cratis-test-$version.tgz" "$version" <<'NODE'
const fs = require('fs');
const [file, input, stamp] = process.argv.slice(2);
const tarball = require('path').normalize(input);
const lock = fs.readFileSync(file, 'utf8');
fs.writeFileSync(file, lock.replace(/"@cratis\/test@npm:1\.2\.3":[\s\S]*?(?="unrelated)/,
    `"@cratis/test@file:${tarball}::locator=consumer%40workspace%3A.":\n` +
    `  version: ${stamp}\n  resolution: "@cratis/test@file:${tarball}::hash=abc&locator=consumer%40workspace%3A."\n` +
    '  checksum: local\n  languageName: node\n  linkType: hard\n\n'));
NODE
        cp "$repo/yarn.lock" "$root/overlay-yarn.lock"
        sed 's|^"@cratis/test@file:.*":$|"@cratis/test@npm:1.2.3":|' "$root/overlay-yarn.lock" > "$repo/yarn.lock"
        node "$(dirname "$SELF")/npm-restore-check.cjs" "$repo" "$output/npm-backup" || failed 'self-test: local resolution with original descriptor'
        passed=$((passed + 1))
        cp "$root/overlay-yarn.lock" "$repo/yarn.lock"
        printf '\n"unrelated-user-addition@npm:2":\n  version: 2\n' >> "$repo/yarn.lock"
        status=0; st_npm restore "$repo" > "$root/output" 2>&1 || status=$?
        [ "$status" -eq 2 ] && [ -d "$output/npm-backup" ] || failed 'self-test: unrelated lockfile addition refused'
        grep -Fq 'file:' "$repo/package.json" || failed 'self-test: partial restore on conflict'
        passed=$((passed + 2))
        sed 's/checksum: keep/checksum: user-edit/' "$root/overlay-yarn.lock" > "$repo/yarn.lock"
        status=0; st_npm restore "$repo" > "$root/output" 2>&1 || status=$?
        [ "$status" -eq 2 ] || failed 'self-test: unrelated lockfile edit refused'
        passed=$((passed + 1))
        cp "$root/overlay-yarn.lock" "$repo/yarn.lock"
        node - "$repo/package.json" <<'NODE'
const fs = require('fs'), file = process.argv[2];
const json = JSON.parse(fs.readFileSync(file)); json.userSetting = false;
fs.writeFileSync(file, JSON.stringify(json));
NODE
        status=0; st_npm restore "$repo" > "$root/output" 2>&1 || status=$?
        [ "$status" -eq 2 ] || failed 'self-test: unrelated manifest edit refused'
        passed=$((passed + 1))
        cp "$output/npm-backup/expected-package.json" "$repo/package.json"
        st_npm restore "$repo" > "$root/output"
        cmp -s "$repo/package.json" "$root/original-package.json" || failed 'self-test: npm restore preserves prior manifest edits'
        cmp -s "$repo/yarn.lock" "$root/original-yarn.lock" || failed 'self-test: npm restore after install'
        [ ! -d "$output/npm-backup" ] || failed 'self-test: resolved backup removed'
        passed=$((passed + 3))
        git -C "$repo" checkout -- package.json
    else printf 'SKIP: npm mutation/restore needs node on inherited PATH\n'; skipped=$((skipped + 10)); fi
    # pack-npm uses npm pack so a nested, gitignored dist directory listed in files is packed whole.
    if command -v npm >/dev/null 2>&1 && command -v node >/dev/null 2>&1; then
        local pk_dir=$root/npm-workspace pk_feed=$root/npm-feed pk_ver=1.2.3-local.202601010000.gabcdef0 pk_count
        mkdir -p "$pk_dir/dist/bundles/sub" "$pk_feed/npm"
        printf '{"name":"@cratis/nested","version":"%s","files":["dist/bundles","dist/**/*.d.ts"]}\n' "$pk_ver" > "$pk_dir/package.json"
        printf 'dist\n' > "$pk_dir/.gitignore"
        printf 'a\n' > "$pk_dir/dist/bundles/a.js"; printf 'b\n' > "$pk_dir/dist/bundles/sub/b.js"
        printf 'export {}\n' > "$pk_dir/dist/a.d.ts"
        git -C "$pk_dir" init -q
        (FEED=$pk_feed; pack_npm_workspace "$pk_dir" package.json "$pk_ver") >/dev/null || failed 'self-test: npm workspace pack'
        [ -f "$pk_feed/npm/@cratis-nested-$pk_ver.tgz" ] || failed 'self-test: npm tarball name'
        pk_count=$(tar tzf "$pk_feed/npm/@cratis-nested-$pk_ver.tgz" | grep -c 'dist/bundles/') || true
        [ "$pk_count" -eq 2 ] || failed "self-test: nested dist files packed ($pk_count of 2)"
        tar tzf "$pk_feed/npm/@cratis-nested-$pk_ver.tgz" | grep -Fq 'dist/a.d.ts' || failed 'self-test: declaration glob packed'
        printf '{"name":"plainpkg","version":"%s","files":["dist/bundles"]}\n' "$pk_ver" > "$pk_dir/package.json"
        (FEED=$pk_feed; pack_npm_workspace "$pk_dir" package.json "$pk_ver") >/dev/null || failed 'self-test: unscoped npm workspace pack'
        [ -f "$pk_feed/npm/plainpkg-$pk_ver.tgz" ] || failed 'self-test: unscoped npm tarball kept'
        passed=$((passed + 5))
    else printf 'SKIP: npm pack needs node and npm on inherited PATH\n'; skipped=$((skipped + 5)); fi
    # Multi-repo stamp: the highest next patch wins and every HEAD is reported.
    local repo2 v2
    repo2=$root/repo2; mkdir -p "$repo2"; git -C "$repo2" init -q
    git -C "$repo2" config user.email test@example.invalid; git -C "$repo2" config user.name 'Local package self-test'
    printf 'x\n' > "$repo2/f"; git -C "$repo2" add f; git -C "$repo2" commit -qm initial; git -C "$repo2" tag v2.0.0
    v2=$(st stamp "$repo" "$repo2" 2> "$root/stderr")
    [[ "$v2" =~ ^2\.0\.1-local\.[0-9]{12}\.g$(git -C "$repo2" rev-parse --short=7 HEAD)$ ]] || failed 'self-test: multi-repo stamp uses highest base and last sha'
    [ "$(grep -c '^HEAD ' "$root/stderr")" -eq 2 ] || failed 'self-test: multi-repo stamp prints each HEAD'
    v2=$(st stamp "$repo2" "$repo" 2> /dev/null)
    [[ "$v2" =~ ^2\.0\.1-local\. ]] || failed 'self-test: multi-repo stamp order independent base'
    passed=$((passed + 3))
    # A train stamp covers every producer: changing a non-last producer changes it, order does not.
    local ta tb tc
    ta=$(st stamp --train "$repo" "$repo2" 2> /dev/null); tb=$(st stamp --train "$repo2" "$repo" 2> /dev/null)
    [ "$ta" = "$tb" ] || failed 'self-test: train stamp is independent of producer order'
    [[ "$ta" =~ ^2\.0\.1-train\.g[0-9a-f]{7}$ ]] || failed "self-test: multi-producer train grammar ($ta)"
    printf 'y\n' > "$repo/g"; git -C "$repo" add g; git -C "$repo" commit -qm second
    tc=$(st stamp --train "$repo" "$repo2" 2> /dev/null)
    [ "$tc" != "$ta" ] || failed 'self-test: a change in a non-last producer must change the train stamp'
    git -C "$repo" reset -q --hard HEAD~1
    [ "$(st stamp --train "$repo" "$repo2" 2> /dev/null)" = "$ta" ] || failed 'self-test: train stamp returns with the same state'
    passed=$((passed + 4))
    # Train mode: a deterministic X.Y.Z-train.g<sha7> stamp, accepted everywhere a local stamp is.
    local tv tv2 tsha tleak
    tsha=$(printf '%s %s\n' "$(basename "$repo")" "$(git -C "$repo" rev-parse HEAD)" | shasum -a 1 | cut -c1-7)
    tv=$(st stamp --train "$repo") || failed 'self-test: stamp --train'
    tv2=$(st stamp --train "$repo")
    [[ "$tv" =~ ^1\.2\.4-train\.g$tsha$ ]] || failed "self-test: train stamp grammar ($tv)"
    [ "$tv" = "$tv2" ] || failed 'self-test: train stamp is deterministic'
    : > "$feed/nuget/Cratis.Train.$tv.nupkg"; : > "$feed/nuget/Cratis.Train.1.2.4-local.200001010000.g1234567.nupkg"
    st --dry-run use-nuget "$repo" "$tv" Cratis.Train > "$root/output" || failed 'self-test: use-nuget accepts a train stamp'
    for tleak in 1.2.4-train.1234567 1.2.4-train.200001010000.g1234567 1.2.4-train.gABCDEF0 1.2.4-train.g123456; do
        status=0; st --dry-run use-nuget "$repo" "$tleak" Cratis.Train > "$root/output" 2>&1 || status=$?
        [ "$status" -eq 2 ] || failed "self-test: invalid train stamp accepted ($tleak)"
    done
    st clean --stamp "$tv" > "$root/output" || failed 'self-test: clean accepts a train stamp'
    has "Cratis.Train.$tv.nupkg" "$root/output" || failed 'self-test: clean lists the train artifact'
    if has '200001010000' "$root/output"; then failed 'self-test: clean --stamp train must not list a local stamp'; fi
    rm "$feed/nuget/Cratis.Train.$tv.nupkg" "$feed/nuget/Cratis.Train.1.2.4-local.200001010000.g1234567.nupkg"
    printf 'Version="%s"\n' "$tv" > "$repo/probe-train"
    status=0; st check "$repo" > "$root/output" 2>&1 || status=$?
    [ "$status" -eq 1 ] || failed 'self-test: check misses an untracked train version'
    has 'train stamp' "$root/output" || failed 'self-test: check names the train stamp'
    git -C "$repo" add probe-train
    status=0; st check "$repo" > "$root/output" 2>&1 || status=$?
    [ "$status" -eq 1 ] || failed 'self-test: check misses a staged train version'
    status=0; st check "$repo" --staged > "$root/output" 2>&1 || status=$?
    [ "$status" -eq 1 ] || failed 'self-test: check --staged misses a train version'
    git -C "$repo" rm -qf --cached probe-train; rm "$repo/probe-train"
    st check "$repo" > "$root/output" || failed 'self-test: check clean after removing the train probe'
    passed=$((passed + 13))
    # use-npm --dir for a Yarn project in a subdirectory (no root package.json).
    if command -v node >/dev/null 2>&1; then
        local sub=$root/repo3
        mkdir -p "$sub/Source/Workbench"; git -C "$sub" init -q
        git -C "$sub" config user.email test@example.invalid; git -C "$sub" config user.name 'Local package self-test'
        cp "$root/original-package.json" "$sub/Source/Workbench/package.json" 2>/dev/null || printf '{"private":true,"dependencies":{"@cratis/test":"1.2.3"}}\n' > "$sub/Source/Workbench/package.json"
        printf '# lock\n' > "$sub/Source/Workbench/yarn.lock"
        git -C "$sub" add .; git -C "$sub" commit -qm initial
        status=0; st_npm use-npm "$sub" "$version" > "$root/output" 2>&1 || status=$?
        [ "$status" -eq 2 ] || failed 'self-test: use-npm without --dir needs a root package.json'
        st_npm use-npm "$sub" "$version" --dir Source/Workbench > "$root/output" || failed 'self-test: use-npm --dir'
        grep -Fq '"@cratis/test": "file:' "$sub/Source/Workbench/package.json" || failed 'self-test: use-npm --dir overlays the subdirectory manifest'
        status=0; st_npm use-npm "$sub" "$version" --dir ../x > "$root/output" 2>&1 || status=$?
        [ "$status" -eq 2 ] || failed 'self-test: use-npm --dir rejects escapes'
        st_npm restore "$sub" > "$root/output" || failed 'self-test: restore follows --dir'
        git -C "$sub" diff --quiet || failed 'self-test: restore --dir leaves the tracked files clean'
        [ ! -e "$(git -C "$sub" rev-parse --absolute-git-dir)/cratis-local/npm-dir" ] || failed 'self-test: restore removes npm-dir'
        passed=$((passed + 6))
    else printf 'SKIP: use-npm --dir needs node\n'; skipped=$((skipped + 6)); fi
    self_test_live_clean
    self_test_private_cache
    self_test_registry
    mkdir -p "$home/.nuget/packages/cratis.test/$version" "$home/.nuget/packages/cratis.test/1.2.3"
    mkdir -p "$home/.nuget/packages/cratis.test/1.2.3-local.keep"
    for arg in nuget npm; do
        : > "$feed/$arg/keep-local.notes.tgz"
        : > "$feed/$arg/keep-local.202610221735.g12345678.tgz"
    done
    for arg in default --all; do
        if [ "$arg" = default ]; then st clean > "$root/output"; else st clean "$arg" > "$root/output"; fi
        grep -Fq "$version" "$root/output" || failed 'self-test: clean finds stamped subjects'
        if grep -Eq 'keep-local|local.keep' "$root/output"; then failed 'self-test: clean selected non-stamp'; fi
        passed=$((passed + 2))
    done
    st clean --stamp "$version" > "$root/output"
    grep -Fq "$home/.nuget/packages/cratis.test/$version" "$root/output" || failed 'self-test: clean lists local cache'
    [ -d "$home/.nuget/packages/cratis.test/$version" ] || failed 'self-test: dry-run mutated cache'
    : > "$feed/nuget/Cratis.Test.$version.snupkg"
    st clean --stamp "$version" --apply > "$root/output"
    [ ! -e "$feed/nuget/Cratis.Test.$version.snupkg" ] || failed 'self-test: clean left a symbols package'
    [ ! -e "$home/.nuget/packages/cratis.test/$version" ] || failed 'self-test: clean failed to remove cache'
    [ -d "$home/.nuget/packages/cratis.test/1.2.3" ] || failed 'self-test: clean removed real version'
    passed=$((passed + 5))
    printf 'SKIP: real dotnet/Yarn/Docker packing is not a hermetic self-test\n'; skipped=$((skipped + 3))
    self_test_expired
    self_test_matrix
    printf 'self-test: %s passed, %s skipped\n' "$passed" "$skipped"
}

self_test_expired() {
    local fixture ef er es ec record result expected stamp=9.8.7-train.gabcdef0
    local fixtures=('task finished' 'consumers gone' 'released offline cache' 'live reference kept' 'composition kept' 'unrecorded kept' 'dry-run no mutation' 'with stamp' 'fuse' 'online unreachable kept' 'prunes sessions' 'list json expiry' 'invalid record' 'record consumer reference kept' 'private release cache' 'prerelease cache kept' 'older release kept' 'without task kept' 'with all')
    ext() { env -i HOME="$ef/home" PATH="$system_path" TMPDIR="$TMP" CRATIS_LOCAL_FEED="$ef/feed" NUGET_PACKAGES="$ec" CRATIS_LOCAL_CLEAN_MAX="${limit:-500}" CRATIS_LOCAL_RELEASE_URL_BASE=http://127.0.0.1:1 /bin/bash "$SELF" "$@"; }
    for fixture in "${fixtures[@]}"; do
        ef=$root/expired-${fixture// /-}; er=$ef/repo; ec=$ef/cache
        mkdir -p "$er" "$ef/feed/nuget" "$ec/cratis.expiry/$stamp" "$ef/home"
        git -C "$er" init -q
        es=$(git -C "$er" rev-parse --absolute-git-dir)/cratis-local
        record=$ef/feed/stamps/$stamp/record.json
        mkdir -p "${record%/*}"
        printf 'fixture\n' > "$ef/feed/nuget/Cratis.Expiry.$stamp.nupkg"
        node - "$record" "$stamp" "$er" "$es" "$fixture" <<'NODE'
const fs = require('node:fs'); const [file,stamp,repo,session,fixture] = process.argv.slice(2), now = new Date().toISOString();
const finished = ['task finished','live reference kept','composition kept','dry-run no mutation','fuse','prunes sessions','list json expiry','record consumer reference kept'].includes(fixture);
const r = {schema:1,stamp,base:'9.8.7',kind:'train',created:now,updated:now,
    packs:[{ecosystem:'nuget',ids:['Cratis.Expiry'],producer:{repo:'Fixture',worktree:repo,branch:'main',head:'a'.repeat(40)},task:'Fixture#1',time:now}],
    consumers:[],tasks:{'Fixture#1':{finished:finished ? now : null}}};
if (fixture === 'consumers gone') r.consumers.push({worktree:repo + '/gone',session:session + '-gone',ecosystem:'nuget',task:null,time:now});
if (fixture === 'record consumer reference kept') r.consumers.push({worktree:repo,session,ecosystem:'nuget',task:null,time:now});
if (fixture === 'without task kept') r.packs[0].task = null;
fs.writeFileSync(file,JSON.stringify(r) + '\n');
NODE
        expected=0; result=removed
        case "$fixture" in
            'released offline cache') mkdir -p "$ec/cratis.expiry/9.8.8"; : > "$ec/cratis.expiry/9.8.8/.nupkg.metadata" ;;
            'private release cache')
                mkdir -p "$es" "$er/.git/cratis-local-nuget/cratis.expiry/9.8.7"
                printf '%s\n' "$er" > "$es/repo"
                printf '%s\n' "$es" > "$ef/feed/sessions"
                printf '%s\n' "$er/.git/cratis-local-nuget" > "$es/nuget-cache"
                printf '%s\n' "$er" > "$er/.git/cratis-local-nuget/.cratis-local-cache"
                : > "$er/.git/cratis-local-nuget/cratis.expiry/9.8.7/.nupkg.metadata" ;;
            'live reference kept'|'record consumer reference kept')
                mkdir -p "$es"; printf '%s\n' "$er" > "$es/repo"
                if [ "$fixture" = 'live reference kept' ]; then printf '%s\n' "$es" > "$ef/feed/sessions"; fi
                printf '{"dependencies":{"fixture":"%s"}}\n' "$stamp" > "$er/package.json"
                result=kept ;;
            'composition kept')
                mkdir -p "$es/composition"; printf '%s\n' "$er" > "$es/repo"
                printf '%s\n' "$es" > "$ef/feed/sessions"
                printf 'Fixture\tCratis.Expiry\t%s\t%s\n' "$stamp" "$er" > "$es/composition/manifest.tsv"
                result=kept ;;
            'unrecorded kept') rm -- "$record"; rmdir "${record%/*}"; result=kept ;;
            'dry-run no mutation') cp "$record" "$ef/before"; result=kept ;;
            'with stamp'|'with all'|'fuse'|'invalid record')
                expected=2; result=kept
                if [ "$fixture" = 'invalid record' ]; then printf '{broken\n' > "$record"; fi ;;
            'prerelease cache kept') mkdir -p "$ec/cratis.expiry/9.8.9-preview"; : > "$ec/cratis.expiry/9.8.9-preview/.nupkg.metadata"; result=kept ;;
            'older release kept') mkdir -p "$ec/cratis.expiry/9.8.6"; : > "$ec/cratis.expiry/9.8.6/.nupkg.metadata"; result=kept ;;
            'online unreachable kept'|'without task kept') result=kept ;;
            'list json expiry') result=kept ;;
            'prunes sessions')
                mkdir -p "$es"; printf '%s\n' "$er" > "$es/repo"; : > "$es/env"
                printf '%s\n%s\n' "$ef/missing-session" "$es" > "$ef/feed/sessions"
                node - "$record" "$es" <<'NODE'
const fs = require('node:fs'); const [file,session] = process.argv.slice(2), r = JSON.parse(fs.readFileSync(file));
r.stamp = '9.8.7-train.g1234567'; r.packs[0].task = null;
r.consumers = [{worktree:r.packs[0].producer.worktree,session,ecosystem:'nuget',task:null,time:r.created},
    {worktree:'/nonexistent/cratis-expiry-fixture',session:session + '-gone',ecosystem:'nuget',task:null,time:r.created},
    {worktree:r.packs[0].producer.worktree,session:session + '-missing',ecosystem:'nuget',task:null,time:r.created}];
const target = file.replace('gabcdef0','g1234567'); fs.mkdirSync(require('node:path').dirname(target)); fs.writeFileSync(target,JSON.stringify(r));
NODE
                ;;
        esac
        case "$fixture" in
            'with stamp') expect_status "$expected" 'expired with stamp' ext clean --expired --stamp "$stamp" --apply ;;
            'with all') expect_status "$expected" 'expired with all' ext clean --all --expired --apply ;;
            'fuse') limit=0 expect_status "$expected" 'expired fuse' ext clean --expired --apply ;;
            'online unreachable kept') expect_status 0 'expired online unreachable kept' ext clean --expired --online --apply ;;
            'dry-run no mutation') expect_status 0 'expired dry-run no mutation' ext clean --expired ;;
            'list json expiry')
                expect_status 0 'list json expiry' ext list --json
                node - "$root/output" <<'NODE' || failed 'list json expiry decision'
const r = JSON.parse(require('node:fs').readFileSync(process.argv[2])).stamps;
if (r.length !== 1 || !r[0].expiry.expired || r[0].expiry.reasons.join() !== 'task-finished' || r[0].expiry.kept.length) process.exit(1);
NODE
                ;;
            *) expect_status "$expected" "expired $fixture" ext clean --expired --apply ;;
        esac
        if [ "$result" = removed ]; then
            [ ! -e "$record" ] && [ ! -e "$ef/feed/nuget/Cratis.Expiry.$stamp.nupkg" ] && [ ! -e "$ec/cratis.expiry/$stamp" ] || failed "expired $fixture left subjects"
        else
            [ -f "$ef/feed/nuget/Cratis.Expiry.$stamp.nupkg" ] && [ -d "$ec/cratis.expiry/$stamp" ] || failed "expired $fixture removed subjects"
        fi
        case "$fixture" in
            'task finished'|'consumers gone') grep -Fq "expired $stamp: ${fixture// /-}" "$root/output" || failed "expired $fixture reason" ;;
            'released offline cache') grep -Fq 'released:Cratis.Expiry@9.8.8' "$root/output" || failed 'offline release reason' ;;
            'live reference kept'|'record consumer reference kept') grep -Fq "kept $stamp: live reference in $er/package.json" "$root/output" || failed 'live override reason' ;;
            'composition kept') grep -Fq "kept $stamp: active composition member" "$root/output" || failed 'composition override reason' ;;
            'unrecorded kept') grep -Fq "unrecorded; use clean --stamp $stamp" "$root/output" || failed 'unrecorded reason' ;;
            'online unreachable kept') grep -Fq "kept $stamp: active" "$root/output" || failed 'network failure removed active stamp' ;;
            'dry-run no mutation') cmp -s "$record" "$ef/before" || failed 'expired dry-run rewrote record' ;;
            'prunes sessions')
                [ "$(cat "$ef/feed/sessions")" = "$es" ] || failed 'expired prunes sessions'
                node - "$ef/feed/stamps/9.8.7-train.g1234567/record.json" <<'NODE' || failed 'expired prunes consumers'
// The consumer with a missing session but an existing worktree stays, so its manifests remain protected.
const r = JSON.parse(require('node:fs').readFileSync(process.argv[2])); if (r.consumers.length !== 2 || r.consumers.some(c => c.worktree.endsWith('fixture')) || !r.consumers.some(c => c.session.endsWith('-missing'))) process.exit(1);
NODE
                ;;
        esac
    done
}

self_test_registry() {
    local rr=$root/'registry repo' rf=$root/registry-feed rb=$root/registry-tools rs record
    local rt=Cratis/Fixture#1 legacy=1.2.5-train.gabcdef0
    mkdir -p "$rr" "$rf/nuget" "$rb"
    git -C "$rr" init -q
    cp "$repo/Directory.Packages.props" "$rr/Directory.Packages.props"
    printf '{"name":"@cratis/registry","version":"1.0.0"}\n' > "$rr/package.json"
    git -C "$rr" add .
    git -C "$rr" -c user.email=test@example.invalid -c user.name='Registry fixture' commit -qm initial
    rs=$(git -C "$rr" rev-parse --absolute-git-dir)/cratis-local
    record=$rf/stamps/$version/record.json
    # Pack-tool doubles exercise successful/failed pack boundaries without building.
    cat > "$rb/dotnet" <<'SH'
#!/bin/bash
set -eu
[ ! -e "$CRATIS_LOCAL_FEED/fail-pack" ] || exit 1
version=''; output=''
for arg in "$@"; do
    case "$arg" in -p:PackageVersion=*) version=${arg#*=} ;; esac
    if [ "$output" = next ]; then output=$arg; fi
    if [ "$arg" = -o ]; then output=next; fi
done
printf 'packed\n' > "$output/Cratis.Registry.$version.nupkg"
SH
    printf '#!/bin/sh\nprintf '\''{"location":"."}\\n'\''\n' > "$rb/yarn"
    cat > "$rb/npm" <<'SH'
#!/bin/bash
set -eu
version=$(node -p 'require("./package.json").version')
mkdir -p "$TMPDIR/registry-tar/package"
cp package.json "$TMPDIR/registry-tar/package/package.json"
tar -czf "$CRATIS_LOCAL_FEED/npm/cratis-registry-$version.tgz" -C "$TMPDIR/registry-tar" package
SH
    chmod +x "$rb/dotnet" "$rb/npm" "$rb/yarn"
    rgt() { env -i HOME="$home" PATH="$rb:$system_path" TMPDIR="$TMP" CRATIS_LOCAL_FEED="$rf" NUGET_PACKAGES="$root/registry-cache" CRATIS_LOCAL_TASK="$rt" /bin/bash "$SELF" "$@"; }
    rg_assert() { node - "$record" "$@" <<'NODE'
const fs = require('node:fs'); const [file,kind,version,repo,session] = process.argv.slice(2);
const r = JSON.parse(fs.readFileSync(file,'utf8'));
if (r.schema !== 1 || r.stamp !== version || !r.created.endsWith('Z') || !r.updated.endsWith('Z')) process.exit(1);
if (kind === 'pack' && (r.packs.length !== 2 || !r.packs.some(p => p.ecosystem === 'nuget' && p.ids.join() === 'Cratis.Registry') || !r.packs.some(p => p.ids.includes('@cratis/registry')) || r.packs.some(p => p.producer.worktree !== repo || !/^[0-9a-f]{40}$/.test(p.producer.head)) || (fs.statSync(file).mode & 0o777) !== 0o600)) process.exit(1);
if (kind === 'use' && (r.consumers.length !== 2 || r.consumers.some(c => c.worktree !== repo || c.session !== session || c.task !== 'Cratis/Fixture#2'))) process.exit(1);
if (kind === 'restored' && r.consumers.some(c => !c.restored?.endsWith('Z'))) process.exit(1);
if (kind === 'finished' && !r.tasks['Cratis/Fixture#1'].finished?.endsWith('Z')) process.exit(1);
NODE
    }
    : > "$rf/nuget/Cratis.Previous.$version.nupkg"
    rgt pack-nuget "$rr" "$version" Fixture.csproj > "$root/output"
    rgt pack-npm "$rr" "$version" > "$root/output"
    expect_status 0 'registry pack records' rg_assert pack "$version" "$rr" "$rs"
    rgt use-nuget "$rr" "$version" --task Cratis/Fixture#2 > "$root/output"
    rgt use-npm "$rr" "$version" --task Cratis/Fixture#2 > "$root/output"
    expect_status 0 'registry use consumer' rg_assert use "$version" "$rr" "$rs"
    expect_status 0 'registry restore consumer' rgt restore "$rr"
    rg_assert restored "$version" "$rr" "$rs" || failed 'registry restore history'
    cp "$record" "$root/registry-before"
    expect_status 0 'registry dry-run no write' rgt --dry-run pack-nuget "$rr" "$legacy" Fixture.csproj
    [ ! -e "$rf/stamps/$legacy" ] || failed 'registry dry-run created record'
    rgt --dry-run use-nuget "$rr" "$version" > "$root/output"
    cmp -s "$record" "$root/registry-before" || failed 'registry dry-run use rewrote record'
    expect_status 0 'finish task dry-run' rgt finish --task "$rt"
    cmp -s "$record" "$root/registry-before" || failed 'finish dry-run rewrote record'
    expect_status 0 'finish task apply' rgt finish --task "$rt" --apply
    rg_assert finished "$version" "$rr" "$rs" || failed 'finish did not mark task'
    expect_status 0 'finish unknown task' rgt finish --task Cratis/Unknown#2 --apply
    grep -Fq 'no records name task' "$root/output" || failed 'finish unknown task message'
    : > "$rf/nuget/Cratis.Legacy.$legacy.nupkg"
    expect_status 0 'list unrecorded legacy' rgt list --stamp "$legacy" --json
    node -e 'const r=JSON.parse(require("fs").readFileSync(process.argv[1])).stamps;if(r.length!==1||r[0].recorded||r[0].packages[0].id!=="Cratis.Legacy")process.exit(1)' "$root/output" || failed 'list legacy missing'
    rgt list --json > "$root/registry-list"
    rg_json() { node - "$root/registry-list" "$rf" <<'NODE'
const fs = require('node:fs'); const [file,feed] = process.argv.slice(2), data = JSON.parse(fs.readFileSync(file));
if (data.schema !== 1 || data.feed !== feed || data.stamps.length !== 2) process.exit(1);
const keys = ['stamp','base','kind','recorded','packages','producers','tasks','consumers','created','ageSeconds','expiry'].sort().join();
for (const r of data.stamps) {
    if (Object.keys(r).sort().join() !== keys || typeof r.expiry.expired !== 'boolean' || !Array.isArray(r.expiry.reasons) || !Array.isArray(r.expiry.kept)) process.exit(1);
    for (const p of r.packages) if (Object.keys(p).sort().join() !== 'ecosystem,file,id,version') process.exit(1);
    for (const c of r.consumers) if (c.state !== 'restored') process.exit(1);
}
NODE
    }
    expect_status 0 'list json schema' rg_json
    cp "$record" "$root/registry-good"
    printf '{"schema":2}\n' > "$record"
    cp "$record" "$root/registry-bad"
    expect_status 2 'registry rejects bad schema' rgt clean --stamp "$version" --apply
    cmp -s "$record" "$root/registry-bad" || failed 'registry rewrote bad schema'
    printf '{broken\n' > "$record"
    expect_status 2 'registry rejects malformed JSON' rgt clean --stamp "$version" --apply
    cp "$root/registry-good" "$record"
    mv "$rf/stamps" "$rf/real-stamps"; ln -s "$rf/real-stamps" "$rf/stamps"
    expect_status 2 'registry symlink refused' rgt finish --task "$rt" --apply
    rm "$rf/stamps"; mv "$rf/real-stamps" "$rf/stamps"
    ln -s "$root/registry-good" "$record.next"
    expect_status 2 'registry next symlink refused' rgt finish --task "$rt" --apply
    rm "$record.next"
    expect_status 2 'registry bad task id' rgt pack-nuget "$rr" "$version" --task 'bad task' Fixture.csproj
    : > "$rf/fail-pack"
    expect_status 1 'registry failed pack no write' rgt pack-nuget "$rr" "$legacy" Fixture.csproj
    [ ! -e "$rf/stamps/$legacy" ] || failed 'failed pack wrote record'
    rm "$rf/fail-pack"
    # Post-publication failures preserve overlays and report a warning/findings.
    printf '{"schema":2}\n' > "$record"
    expect_status 1 'registry use failure keeps overlay' rgt use-nuget "$rr" "$version"
    { [ -f "$rs/env" ] && grep -Fq WARNING "$root/stderr"; } || failed 'registry failed use lost overlay/warning'
    cp "$root/registry-good" "$record"
    rgt restore "$rr" > "$root/output"
    expect_status 0 'clean removes stamp record' rgt clean --stamp "$version" --apply
    [ ! -e "$rf/stamps/$version" ] && [ ! -e "$rf/nuget/Cratis.Registry.$version.nupkg" ] || failed 'clean retained stamp record/artifacts'
    [ -e "$rf/nuget/Cratis.Legacy.$legacy.nupkg" ] || failed 'registry clean removed another stamp'
    self_test_registry_invalid
}

self_test_registry_invalid() {
    local fixture rf bad_record other=1.2.3-train.gabcdef0
    for fixture in clean-scoped restore-other restore-no-node restore-own finish list; do
        rf=$root/registry-invalid-$fixture
        bad_record=$rf/stamps/$other/record.json
        mkdir -p "$rf/nuget" "${bad_record%/*}"
        printf '{broken\n' > "$bad_record"
        : > "$rf/nuget/Cratis.Test.$version.nupkg"
        rgt use-nuget "$rr" "$version" > "$root/output"
        if [ "$fixture" = restore-own ]; then
            bad_record=$rf/stamps/$version/record.json
            printf '{broken\n' > "$bad_record"
        fi
        cp "$bad_record" "$root/invalid-before"
        case "$fixture" in
            clean-scoped)
                rgt restore "$rr" > "$root/output"
                expect_status 0 'registry invalid other record clean scoped' rgt clean --stamp "$version" --apply
                [ ! -e "$rf/stamps/$version" ] && [ ! -e "$rf/nuget/Cratis.Test.$version.nupkg" ] || failed 'invalid other record blocked scoped clean'
                expect_status 2 'registry invalid record clean all' rgt clean --all --apply
                expect_status 2 'registry invalid record clean default' rgt clean --apply
                ;;
            restore-other)
                expect_status 0 'registry invalid other record restore' rgt restore "$rr"
                ;;
            restore-no-node)
                if PATH=/usr/bin:/bin:/usr/sbin:/sbin command -v node > /dev/null 2>&1; then
                    printf 'SKIP: restore without node needs a system PATH without node\n' >&2; skipped=$((skipped + 1))
                    rgt restore "$rr" > "$root/output"
                else
                    expect_status 0 'registry restore without node restores the overlay' env -i HOME="$home" PATH=/usr/bin:/bin:/usr/sbin:/sbin TMPDIR="$TMP" CRATIS_LOCAL_FEED="$rf" NUGET_PACKAGES="$root/registry-cache" /bin/bash "$SELF" restore "$rr"
                    { [ ! -e "$rs/env" ] && grep -Fq 'WARNING: node not found' "$root/stderr"; } || failed 'restore without node kept overlay or missed warning'
                fi
                ;;
            restore-own)
                expect_status 1 'registry invalid own record restore' rgt restore "$rr"
                { grep -Fq 'WARNING:' "$root/stderr" && grep -Fq "$bad_record" "$root/stderr"; } || failed 'restore missing invalid record warning'
                ;;
            finish)
                expect_status 1 'registry finish skips invalid' rgt finish --task "$rt" --apply
                record=$rf/stamps/$version/record.json rg_assert finished "$version" "$rr" "$rs" || failed 'finish skipped valid record'
                { grep -Fq 'WARNING:' "$root/stderr" && grep -Fq "$bad_record" "$root/stderr"; } || failed 'finish missing invalid record warning'
                rgt restore "$rr" > "$root/output"
                ;;
            list)
                expect_status 1 'list invalid record' rgt list --json
                node - "$root/output" "$version" "$other" <<'NODE' || failed 'list missing valid/invalid rows'
const fs = require('node:fs'); const [file, valid, invalid] = process.argv.slice(2);
const rows = JSON.parse(fs.readFileSync(file, 'utf8')).stamps;
if (rows.length !== 2 || rows.find(r => r.stamp === valid)?.recorded !== true ||
    rows.find(r => r.stamp === invalid)?.recorded !== 'invalid' || typeof rows.find(r => r.stamp === invalid)?.error !== 'string') process.exit(1);
NODE
                expect_status 1 'list invalid record text' rgt list
                { grep -Fq invalid "$root/output" && grep -Fq "$version" "$root/output"; } || failed 'text list missing valid/invalid rows'
                rgt restore "$rr" > "$root/output"
                ;;
        esac
        [ ! -e "$rs/env" ] && [ ! -e "$rs/Directory.Packages.props" ] || failed 'invalid record prevented overlay restore'
        cmp -s "$bad_record" "$root/invalid-before" || failed 'invalid record rewritten'
    done
}

self_test_private_cache() {
    local pc_repo=$root/'private cache repo' pc_feed=$root/private-feed pc_shared=$root/private-shared
    local pc_cache pc_session pc_opt=0 other=1.2.5-train.g1234567
    mkdir -p "$pc_repo" "$pc_feed/nuget"
    git -C "$pc_repo" init -q
    cp "$repo/Directory.Packages.props" "$pc_repo/Directory.Packages.props"
    git -C "$pc_repo" add Directory.Packages.props
    git -C "$pc_repo" -c user.email=test@example.invalid -c user.name='Private cache fixture' commit -qm initial
    pc_cache=$(git -C "$pc_repo" rev-parse --absolute-git-dir)/cratis-local-nuget
    pc_session=$(git -C "$pc_repo" rev-parse --absolute-git-dir)/cratis-local
    pct() { env -i HOME="$home" PATH="$system_path" TMPDIR="$TMP" CRATIS_LOCAL_FEED="$pc_feed" NUGET_PACKAGES="$pc_shared" CRATIS_LOCAL_PRIVATE_CACHE="$pc_opt" /bin/bash "$SELF" "$@"; }
    : > "$pc_feed/nuget/Cratis.Test.$version.nupkg"
    expect_status 0 'private-cache default unchanged' pct use-nuget "$pc_repo" "$version"
    [ ! -e "$pc_cache" ] && [ ! -e "$pc_session/nuget-cache" ] || failed 'private-cache default created a cache'
    has 'Optional per-worktree cache' "$root/output" || failed 'private-cache default hint changed'
    pct restore "$pc_repo" > "$root/output"
    expect_status 0 'private-cache dry-run' pct --dry-run use-nuget "$pc_repo" "$version" --private-cache
    [ ! -e "$pc_cache" ] && [ ! -e "$pc_session/nuget-cache" ] || failed 'private-cache dry-run wrote a cache'
    expect_status 0 'private-cache env export' pct use-nuget "$pc_repo" "$version" --private-cache
    env -i PATH=/usr/bin:/bin /bin/bash -c "source \"\$1\"; [ \"\$NUGET_PACKAGES\" = \"\$2\" ]" _ "$pc_session/env" "$pc_cache" || failed 'private-cache env does not export gitdir cache'
    if has 'Optional per-worktree cache' "$root/output"; then failed 'private-cache still prints manual hint'; fi
    pc_marker() { [ "$(cat "$pc_cache/.cratis-local-cache")" = "$pc_repo" ] && [ "$(cat "$pc_session/nuget-cache")" = "$pc_cache" ]; }
    expect_status 0 'private-cache marker' pc_marker
    expect_status 2 'private-cache live kept' pct clean "$pc_repo" --private-cache --apply
    [ -d "$pc_cache" ] || failed 'private-cache live cache removed'
    pct restore "$pc_repo" > "$root/output"
    [ -f "$pc_session/nuget-cache" ] && [ -d "$pc_cache" ] || failed 'private-cache restore removed cache'
    mkdir -p "$pc_cache/cratis.test/$version" "$pc_cache/cratis.test/$other" "$pc_cache/cratis.test/1.2.3" "$pc_cache/cratis.test/1.2.3-local.keep"
    mkdir -p "$pc_shared/cratis.test/$version" "$pc_shared/cratis.test/$other" "$pc_shared/cratis.test/1.2.3"
    expect_status 0 'private-cache clean scoped' pct clean --stamp "$version" --apply
    [ ! -e "$pc_cache/cratis.test/$version" ] && [ -d "$pc_cache/cratis.test/$other" ] && [ -d "$pc_cache/cratis.test/1.2.3" ] && [ -d "$pc_cache/cratis.test/1.2.3-local.keep" ] || failed 'private-cache stamp scope violated'
    [ ! -e "$pc_shared/cratis.test/$version" ] || failed 'private-cache shared local version retained'
    expect_status 0 'private-cache shared cache only local' pct clean --all --apply
    [ ! -e "$pc_shared/cratis.test/$other" ] && [ -d "$pc_shared/cratis.test/1.2.3" ] || failed 'private-cache shared non-local folder removed'
    rm -- "$pc_cache/.cratis-local-cache"
    mkdir -p "$pc_shared/cratis.test/$version"
    expect_status 2 'private-cache unmarked refused' pct clean --stamp "$version" --apply
    [ -d "$pc_shared/cratis.test/$version" ] || failed 'private-cache unmarked refusal mutated shared cache'
    printf '%s\n' "$root/other-repo" > "$pc_cache/.cratis-local-cache"
    expect_status 2 'private-cache mismatched marker refused' pct clean --all --apply
    printf '%s\n' "$pc_repo" > "$pc_cache/.cratis-local-cache"
    mv "$pc_cache" "$pc_cache.saved"; ln -s "$pc_cache.saved" "$pc_cache"
    expect_status 2 'private-cache symlink refused' pct clean --apply
    rm -- "$pc_cache"; mv "$pc_cache.saved" "$pc_cache"
    printf '%s\n' "$pc_shared" > "$pc_session/nuget-cache"
    expect_status 2 'private-cache outside gitdir refused' pct clean --apply
    printf '%s\n' "$pc_cache" > "$pc_session/nuget-cache"
    printf '<Project><!-- %s --></Project>\n' "$other" > "$pc_repo/Directory.Packages.props"
    expect_status 2 'private-cache restored manifest live kept' pct clean "$pc_repo" --private-cache --stamp "$version" --apply
    cp "$repo/Directory.Packages.props" "$pc_repo/Directory.Packages.props"
    expect_status 0 'private-cache whole dry-run' pct clean "$pc_repo" --private-cache
    [ -d "$pc_cache" ] && [ -f "$pc_session/nuget-cache" ] || failed 'private-cache whole dry-run mutated cache'
    expect_status 0 'private-cache whole removal' pct clean "$pc_repo" --private-cache --apply
    [ ! -e "$pc_cache" ] && [ ! -e "$pc_session/nuget-cache" ] && [ -d "$pc_shared/cratis.test/1.2.3" ] || failed 'private-cache whole removal scope violated'
    : > "$pc_feed/nuget/Cratis.Test.$version.nupkg"
    pc_opt=1
    expect_status 0 'private-cache environment opt-in' pct use-nuget "$pc_repo" "$version"
    pc_marker || failed 'private-cache environment opt-in missing marker'
    pct restore "$pc_repo" > "$root/output"
    pct clean "$pc_repo" --private-cache --apply > "$root/output"
}

self_test_base_version() {
    local base mode actual status
    for mode in local train; do
        local options=()
        if [ "$mode" = train ]; then options=(--train); fi
        for base in 0.0.0 5.0.0 10.20.300; do
            actual=$(st stamp ${options[@]+"${options[@]}"} --base-version "$base" "$repo") || failed 'self-test: base override'
            case "$actual" in "$base-$mode."*) ;; *) failed "self-test: base override grammar $actual" ;; esac
            valid_stamp "$actual"
            passed=$((passed + 1))
        done
        for base in '' 01.0.0 1.01.0 1.0.01 v5.0.0 5.0 5.0.0.1 5.0.0-preview 5.0.0+build -1.0.0 '5.0.0 '; do
            status=0; st stamp ${options[@]+"${options[@]}"} --base-version "$base" "$repo" > "$root/output" 2>&1 || status=$?
            [ "$status" -eq 2 ] || failed "self-test: invalid base accepted: $base"
            passed=$((passed + 1))
        done
    done
    for mode in missing repeated producer; do
        status=0
        case "$mode" in
            missing) st stamp "$repo" --base-version > "$root/output" 2>&1 || status=$? ;;
            repeated) st stamp --base-version 5.0.0 --base-version 6.0.0 "$repo" > "$root/output" 2>&1 || status=$? ;;
            producer) st stamp --base-version 5.0.0 > "$root/output" 2>&1 || status=$? ;;
        esac
        [ "$status" -eq 2 ] || failed "self-test: base option $mode"
        passed=$((passed + 1))
    done
    actual=$(st stamp "$repo" --base-version 0.0.0 "$repo" 2> "$root/stderr")
    case "$actual" in 0.0.0-local.*) ;; *) failed 'self-test: multi-producer explicit base overrides all tags' ;; esac
    passed=$((passed + 1))
}

self_test_live_clean() {
    local lr=$root/live-repo lf=$root/live-feed ls file mode status
    local lv=5.0.0-local.202601010000.gabcdef0 uv=5.0.0-train.gabcdef1
    mkdir -p "$lr/nested" "$lf/nuget"
    git -C "$lr" init -q
    printf '<Project/>\n' > "$lr/Directory.Packages.props"
    git -C "$lr" add Directory.Packages.props
    git -C "$lr" -c user.email=test@example.invalid -c user.name='Live clean fixture' commit -qm initial
    ls=$(git -C "$lr" rev-parse --absolute-git-dir)/cratis-local
    lct() { env -i HOME="$home" PATH="$system_path" TMPDIR="$TMP" CRATIS_LOCAL_FEED="$lf" NUGET_PACKAGES="$root/live-cache" /bin/bash "$SELF" "$@"; }
    : > "$lf/nuget/Cratis.Live.$lv.nupkg"
    : > "$lf/nuget/Cratis.Unused.$uv.nupkg"
    mkdir -p "$root/live-cache/cratis.live/$lv" "$root/live-cache/cratis.live/5.0.0"
    lct use-nuget "$lr" "$lv" > "$root/output"
    for mode in dry apply; do
        local options=()
        if [ "$mode" = apply ]; then options=(--apply); fi
        status=0; lct clean --stamp "$lv" ${options[@]+"${options[@]}"} > "$root/output" 2> "$root/stderr" || status=$?
        { [ "$status" -eq 2 ] && has "kept stamp $lv" "$root/output" && has "$ls/Directory.Packages.props" "$root/output"; } || failed "self-test: $mode clean must report live overlay"
        [ -f "$lf/nuget/Cratis.Live.$lv.nupkg" ] && [ -f "$ls/env" ] && [ -d "$root/live-cache/cratis.live/$lv" ] || failed 'self-test: live clean mutated subjects'
        passed=$((passed + 2))
    done
    lct restore "$lr" > "$root/output"
    for file in nuget.config Directory.Packages.props nested/packages.lock.json nested/package.json nested/yarn.lock; do
        printf '%s\n' "$lv" > "$lr/$file"
        for mode in dry apply; do
            local options=()
            if [ "$mode" = apply ]; then options=(--apply); fi
            status=0; lct clean --stamp "$lv" ${options[@]+"${options[@]}"} > "$root/output" 2> "$root/stderr" || status=$?
            { [ "$status" -eq 2 ] && has "kept stamp $lv" "$root/output" && has "$lr/$file" "$root/output"; } || failed "self-test: $mode clean missed live $file"
            [ -f "$lf/nuget/Cratis.Live.$lv.nupkg" ] || failed 'self-test: live manifest artifact removed'
            passed=$((passed + 2))
        done
        if [ "$file" = Directory.Packages.props ]; then printf '<Project/>\n' > "$lr/$file"; else rm "$lr/$file"; fi
    done
    printf '%s\n' "$uv" | tr '[:lower:]' '[:upper:]' > "$lr/nested/packages.lock.json"
    for mode in dry apply; do
        local options=()
        if [ "$mode" = apply ]; then options=(--apply); fi
        status=0; lct clean --stamp "$uv" ${options[@]+"${options[@]}"} > "$root/output" 2> "$root/stderr" || status=$?
        { [ "$status" -eq 2 ] && has "kept stamp $uv" "$root/output" && [ -f "$lf/nuget/Cratis.Unused.$uv.nupkg" ]; } || failed 'self-test: case-insensitive train reference protection'
        passed=$((passed + 1))
    done
    printf '%s\n' "$lv" > "$lr/nested/packages.lock.json"
    lct clean --stamp "$uv" --apply > "$root/output" || failed 'self-test: unrelated live stamp blocks unreferenced cleanup'
    [ ! -e "$lf/nuget/Cratis.Unused.$uv.nupkg" ] && [ -f "$lf/nuget/Cratis.Live.$lv.nupkg" ] || failed 'self-test: unreferenced stamp selection'
    rm "$lr/nested/packages.lock.json"
    lct clean --stamp "$lv" > "$root/output"
    has "Cratis.Live.$lv.nupkg" "$root/output" && [ -d "$root/live-cache/cratis.live/$lv" ] || failed 'self-test: unreferenced dry-run'
    lct clean --stamp "$lv" --apply > "$root/output"
    [ ! -e "$lf/nuget/Cratis.Live.$lv.nupkg" ] && [ ! -d "$root/live-cache/cratis.live/$lv" ] && [ -d "$root/live-cache/cratis.live/5.0.0" ] || failed 'self-test: unreferenced per-worktree cleanup scope'
    passed=$((passed + 4))
}

# C fixtures: the printed closure is one plan, in order, and every line is valid shell.
self_test_closure() {
    local name line a b c p last first fa fl fm
    for name in App Lib Mid; do
        mkdir -p "$root/fx-$name"; git -C "$root/fx-$name" init -q
        git -C "$root/fx-$name" config user.email test@example.invalid; git -C "$root/fx-$name" config user.name 'Local package self-test'
        git -C "$root/fx-$name" commit -q --allow-empty -m initial
    done
    fa=$(git -C "$root/fx-App" rev-parse --show-toplevel); fl=$(git -C "$root/fx-Lib" rev-parse --show-toplevel); fm=$(git -C "$root/fx-Mid" rev-parse --show-toplevel)
    expect_status 0 'C1 closure for the fixture plan' fxm closure App="$root/fx-App" Lib="$root/fx-Lib" Mid="$root/fx-Mid"
    cp "$root/output" "$root/closure.out"
    bash -n "$root/closure.out" || failed 'self-test: C2 every printed closure line must be valid shell'
    [ "$(grep -c ' verify-pack ' "$root/closure.out")" -eq 1 ] || failed 'self-test: C1 exactly one verify-pack'
    a=$(grep -n ' verify-pack ' "$root/closure.out" | cut -d: -f1)
    last=$(grep -nE ' pack-(nuget|npm) ' "$root/closure.out" | tail -n 1 | cut -d: -f1)
    first=$(grep -n " use-nuget $fa " "$root/closure.out" | head -n 1 | cut -d: -f1)
    { [ "$a" -gt "$last" ] && [ "$a" -lt "$first" ]; } || failed 'self-test: C1 verify-pack sits after the last pack and before the consumer overlay'
    line=$(grep ' verify-pack ' "$root/closure.out")
    case "$line" in *' --consumer App --producer Lib --producer Mid'*) ;; *) failed 'self-test: C1 verify-pack carries the whole plan' ;; esac
    grep -q "pack-nuget . \"\$STAMP\" --property A=1 --property B=2 Lib.Extra.slnx" "$root/closure.out" || failed 'self-test: C2 the second Lib row carries both properties'
    [ "$(grep -c "pack-nuget \. \"\$STAMP\" Lib.slnx" "$root/closure.out")" -eq 1 ] || failed 'self-test: C2 the first Lib row carries no properties'
    # C4: Mid's overlay, then its pack in a shell that sources the env file, then its restore.
    a=$(grep -n " use-nuget $fm " "$root/closure.out" | cut -d: -f1)
    b=$(grep -n "$fm && \. .*/env && .* pack-nuget " "$root/closure.out" | cut -d: -f1)
    c=$(grep -n " restore $fm" "$root/closure.out" | cut -d: -f1)
    { [ -n "$a" ] && [ -n "$b" ] && [ -n "$c" ] && [ "$a" -lt "$b" ] && [ "$b" -lt "$c" ]; } || failed 'self-test: C4 overlay, sourced pack and restore are in order'
    if grep -q " use-nuget $fl " "$root/closure.out"; then failed 'self-test: C4 Lib has no upstream to overlay'; fi
    passed=$((passed + 6))
    # C2 (real map): Chronicle's three pack properties are separate flags; C3: an invalid plan prints nothing.
    expect_status 0 'C2 real closure Studio <- Chronicle' st closure Studio="$root/fx-App" Chronicle="$root/fx-Lib"
    cp "$root/output" "$root/closure.out"
    bash -n "$root/closure.out" || failed 'self-test: C2 real closure is valid shell'
    p=$(grep 'pack-nuget' "$root/closure.out" | grep -o -- '--property' | wc -l | tr -d ' ')
    [ "$p" -eq 3 ] || failed "self-test: C2 Chronicle carries 3 --property flags (got $p)"
    expect_status 2 'C3 closure for an unplannable pair prints nothing' st closure Stage="$root/fx-App" Screenplay="$root/fx-Lib"
    [ ! -s "$root/output" ] || failed 'self-test: C3 stdout must be empty'
    expect_status 0 'closure --train' st closure --train Studio="$root/fx-App" Chronicle="$root/fx-Lib"
    has 'stamp --train' "$root/output" || failed 'self-test: closure --train prints a train stamp'
    passed=$((passed + 3))
}

# V fixtures: verify-pack and verify-assets against a fixture feed (needs zip, unzip and tar).
# Each fixture starts from the same complete feed so one case cannot hide behind another.
self_test_verify() {
    local vfeed=$root/vfeed s=1.2.4-local.202601010000.gaaaaaaa old=1.2.4-local.200001010000.g1234567 args id
    local lib=(--consumer App --producer Lib --producer Mid)
    local nodedir; nodedir=$(dirname "$(command -v node)")
    fxv() { env -i HOME="$home" PATH="$nodedir:/usr/bin:/bin:/usr/sbin:/sbin" TMPDIR="$TMP" CRATIS_LOCAL_MAP="$fx" CRATIS_LOCAL_FEED="$vfeed" /bin/bash "$SELF" "$@"; }
    if ! command -v zip >/dev/null 2>&1 || ! command -v unzip >/dev/null 2>&1 || ! command -v tar >/dev/null 2>&1 || ! command -v node >/dev/null 2>&1; then
        printf 'SKIP: verify fixtures need zip, unzip, tar and node\n'; skipped=$((skipped + 30)); return 0
    fi
    # mkpkg ID STAMP [DEPENDENCY-ID RANGE]...: a .nupkg (a zip with a nuspec) or, for an @scope/name ID, an npm tarball.
    mkpkg() {
        local pid=$1 stamp=$2 deps='' dir=$root/pk
        shift 2
        mkdir -p "$dir" "$vfeed/nuget" "$vfeed/npm"
        case "$pid" in
            @*) mkdir -p "$dir/package"; printf '{"name":"%s","version":"%s"}\n' "$pid" "$stamp" > "$dir/package/package.json"
                (cd "$dir" && tar -czf "$vfeed/npm/${pid//\//-}-$stamp.tgz" package) ;;
            *) while [ $# -ge 2 ]; do deps="$deps<dependency id=\"$1\" version=\"$2\" />"; shift 2; done
                printf '<package><metadata><id>%s</id><dependencies>%s</dependencies></metadata></package>\n' "$pid" "$deps" > "$dir/$pid.nuspec"
                rm -f "$vfeed/nuget/$pid.$stamp.nupkg"; (cd "$dir" && zip -q "$vfeed/nuget/$pid.$stamp.nupkg" "$pid.nuspec") ;;
        esac
    }
    # mkfeed STAMP [MID-DEPENDENCY-RANGE]: the complete feed of the plan "App Lib Mid".
    mkfeed() {
        rm -rf "$vfeed"
        mkpkg Cratis.Lib "$1"; mkpkg Cratis.Lib.Core "$1"; mkpkg @cratis/lib "$1"
        mkpkg Cratis.Mid "$1" Cratis.Lib "${2:-$1}"
        mkpkg Cratis.Extra "$1"
    }
    # mkassets DIR ID/VERSION...: a restore result that resolves exactly these packages.
    mkassets() {
        local dir=$1 entry sep=''
        shift; mkdir -p "$dir/obj"
        { printf '{\n  "libraries": {\n'
            for entry in "$@"; do printf '%s    "%s": {\n      "type": "package"\n    }' "$sep" "$entry"; sep=$',\n'; done
            printf '\n  }\n}\n'; } > "$dir/obj/project.assets.json"
    }
    # V1: complete feed, plan-mode verify and the exact JSON line.
    mkfeed "$s"
    expect_status 0 'V1 complete feed verifies' fxv verify-pack "$s" "${lib[@]}"
    has 'verify-pack: 4 planned artifacts present' "$root/output" || failed 'self-test: V1 summary'
    fxv verify-pack "$s" "${lib[@]}" --json > "$root/output" 2> "$root/stderr" || failed 'self-test: V1 json'
    printf '{"stamp":"%s","packages":[{"id":"Cratis.Lib","version":"%s"},{"id":"Cratis.Lib.Core","version":"%s"},{"id":"@cratis/lib","version":"%s"},{"id":"Cratis.Mid","version":"%s"}]}\n' "$s" "$s" "$s" "$s" "$s" > "$root/json.expected"
    cmp -s "$root/output" "$root/json.expected" || failed 'self-test: V1 json line grammar'
    passed=$((passed + 2))
    # V2: a cross-producer dependency at the wrong version.
    mkfeed "$s" 1.0.0
    expect_status 1 'V2 wrong-version cross-producer dependency' fxv verify-pack "$s" "${lib[@]}"
    has 'dependency: Cratis.Mid -> Cratis.Lib 1.0.0' "$root/output" || failed 'self-test: V2 names the dependency'
    passed=$((passed + 1))
    # V3: lower bound equal to the stamp passes; a range that merely contains the stamp does not.
    for args in "[$s, )" "$s" "[$s]"; do
        mkfeed "$s" "$args"
        expect_status 0 "V3 dependency range $args" fxv verify-pack "$s" "${lib[@]}"
    done
    mkfeed "$s" "[1.0.0, $s)"
    expect_status 1 'V3 range with the stamp as upper bound' fxv verify-pack "$s" "${lib[@]}"
    # V3b: strict nuspec parsing. mkraw ID STAMP DEPENDENCIES-XML writes the dependencies element verbatim.
    mkraw() {
        local pk=$root/pk; mkdir -p "$pk"
        printf '<package><metadata><id>%s</id><dependencies>%s</dependencies></metadata></package>\n' "$1" "$3" > "$pk/$1.nuspec"
        rm -f "$vfeed/nuget/$1.$2.nupkg"; (cd "$pk" && zip -q "$vfeed/nuget/$1.$2.nupkg" "$1.nuspec")
    }
    mkfeed "$s"; mkraw Cratis.Mid "$s" "<dependency version=\"1.0.0\" id=\"Cratis.Lib\" />"
    expect_status 1 'V3b reversed attributes are read' fxv verify-pack "$s" "${lib[@]}"
    has 'dependency: Cratis.Mid -> Cratis.Lib 1.0.0' "$root/output" || failed 'self-test: V3b reversed attributes name the dependency'
    mkfeed "$s"; mkraw Cratis.Mid "$s" "<dependency version='1.0.0' id='Cratis.Lib' />"
    expect_status 1 'V3b single quotes are read' fxv verify-pack "$s" "${lib[@]}"
    mkfeed "$s"; mkraw Cratis.Mid "$s" "<dependency
      id=\"Cratis.Lib\"
      version=\"1.0.0\"
      exclude=\"Build\" />"
    expect_status 1 'V3b multiline element is read' fxv verify-pack "$s" "${lib[@]}"
    mkfeed "$s"; mkraw Cratis.Mid "$s" "<dependency version='$s' id='Cratis.Lib' exclude='Build'/>"
    expect_status 0 'V3b reversed single-quoted element at the stamp passes' fxv verify-pack "$s" "${lib[@]}"
    for bad in "<dependency id=\"Cratis.Lib\" />" "<dependency version=\"$s\" />" "<dependency id=\"\" version=\"$s\" />" "<dependency id=\"Cratis.Lib\" version=\"$s\""; do
        mkfeed "$s"; mkraw Cratis.Mid "$s" "$bad"
        expect_status 1 "V3b malformed element fails: $bad" fxv verify-pack "$s" "${lib[@]}"
        has 'unparseable dependency: Cratis.Mid' "$root/output" || failed 'self-test: V3b malformed element is named'
    done
    for args in "($s,)" "($s, )" "[$s, $s]" "[$s,]" "[$s, 9.9.9)" "$s-x" "[ $s ]x"; do
        mkfeed "$s" "$args"
        expect_status 1 "V3b dependency range rejected: $args" fxv verify-pack "$s" "${lib[@]}"
    done
    # V4: a dependency outside the plan is ignored.
    mkfeed "$s"; mkpkg Cratis.Mid "$s" Cratis.Lib "$s" Cratis.Other 1.0.0
    expect_status 0 'V4 unplanned dependency is ignored' fxv verify-pack "$s" "${lib[@]}"
    # V5: a missing planned artifact is not satisfied by another stamp.
    mkfeed "$s"; rm "$vfeed/nuget/Cratis.Lib.Core.$s.nupkg"; mkpkg Cratis.Lib.Core "$old"
    expect_status 1 'V5 missing planned artifact' fxv verify-pack "$s" "${lib[@]}"
    has "missing: Cratis.Lib.Core $s (Lib nuget)" "$root/output" || failed 'self-test: V5 names the artifact'
    mkassets "$root/cons" "Cratis.Lib/$s" "Cratis.Lib.Core/$s" "Cratis.Mid/$s"
    expect_status 1 'V5 verify-assets plan mode also fails on the missing artifact' fxv verify-assets "$root/cons" "$s" "${lib[@]}"
    passed=$((passed + 2))
    # V6: a real NuGet-only plan never demands the npm tarballs of a dual-ecosystem producer.
    rm -rf "$vfeed"
    while IFS= read -r id; do mkpkg "$id" "$s"; done < <(awk -F'\t' '$1 == "ids" && $2 == "Arc" && $3 == "nuget" { n = split($4, a, ","); for (i = 1; i <= n; i++) print a[i] }' "$(dirname "$SELF")/dependency-map.tsv")
    expect_status 0 'V6 real Arc NuGet plan without npm tarballs' env -i HOME="$home" PATH=/usr/bin:/bin TMPDIR="$TMP" CRATIS_LOCAL_FEED="$vfeed" /bin/bash "$SELF" verify-pack "$s" --consumer Stage --producer Arc
    # V7: stale assets (an older local stamp) name the ID, version and path.
    mkfeed "$s"
    mkassets "$root/cons" "Cratis.Lib/$old" "Cratis.Lib.Core/$s" "Cratis.Mid/$s"
    expect_status 1 'V7 stale local stamp' fxv verify-assets "$root/cons" "$s" "${lib[@]}"
    has "Cratis.Lib $old $root/cons/obj/project.assets.json" "$root/output" || failed 'self-test: V7 names id, version and path'
    passed=$((passed + 1))
    # V8: a train stamp changes with a commit in a non-last producer, so the old feed is stale.
    local t1 t2
    t1=$(st stamp --train "$repo" "$repo2" 2> /dev/null); mkfeed "$t1"
    expect_status 0 'V8 train feed verifies at its own stamp' fxv verify-pack "$t1" "${lib[@]}"
    git -C "$repo" commit -q --allow-empty -m 'non-last producer change'
    t2=$(st stamp --train "$repo" "$repo2" 2> /dev/null)
    [ "$t1" != "$t2" ] || failed 'self-test: V8 stamp changes with a non-last producer commit'
    expect_status 1 'V8 stale train stamp' fxv verify-pack "$t2" "${lib[@]}"
    has "missing: Cratis.Lib $t2" "$root/output" || failed 'self-test: V8 missing at the new train stamp'
    passed=$((passed + 2))
    # V9: a second, stale installed copy of an npm package.
    mkfeed "$s"
    mkdir -p "$root/npmcons/node_modules/@cratis/lib"
    printf '{"name":"@cratis/lib","version":"%s"}\n' "$s" > "$root/npmcons/node_modules/@cratis/lib/package.json"
    expect_status 0 'V9 one installed copy at the stamp' fxv verify-assets "$root/npmcons" "$s" --npm "${lib[@]}"
    mkdir -p "$root/npmcons/node_modules/x/node_modules/@cratis/lib"
    printf '{"name":"@cratis/lib","version":"1.0.0"}\n' > "$root/npmcons/node_modules/x/node_modules/@cratis/lib/package.json"
    expect_status 1 'V9 stale second npm copy' fxv verify-assets "$root/npmcons" "$s" --npm "${lib[@]}"
    has "@cratis/lib 1.0.0" "$root/output" || failed 'self-test: V9 names the stale copy'
    expect_status 1 'V9 feed-wide npm mode flags the same copy' fxv verify-assets "$root/npmcons" "$s" --npm
    passed=$((passed + 1))
    # V10: a named producer the consumer never resolved.
    mkfeed "$s"
    mkassets "$root/cons" "Cratis.Mid/$s" "Cratis.Extra/$s"
    expect_status 1 'V10 unconsumed producer' fxv verify-assets "$root/cons" "$s" --consumer App --producer Lib --producer Mid --producer Extra
    has 'unconsumed: Lib (nuget)' "$root/output" || failed 'self-test: V10 names the unconsumed producer'
    mkassets "$root/cons" "Cratis.Mid/$s" "Cratis.Extra/$s" "Cratis.Lib/$s"
    expect_status 0 'V10 one consumed ID is enough' fxv verify-assets "$root/cons" "$s" --consumer App --producer Lib --producer Mid --producer Extra
    passed=$((passed + 1))
    # V11: an unrelated package at the stamp is ignored in plan mode and flagged by the feed-wide mode.
    mkfeed "$s"; mkpkg Cratis.Unrelated "$s"
    mkassets "$root/cons" "Cratis.Lib/$s" "Cratis.Mid/$s" "Cratis.Unrelated/9.9.9"
    expect_status 0 'V11 plan mode ignores an unrelated package' fxv verify-assets "$root/cons" "$s" "${lib[@]}"
    expect_status 1 'V11 feed-wide mode flags it' fxv verify-assets "$root/cons" "$s"
    # V12: --json lists only what the consumer resolved, whatever else the feed holds.
    mkassets "$root/cons" "Cratis.Lib/$s" "Cratis.Mid/$s"
    fxv verify-assets "$root/cons" "$s" "${lib[@]}" --json > "$root/output" 2> "$root/stderr" || failed 'self-test: V12 json'
    printf '{"stamp":"%s","packages":[{"id":"Cratis.Lib","version":"%s"},{"id":"Cratis.Mid","version":"%s"}]}\n' "$s" "$s" "$s" > "$root/json.expected"
    cmp -s "$root/output" "$root/json.expected" || failed 'self-test: V12 json lists only consumer resolutions'
    passed=$((passed + 1))
    # V15: unreadable evidence is a failure, never a skip: broken assets JSON, a package.json without a version,
    # and a tarball whose package.json has no name. A nested "version" key must not stand in for the top-level one.
    mkdir -p "$root/badassets/obj"; printf '{"libraries": {"Cratis.Lib/%s": {"type": "package"' "$s" > "$root/badassets/obj/project.assets.json"
    expect_status 2 'V15 truncated project.assets.json' fxv verify-assets "$root/badassets" "$s" "${lib[@]}"
    printf '{"version": 3}\n' > "$root/badassets/obj/project.assets.json"
    expect_status 2 'V15 assets without libraries' fxv verify-assets "$root/badassets" "$s" "${lib[@]}"
    mkfeed "$s"; rm -rf "$root/npmbad"; mkdir -p "$root/npmbad/node_modules/@cratis/lib"
    printf '{"name":"@cratis/lib"}\n' > "$root/npmbad/node_modules/@cratis/lib/package.json"
    expect_status 2 'V15 installed package.json without a version' fxv verify-assets "$root/npmbad" "$s" --npm "${lib[@]}"
    printf '{"name":"@cratis/lib","dependencies":{"x":{"version":"%s"}},"version":"1.0.0"}\n' "$s" > "$root/npmbad/node_modules/@cratis/lib/package.json"
    expect_status 1 'V15 nested version does not hide the real one' fxv verify-assets "$root/npmbad" "$s" --npm "${lib[@]}"
    # V13/V14: cannot-run cases exit 2.
    expect_status 2 'V13 no edge in that ecosystem' fxv verify-assets "$root/cons" "$s" --npm --consumer Mid --producer Lib
    mkdir -p "$root/noassets"
    expect_status 2 'V13 no project.assets.json' fxv verify-assets "$root/noassets" "$s" "${lib[@]}"
    expect_status 2 'V14 verify-pack --consumer without --producer' fxv verify-pack "$s" --consumer App
    expect_status 2 'V14 verify-pack --producer without --consumer' fxv verify-pack "$s" --producer Lib
    expect_status 2 'V14 verify-assets --consumer without --producer' fxv verify-assets "$root/cons" "$s" --consumer App
    expect_status 2 'V14 verify-assets --producer without --consumer' fxv verify-assets "$root/cons" "$s" --producer Lib
    # Manual forms stay available.
    expect_status 0 'name-mode verify-pack' fxv verify-pack "$s" Lib --ecosystem nuget
    expect_status 2 'name-mode verify-pack without ids' fxv verify-pack "$s" Nod
}

# Fixture matrix for the plan, closure and verify commands. Runs inside self_test and uses its
# root/home/feed/st/passed/skipped (dynamic scope). `fx` is the fixture map, `real` the shipped one.
# expect_status WANT DESCRIPTION COMMAND...: run, capture stdout/stderr in $root/output and $root/stderr, require the exit code.
expect_status() {
    local want=$1 desc=$2 got=0
    shift 2
    printf 'self-test [%ss]: %s\n' "$SECONDS" "$desc" >&2
    "$@" > "$root/output" 2> "$root/stderr" || got=$?
    if [ "$got" -ne "$want" ]; then
        cat "$root/stderr" >&2
        failed "self-test: $desc (exit $got, wanted $want)"
    fi
    passed=$((passed + 1))
}
tsv() { local IFS=$'\t'; printf '%s\n' "$*"; }
self_test_matrix() {
    local fx=$root/fx.tsv status
    fxm() { env -i HOME="$home" PATH=/usr/bin:/bin:/usr/sbin:/sbin TMPDIR="$TMP" CRATIS_LOCAL_MAP="$fx" /bin/bash "$SELF" "$@"; }
    {
        echo '# fixture map'
        tsv pack Lib nuget Lib.slnx
        tsv pack Lib nuget Lib.Extra.slnx A=1,B=2
        tsv ids Lib nuget Cratis.Lib,Cratis.Lib.Core
        tsv pack Lib npm .
        tsv ids Lib npm @cratis/lib
        tsv pack Mid nuget Mid.slnx
        tsv ids Mid nuget Cratis.Mid
        tsv pack Extra nuget Extra.slnx
        tsv ids Extra nuget Cratis.Extra
        tsv pack Nod nuget Nod.slnx
        tsv uses Mid Lib nuget Directory.Packages.props
        tsv uses App Mid nuget Directory.Packages.props
        tsv uses App Lib nuget Directory.Packages.props
        tsv uses App Extra nuget Directory.Packages.props
        tsv uses App Lib npm .
        tsv uses App Orl nuget Directory.Packages.props
        tsv uses Orl Lib nuget Directory.Packages.props
        tsv uses App Nod nuget Directory.Packages.props
    } > "$fx"
    # P1: exact plan rows and the unnamed-hop warning on stderr only.
    {
        tsv pack Lib nuget; tsv pack Lib npm; tsv pack Mid nuget; tsv overlay Mid Lib
        tsv consume Lib nuget Directory.Packages.props; tsv consume Lib npm .; tsv consume Mid nuget Directory.Packages.props
    } > "$root/p1.expected"
    fxm plan App Lib Mid > "$root/output" 2> "$root/stderr" || failed 'self-test: P1 plan'
    cmp -s "$root/output" "$root/p1.expected" || failed 'self-test: P1 plan rows differ from the expected plan'
    has 'Lib -> Orl -> App' "$root/stderr" || { cat "$root/stderr" >&2; failed 'self-test: P1 unnamed-hop warning'; }
    if has 'WARNING' "$root/output"; then failed 'self-test: P1 warning must stay on stderr'; fi
    # P2/P3: the real map. An NuGet-only edge plans only the NuGet ecosystem of a dual-ecosystem producer;
    # npm-only ids on a NuGet edge cannot be planned. P3 must move to the fixture map if Screenplay ships NuGet ids.
    st plan Stage Arc > "$root/output" 2> "$root/stderr" || failed 'self-test: P2 real plan'
    { [ "$(grep -c '^pack' "$root/output")" -eq 1 ] && has "$(tsv pack Arc nuget)" "$root/output"; } || failed 'self-test: P2 plans exactly pack Arc nuget'
    status=0; st plan Stage Screenplay > "$root/output" 2> "$root/stderr" || status=$?
    { [ "$status" -eq 2 ] && [ ! -s "$root/output" ] && has 'Screenplay (nuget)' "$root/stderr"; } || failed 'self-test: P3 npm-only ids on a NuGet edge'
    passed=$((passed + 3))
    # P4-P7: invalid inputs exit 2 with nothing on stdout (unknown, repeated, consumer as producer, wrong order, no edge, no ids, no producer).
    local bad
    for bad in 'App Nope' 'App Lib Lib' 'App Lib App' 'App Mid Lib' 'Mid Extra' 'App Nod' 'App'; do
        status=0
        # shellcheck disable=SC2086
        fxm plan $bad > "$root/output" 2> "$root/stderr" || status=$?
        { [ "$status" -eq 2 ] && [ ! -s "$root/output" ]; } || failed "self-test: plan $bad must exit 2 with empty stdout (got $status)"
        passed=$((passed + 1))
    done
    # P8: a malformed row names its line.
    { cat "$fx"; printf 'uses\tApp\tLib\n'; } > "$root/bad.tsv"
    status=0; fx=$root/bad.tsv; fxm plan App Lib > "$root/output" 2> "$root/stderr" || status=$?
    fx=$root/fx.tsv
    { [ "$status" -eq 2 ] && has "bad.tsv:$(($(wc -l < "$root/bad.tsv"))): malformed" "$root/stderr"; } || failed 'self-test: P8 malformed map row'
    passed=$((passed + 1))
    # Pack properties are separate -p: arguments of the one command, never an environment variable.
    mkdir -p "$root/bin"; printf '#!/bin/sh\nexit 0\n' > "$root/bin/dotnet"; chmod +x "$root/bin/dotnet"
    env -i HOME="$home" PATH="$root/bin:/usr/bin:/bin" TMPDIR="$TMP" /bin/bash "$SELF" --dry-run pack-nuget "$repo" "$version" --property A=1 --property B=2 Lib.slnx > "$root/output" || failed 'self-test: pack-nuget --property dry run'
    { has ' -p:A=1 -p:B=2 ' "$root/output" && ! has CRATIS_LOCAL_PACK_PROPERTIES "$root/output"; } || failed 'self-test: pack-nuget prints separate -p: tokens'
    status=0; env -i HOME="$home" PATH="$root/bin:/usr/bin:/bin" TMPDIR="$TMP" /bin/bash "$SELF" --dry-run pack-nuget "$repo" "$version" --property 'A=1;B' Lib.slnx > "$root/output" 2>&1 || status=$?
    [ "$status" -eq 2 ] || failed 'self-test: pack-nuget rejects an invalid property'
    status=0; env -i HOME="$home" PATH="$root/bin:/usr/bin:/bin" TMPDIR="$TMP" /bin/bash "$SELF" --dry-run pack-nuget "$repo" "$version" --property A=1 > "$root/output" 2>&1 || status=$?
    [ "$status" -eq 2 ] || failed 'self-test: pack-nuget needs a target after the properties'
    passed=$((passed + 3))
    self_test_closure
    self_test_verify
    self_test_symlink_feed
    self_test_composition
}

# Registry paths allow system/configured symlink parents of the feed.
self_test_symlink_feed() {
    local physical=$root/feed-physical-parent alias=$root/feed-symlink-parent
    local sr=$root/feed-symlink-repo sv=1.2.3-local.202610240544.gabcdef0
    mkdir -p "$physical" "$sr"
    ln -s "$physical" "$alias"
    git -C "$sr" init -q
    printf '.ai-work/\n' > "$sr/.gitignore"
    printf '<Project><ItemGroup><PackageVersion Include="Cratis.Symlink" Version="1.2.3"/></ItemGroup></Project>\n' > "$sr/Directory.Packages.props"
    git -C "$sr" add .gitignore Directory.Packages.props
    git -C "$sr" -c user.email=test@example.invalid -c user.name='Symlink feed fixture' commit -qm initial
    slf() { env -i HOME="$home" PATH="$system_path" TMPDIR="$TMP" CRATIS_LOCAL_FEED="$alias/missing/feed" CRATIS_LOCAL_SESSION_DIR="$sr/.ai-work/local-packages" /bin/bash "$SELF" "$@"; }
    expect_status 0 'symlink feed resolution does not create a missing feed' slf check "$sr"
    [ ! -e "$physical/missing" ] || failed 'symlink feed resolution created directories'
    mkdir -p "$physical/missing/feed/nuget"
    : > "$physical/missing/feed/nuget/Cratis.Symlink.$sv.nupkg"
    expect_status 0 'legacy use-nuget under symlinked feed parent' slf use-nuget "$sr" "$sv" Cratis.Symlink
    has "$physical/missing/feed/nuget" "$sr/.ai-work/local-packages/env" || failed 'legacy feed path was not resolved physically'
    expect_status 0 'legacy restore under symlinked feed parent' slf restore "$sr"
    has '0 leaks' "$root/output" || failed 'symlink feed restore leaked a local pin'
    expect_status 0 'legacy clean under symlinked feed parent' slf clean --stamp "$sv" --apply
    [ ! -e "$physical/missing/feed/nuget/Cratis.Symlink.$sv.nupkg" ] || failed 'symlink feed clean left the artifact'
    passed=$((passed + 4))
}

# Composition fixtures are isolated, SDK-native and never restore/build a producer.
self_test_composition() {
    local cf=$root/composition-parent/feed cr=$root/composition-repo cm=$root/composition.tsv
    local a=22.138.1-local.202610240544.gd1ad215 b=0.2.2-local.202610240544.g91916a3 c=0.1.18-local.202610240544.g524bdce
    local boundary kind base=$root/composition-prior cpkg=$root/composition-package
    if ! command -v node >/dev/null 2>&1 || ! command -v dotnet >/dev/null 2>&1 || ! command -v zip >/dev/null 2>&1 || ! command -v unzip >/dev/null 2>&1; then
        printf 'SKIP: composition fixtures require node, dotnet SDK, zip and unzip\n'; skipped=$((skipped + 1)); return 0
    fi
    mkdir -p "$root/composition-physical-parent"
    ln -s "$root/composition-physical-parent" "$root/composition-parent"
    mkdir -p "$cf/nuget" "$cr" "$cpkg"
    : > "$root/.composition-fixture"
    git -C "$cr" init -q
    printf '.ai-work/\nobj/\n' > "$cr/.gitignore"
    printf '<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup><PackageVersion Include="Cratis.Keep" Version="1.2.3"/></ItemGroup></Project>\n' > "$cr/Directory.Packages.props"
    mkdir -p "$cr/nested"
    printf '<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup></Project>\n' > "$cr/nested/Directory.Packages.props"
    printf '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Cratis.Stage" Version="0.1.0" /></ItemGroup></Project>\n' > "$cr/nested/OptOut.csproj"
    printf '<configuration><packageSources><clear/><add key="remote" value="https://example.invalid/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="remote"><package pattern="*"/><package pattern="Cratis.*"/><package pattern="Cratis.Screenplay*"/><package pattern="Cratis.Screenplay"/></packageSource></packageSourceMapping></configuration>\n' > "$cr/nuget.config"
    git -C "$cr" add .gitignore Directory.Packages.props nuget.config nested
    git -C "$cr" -c user.email=test@example.invalid -c user.name='Composition fixture' commit -qm initial
    cp "$cr/Directory.Packages.props" "$root/composition-original.props"
    printf 'producer\tChronicle\t%s\t%s\nproducer\tScreenplay\t%s\t%s\nproducer\tStage\t%s\t%s\npackage\tChronicle\tCratis.Chronicle.InProcess\npackage\tScreenplay\tCratis.Screenplay\npackage\tStage\tCratis.Stage\n' "$a" "$cr" "$b" "$cr" "$c" "$cr" > "$cm"
    cst() { env -i HOME="$home" PATH="$PATH" TMPDIR="$TMP" DOTNET_CLI_HOME="$home" DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 PI_PHASE_ORIGINAL_PATH="${PI_PHASE_ORIGINAL_PATH:-}" PI_PHASE_SHIM_DIR="${PI_PHASE_SHIM_DIR:-}" PI_PHASE_MSBUILD_MAX_CPU="${PI_PHASE_MSBUILD_MAX_CPU:-1}" CRATIS_LOCAL_FEED="$cf" CRATIS_LOCAL_SESSION_DIR="$cr/.ai-work/local-packages" CRATIS_LOCAL_COMPOSITION_TEST_ROOT="$root" CRATIS_LOCAL_COMPOSITION_FAIL="${composition_fail:-}" CRATIS_LOCAL_COMPOSITION_INTERRUPT="${composition_interrupt:-}" /bin/bash "$SELF" "$@"; }
    cmpkg() {
        local id=$1 ver=$2 dependencies=${3:-} xmlfile=$cpkg/$1.nuspec
        printf '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>%s</id><version>%s</version><dependencies>%s</dependencies></metadata></package>' "$id" "$ver" "$dependencies" > "$xmlfile"
        rm -f -- "$cf/nuget/$id.$ver.nupkg"
        (cd "$cpkg" && zip -q "$cf/nuget/$id.$ver.nupkg" "$id.nuspec")
    }
    cmpkg Cratis.Chronicle.InProcess "$a"
    cmpkg Cratis.Screenplay "$b" '<group targetFramework="net10.0"><dependency id="Cratis.Chronicle.InProcess" version="[22.136.4,23.0.0)"/></group>'
    cmpkg Cratis.Stage "$c" "<dependency id=\"Cratis.Screenplay\" version=\"[$b]\"/>"
    expect_status 0 'composition native mixed-version pack' cst verify-pack --composition "$cm" --json
    node - "$root/output" "$a" "$b" "$c" <<'JSON'
const fs = require('fs'); const [file,...versions] = process.argv.slice(2), data = JSON.parse(fs.readFileSync(file));
if (data.packages.length !== 3 || !versions.every(v => data.packages.some(p => p.version === v && p.producer))) process.exit(1);
JSON
    passed=$((passed + 1))
    mkdir -p "$root/no-sdk"
    printf '#!/bin/sh\nexit 127\n' > "$root/no-sdk/dotnet"; chmod +x "$root/no-sdk/dotnet"
    expect_status 2 'composition unavailable native SDK fails closed' env -i HOME="$home" PATH="$root/no-sdk:$PATH" TMPDIR="$TMP" CRATIS_LOCAL_FEED="$cf" /bin/bash "$SELF" verify-pack --composition "$cm"
    mkdir -p "$root/composition-foreign-nuget" "$root/composition-foreign-lock"
    printf 'keep foreign lock state\n' > "$root/composition-foreign-lock/sentinel"
    mv "$cf/nuget" "$root/composition-original-nuget"
    ln -s "$root/composition-foreign-nuget" "$cf/nuget"
    expect_status 2 'composition rejects helper-owned nuget symlink' cst verify-pack --composition "$cm"
    [ -d "$root/composition-foreign-nuget" ] || failed 'composition removed foreign nuget directory'
    rm "$cf/nuget"; mv "$root/composition-original-nuget" "$cf/nuget"
    ln -s "$root/composition-foreign-lock" "$cf/.composition-lock"
    expect_status 2 'composition rejects helper-owned lock symlink' cst use-nuget "$cr" --composition "$cm"
    has 'keep foreign lock state' "$root/composition-foreign-lock/sentinel" || failed 'composition removed foreign lock state'
    rm "$cf/.composition-lock"
    passed=$((passed + 2))
    cp "$cm" "$root/composition-good.tsv"
    printf 'package\tStage\tcratis.screenplay\n' >> "$cm"
    expect_status 2 'composition duplicate case-insensitive owner' cst verify-pack --composition "$cm"
    cp "$root/composition-good.tsv" "$cm"
    expect_status 2 'composition flags cannot mix legacy stamp' cst verify-pack "$a" --composition "$cm"
    expect_status 2 'composition activation cannot mix legacy IDs' cst use-nuget "$cr" --composition "$cm" Cratis.Stage
    printf 'producer\tBad\t1.2.3\t%s\npackage\tBad\tBad\n' "$cr" > "$root/composition-bad.tsv"
    expect_status 2 'composition rejects nonlocal producer stamp' cst verify-pack --composition "$root/composition-bad.tsv"
    cmpkg Cratis.Screenplay "$b" '<group><dependency id="Cratis.Chronicle.InProcess" version="[22.0.0,22.1.0)"/></group>'
    expect_status 1 'composition incompatible native group range' cst verify-pack --composition "$cm"
    cmpkg Cratis.Screenplay "$b" '<dependency id="Cratis.Chronicle.InProcess" version="[broken"/>'
    expect_status 1 'composition malformed native range' cst verify-pack --composition "$cm"
    cmpkg Cratis.Screenplay "$b" "<dependency id=\"Cratis.Undeclared\" version=\"[$a]\"/>"
    expect_status 1 'composition undeclared local nuspec edge' cst verify-pack --composition "$cm"
    cmpkg Cratis.Screenplay "$b" '<group><dependency id="Cratis.Chronicle.InProcess" version="[22.136.4,23.0.0)"/></group>'
    cp "$cf/nuget/Cratis.Stage.$c.nupkg" "$root/stage.nupkg"
    cmpkg Cratis.Stage "$c"; printf '<package><metadata>' > "$cpkg/Cratis.Stage.nuspec"
    (cd "$cpkg" && zip -q "$cf/nuget/Cratis.Stage.$c.nupkg" Cratis.Stage.nuspec)
    expect_status 1 'composition malformed XML' cst verify-pack --composition "$cm"
    cp "$root/stage.nupkg" "$cf/nuget/Cratis.Stage.$c.nupkg"
    mv "$cf/nuget/Cratis.Stage.$c.nupkg" "$root/hidden-stage.nupkg"
    expect_status 1 'composition missing package' cst verify-pack --composition "$cm"
    mv "$root/hidden-stage.nupkg" "$cf/nuget/Cratis.Stage.$c.nupkg"
    mkdir -p "$cr/obj"
    cat > "$cr/obj/project.assets.json" <<JSON
{"libraries":{"Cratis.Chronicle.InProcess/$a":{"type":"package"},"Cratis.Screenplay/$b":{"type":"package"},"Cratis.Stage/$c":{"type":"package"}},"targets":{"net10.0":{"Cratis.Chronicle.InProcess/$a":{"type":"package"},"Cratis.Screenplay/$b":{"type":"package","dependencies":{"Cratis.Chronicle.InProcess":"[22.136.4,23.0.0)"}},"Cratis.Stage/$c":{"type":"package","dependencies":{"Cratis.Screenplay":"[$b]"}}}}}
JSON
    cp "$cr/obj/project.assets.json" "$root/composition-assets.json"
    expect_status 0 'composition exact assets graph' cst verify-assets "$cr" --composition "$cm" --json
    node - "$root/composition-assets.json" "$cr/obj/project.assets.json" <<'JSON'
const fs=require('fs'), [input,output]=process.argv.slice(2), data=JSON.parse(fs.readFileSync(input));
const target=data.targets['net10.0']; delete target[Object.keys(target).find(k=>k.startsWith('Cratis.Chronicle'))];
fs.writeFileSync(output,JSON.stringify(data));
JSON
    expect_status 1 'composition missing consumed local dependency' cst verify-assets "$cr" --composition "$cm"
    node - "$root/composition-assets.json" "$cr/obj/project.assets.json" <<'JSON'
const fs=require('fs'), [input,output]=process.argv.slice(2), data=JSON.parse(fs.readFileSync(input));
for (const graph of [data.libraries,data.targets['net10.0']]) delete graph[Object.keys(graph).find(k=>k.startsWith('Cratis.Chronicle'))];
const target=data.targets['net10.0']; target[Object.keys(target).find(k=>k.startsWith('Cratis.Screenplay'))].dependencies={};
fs.writeFileSync(output,JSON.stringify(data));
JSON
    expect_status 1 'composition nuspec local dependency absent from graph' cst verify-assets "$cr" --composition "$cm"
    cmpkg Cratis.Screenplay "$b" '<group targetFramework="net8.0"><dependency id="Cratis.Chronicle.InProcess" version="[22.136.4,23.0.0)"/></group><group targetFramework="net10.0"/>'
    expect_status 0 'composition native nearest group allows unused owned ID' cst verify-assets "$cr" --composition "$cm"
    cmpkg Cratis.Screenplay "$b" '<group><dependency id="Cratis.Chronicle.InProcess" version="[22.136.4,23.0.0)"/></group>'
    node - "$root/composition-assets.json" "$cr/obj/project.assets.json" "$a" <<'JSON'
const fs=require('fs'), [input,output,a]=process.argv.slice(2), data=JSON.parse(fs.readFileSync(input));
data.targets.net9=data.targets['net10.0']; data.targets.net9['Cratis.Chronicle.InProcess/'+a].dependencies={'Cratis.Stage':'[1.0.0,2.0.0)'};
fs.writeFileSync(output,JSON.stringify(data));
JSON
    expect_status 1 'composition bad edge in another target' cst verify-assets "$cr" --composition "$cm"
    node - "$root/composition-assets.json" "$cr/obj/project.assets.json" <<'JSON'
const fs=require('fs'), [input,output]=process.argv.slice(2), data=JSON.parse(fs.readFileSync(input)), target=data.targets['net10.0'];
Object.assign(data.libraries,{'Other.A/1.0.0':{type:'package'},'Other.B/2.0.0':{type:'package'}});
Object.assign(target,{'Other.A/1.0.0':{type:'package',dependencies:{'Other.B':'[1.0.0]'}},'Other.B/2.0.0':{type:'package'}});
fs.writeFileSync(output,JSON.stringify(data));
JSON
    expect_status 0 'composition range conflict outside the composition is a warning' cst verify-assets "$cr" --composition "$cm" --json
    has 'WARNING: dependency range outside the composition' "$root/stderr" || failed 'composition outside-range warning on stderr'
    node -e 'const d=JSON.parse(require("fs").readFileSync(process.argv[1]));if(d.warnings.length!==1||!/Other\.A -> Other\.B/.test(d.warnings[0]))process.exit(1)' "$root/output" || failed 'composition outside-range warning in JSON'
    node - "$root/composition-assets.json" "$cr/obj/project.assets.json" "$c" <<'JSON'
const fs=require('fs'), [input,output,c]=process.argv.slice(2), data=JSON.parse(fs.readFileSync(input)), target=data.targets['net10.0'];
data.libraries['Other.B/2.0.0']={type:'package'}; target['Other.B/2.0.0']={type:'package'};
target['Cratis.Stage/'+c].dependencies['Other.B']='[1.0.0]';
fs.writeFileSync(output,JSON.stringify(data));
JSON
    expect_status 1 'composition range conflict on a composed edge is refused' cst verify-assets "$cr" --composition "$cm"
    node - "$root/composition-assets.json" "$cr/obj/project.assets.json" <<'JSON'
const fs=require('fs'), [input,output]=process.argv.slice(2), data=JSON.parse(fs.readFileSync(input));
data.targets.net9=JSON.parse(JSON.stringify(data.targets['net10.0']));
Object.assign(data.libraries,{'Other.C/1.0.0':{type:'package'},'Other.C/2.0.0':{type:'package'}});
data.targets.net9['Other.C/1.0.0']={type:'package'}; data.targets['net10.0']['Other.C/2.0.0']={type:'package'};
fs.writeFileSync(output,JSON.stringify(data));
JSON
    expect_status 0 'composition multitarget assets with one released ID at two versions' cst verify-assets "$cr" --composition "$cm"
    passed=$((passed + 3))
    sed "s/$a/22.136.4/g" "$root/composition-assets.json" > "$cr/obj/project.assets.json"
    expect_status 1 'composition published fallback version' cst verify-assets "$cr" --composition "$cm"
    printf '{"libraries":{},"targets":{"net10.0":{}}}\n' > "$cr/obj/project.assets.json"
    expect_status 1 'composition zero consumption' cst verify-assets "$cr" --composition "$cm"
    printf '{broken\n' > "$cr/obj/project.assets.json"
    expect_status 1 'composition malformed assets' cst verify-assets "$cr" --composition "$cm"
    cp "$root/composition-assets.json" "$cr/obj/project.assets.json"
    expect_status 0 'composition activation with transitive pinning' cst use-nuget "$cr" --composition "$cm" --pin-transitive
    composition_registry() {
        node - "$cf" "$cr" "$a" "$b" "$c" <<'NODE'
const fs = require('node:fs'), path = require('node:path'); const [feed,repo,...stamps] = process.argv.slice(2);
for (const stamp of stamps) {
    const r = JSON.parse(fs.readFileSync(path.join(feed,'stamps',stamp,'record.json')));
    if (r.schema !== 1 || r.consumers.length !== 1 || r.consumers[0].worktree !== repo || r.consumers[0].ecosystem !== 'nuget') process.exit(1);
}
NODE
    }
    expect_status 0 'registry composition consumer' composition_registry
    output=$cr/.ai-work/local-packages/composition
    grep -Fq 'CentralPackageTransitivePinningEnabled>true' "$output/Directory.Packages.props" || failed 'composition pin property'
    self_test_nuget_overlay "$output" Cratis.Stage "$c"
    passed=$((passed + 8))
    # Only exact owned IDs are removed remotely; broader patterns remain remote.
    [ "$(grep -c 'pattern="Cratis.Screenplay"' "$output/nuget.config")" -eq 1 ] || failed 'composition exclusive exact mapping'
    has 'pattern="*"' "$output/nuget.config" || failed 'composition removed remote wildcard'
    has 'pattern="Cratis.*"' "$output/nuget.config" || failed 'composition removed broader remote prefix'
    has 'pattern="Cratis.Screenplay*"' "$output/nuget.config" || failed 'composition removed overlapping remote prefix'
    has "$root/composition-physical-parent/feed/nuget" "$output/env" || failed 'composition feed path was not resolved physically'
    passed=$((passed + 4))
    cp "$output/Directory.Packages.props" "$root/composition-overlay.props"
    expect_status 0 'composition identical activation is a no-op' cst use-nuget "$cr" --composition "$cm" --pin-transitive
    cmp -s "$output/Directory.Packages.props" "$root/composition-overlay.props" || failed 'composition repeated activation drift'
    expect_status 2 'composition changed pinning refuses reactivation' cst use-nuget "$cr" --composition "$cm"
    expect_status 2 'composition rejects legacy activation while active' cst use-nuget "$cr" "$a" Cratis.Chronicle.InProcess
    expect_status 2 'composition active artifacts retained by clean' cst clean --stamp "$a" --apply
    expect_status 2 'composition refuses legacy npm activation' cst use-npm "$cr" "$b"
    cp "$output/env" "$root/composition-env"
    printf '# unrelated session edit\n' >> "$output/env"
    expect_status 2 'composition edited overlay retained on restore' cst restore "$cr"
    grep -Fq 'unrelated session edit' "$output/env" || failed 'composition deleted edited overlay'
    cp "$root/composition-env" "$output/env"
    cp "$output/cratis-local.targets" "$root/composition-targets"
    printf '<!-- unrelated targets edit -->\n' >> "$output/cratis-local.targets"
    expect_status 2 'composition edited targets retained on restore' cst restore "$cr"
    has 'unrelated targets edit' "$output/cratis-local.targets" || failed 'composition deleted edited targets'
    cp "$root/composition-targets" "$output/cratis-local.targets"
    printf '<!-- consumer edit -->\n' >> "$cr/Directory.Packages.props"
    cp "$cr/Directory.Packages.props" "$root/composition-original.props"
    expect_status 0 'composition restores preserving consumer edits' cst restore "$cr"
    has '0 leaks' "$root/output" || failed 'composition restore did not run check'
    expect_status 0 'composition repeated restore' cst restore "$cr"
    [ ! -e "$output" ] || failed 'composition overlay survives restore'
    cmp -s "$cr/Directory.Packages.props" "$root/composition-original.props" || failed 'composition consumer props mutated'
    # Whole-cache cleanup also cleans feed artifacts. Keep this fixture's feed
    # separate so the recovery fixtures below retain their declared packages.
    local composition_feed=$cf composition_cache
    cf=$root/private-composition-feed
    mkdir -p "$cf/nuget"
    cp "$composition_feed/nuget/"*.nupkg "$cf/nuget/"
    expect_status 0 'private-cache composition' cst use-nuget "$cr" --composition "$cm" --private-cache
    composition_cache=$(git -C "$cr" rev-parse --absolute-git-dir)/cratis-local-nuget
    env -i PATH=/usr/bin:/bin /bin/bash -c "source \"\$1\"; [ \"\$NUGET_PACKAGES\" = \"\$2\" ]" _ "$output/env" "$composition_cache" || failed 'private-cache composition env export'
    [ "$(cat "$cr/.ai-work/local-packages/nuget-cache")" = "$composition_cache" ] && [ "$(cat "$composition_cache/.cratis-local-cache")" = "$cr" ] || failed 'private-cache composition marker'
    expect_status 2 'private-cache composition live kept' cst clean "$cr" --private-cache --apply
    expect_status 0 'private-cache composition restore' cst restore "$cr"
    [ -d "$composition_cache" ] || failed 'private-cache composition restore removed cache'
    expect_status 0 'private-cache composition removal' cst clean "$cr" --private-cache --apply
    [ ! -e "$composition_cache" ] && [ ! -e "$cr/.ai-work/local-packages/nuget-cache" ] || failed 'private-cache composition removal left cache registration'
    cf=$composition_feed
    [ -f "$cf/nuget/Cratis.Chronicle.InProcess.$a.nupkg" ] && [ -f "$cf/nuget/Cratis.Screenplay.$b.nupkg" ] && [ -f "$cf/nuget/Cratis.Stage.$c.nupkg" ] || failed 'private-cache composition cleanup removed recovery feed packages'
    # Every live mutation boundary is exercised both as an ordinary failure and
    # an uncatchable parent/child interruption, then recovered by the next call.
    for kind in fail interrupt; do
        for boundary in journal session stage-manifest.tsv stage-Directory.Packages.props stage-cratis-local.targets stage-nuget.config stage-env stage-state.json stage-transaction staged published repo registration-staged registered committed; do
            printf '%s\n' "$root/another-session" > "$cf/sessions"; cp "$cf/sessions" "$base"
            composition_fail=''; composition_interrupt=''
            if [ "$kind" = fail ]; then composition_fail=$boundary; else composition_interrupt=$boundary; fi
            status=0; cst use-nuget "$cr" --composition "$cm" > "$root/output" 2> "$root/error" || status=$?
            [ "$status" -ne 0 ] || failed "composition $kind $boundary returned success"
            [ ! -s "$root/output" ] || failed "composition $kind $boundary printed activation instructions"
            composition_fail=''; composition_interrupt=''
            # A recovery-triggering restore must roll back incomplete activation,
            # or finish a committed activation and then restore it.
            expect_status 0 "composition recovery $kind $boundary" cst restore "$cr"
            [ ! -e "$output" ] || failed "composition recovery left publication $boundary"
            if [ "$boundary" != committed ]; then cmp -s "$cf/sessions" "$base" || failed "composition registry rollback $boundary"; fi
            cmp -s "$cr/Directory.Packages.props" "$root/composition-original.props" || failed "composition consumer drift $boundary"
            passed=$((passed + 3))
        done
    done
    expect_status 0 'composition activate before interrupted restoration' cst use-nuget "$cr" --composition "$cm"
    for boundary in restore-journal restore-renamed restore-committed; do
        composition_interrupt=$boundary
        status=0; cst restore "$cr" > "$root/output" 2> "$root/error" || status=$?
        [ "$status" -ne 0 ] || failed "composition interrupted restoration $boundary"
        composition_interrupt=''
        expect_status 0 "composition recover interrupted restoration $boundary" cst restore "$cr"
        [ ! -e "$output" ] || failed 'composition incomplete restoration recovery'
        expect_status 0 'composition reactivation after restoration recovery' cst use-nuget "$cr" --composition "$cm"
    done
    expect_status 0 'composition final restore' cst restore "$cr"
    # Another recoverer holds the transition guard after observing this dead owner.
    # A contender must not touch the stale lock (or its journal) until that transition ends.
    local dead_owner
    dead_owner=$(node -p process.pid)
    kill -0 "$dead_owner" 2>/dev/null && failed 'composition fixture owner is still alive'
    mkdir "$cf/.composition-lock" "$cf/.composition-recovery"
    printf '%s\n' "$dead_owner" > "$cf/.composition-lock/owner"
    expect_status 2 'composition competing stale-lock recoverer is excluded' cst restore "$cr"
    [ "$(cat "$cf/.composition-lock/owner")" = "$dead_owner" ] || failed 'composition contender removed stale lock'
    [ -d "$cf/.composition-recovery" ] || failed 'composition contender removed recovery guard'
    rmdir "$cf/.composition-recovery"
    expect_status 0 'composition stale-lock takeover after recovery guard release' cst restore "$cr"
    [ ! -e "$cf/.composition-lock" ] || failed 'composition stale-lock takeover left lock'
    mkdir -p "$cf/.composition-lock"; printf 'unknown owner\n' > "$cf/.composition-lock/owner"
    expect_status 2 'composition unknown ownership fails closed' cst use-nuget "$cr" --composition "$cm"
    grep -Fq 'unknown owner' "$cf/.composition-lock/owner" || failed 'composition deleted unknown owner'
    rm "$cf/.composition-lock/owner"; rmdir "$cf/.composition-lock"
    mkdir -p "$cf/.composition-lock"; printf '%s\n' "$$" > "$cf/.composition-lock/owner"
    expect_status 2 'composition concurrent activation lock' cst use-nuget "$cr" --composition "$cm"
    rm "$cf/.composition-lock/owner"; rmdir "$cf/.composition-lock"
    mkdir -p "$cr/.ai-work/local-packages/npm-backup"; printf 'unrelated npm backup\n' > "$cr/.ai-work/local-packages/npm-backup/package.json"
    expect_status 2 'composition preserves incompatible npm backup' cst use-nuget "$cr" --composition "$cm"
    grep -Fq 'unrelated npm backup' "$cr/.ai-work/local-packages/npm-backup/package.json" || failed 'composition npm backup drift'
    passed=$((passed + 1))
}

TASK=''
case "$MODE" in
    pack-nuget|pack-npm|use-nuget|use-npm|finish)
        TASK=${CRATIS_LOCAL_TASK:-}
        ARGS=()
        while [ $# -gt 0 ]; do
            if [ "$1" = --task ]; then
                [ $# -ge 2 ] || die '--task requires an ID'
                TASK=$2; [ -n "$TASK" ] || die 'invalid task ID'; shift 2
            else ARGS+=("$1"); shift; fi
        done
        set -- ${ARGS[@]+"${ARGS[@]}"}
        if [ -n "$TASK" ]; then [[ "$TASK" =~ ^[A-Za-z0-9][A-Za-z0-9._#:/-]{0,127}$ ]] || die 'invalid task ID'; fi ;;
esac
case "$MODE" in
    list) registry list "$@" ;;
    finish)
        [ -n "$TASK" ] || die 'finish requires --task ID or CRATIS_LOCAL_TASK'
        [ $# -eq 0 ] || { [ $# -eq 1 ] && [ "$1" = --apply ]; } || die 'finish accepts --task ID and --apply'
        if [ "${1:-}" = --apply ] && [ "$DRY" -eq 0 ]; then registry_update finish --apply; else registry finish; fi ;;
    stamp) stamp "$@" ;;
    plan) plan_command "$@" ;;
    closure) closure "$@" ;;
    verify-pack) verify_pack "$@" ;;
    verify-assets) verify_assets "$@" ;;
    pack-nuget) pack_nuget "$@" ;;
    pack-npm) pack_npm "$@" ;;
    use-nuget) acquire_session_lock; use_nuget "$@" ;;
    use-npm) acquire_session_lock; use_npm "$@" ;;
    restore) acquire_session_lock; restore "$@" ;;
    check) check "$@" ;;
    clean) acquire_session_lock; clean "$@" ;;
    --self-test) self_test "$@" ;;
    *) die "unknown command: $MODE (use --help)" ;;
esac
