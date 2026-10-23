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
  stamp PRODUCER
    Next patch of the highest reachable vX.Y.Z tag, UTC minute and HEAD sha7.
  pack-nuget PRODUCER STAMP PROJECT-OR-SLN...
    Release-pack each target into the NuGet feed; requires dotnet.
  pack-npm PRODUCER STAMP
    Requires node and Yarn 4, and a completed repository build. Temporarily
    version non-private workspaces, pack with %s-%v.tgz (Yarn slugifies scoped
    names), and restore tracked manifests even on failure. Pack lifecycle hooks
    are Yarn's own behavior; this helper does not run a separate build.
  use-nuget CONSUMER STAMP [PACKAGE-ID...]
    Create a CPM overlay importing the repository's own Directory.Packages.props.
    Defaults to every stamped package ID in the feed. Prints properties and sourceable
    env. With source mapping, also prints the required restore --configfile.
    Non-CPM: manually use an untracked project copy with explicit PackageReference
    versions and dotnet restore --source FEED/nuget; do not edit shipped versions.
  use-npm CONSUMER STAMP
    Requires node and tar. Back up package.json, yarn.lock (including existing edits),
    and git diff; add file-tarball resolutions for every stamped feed package.
    Run yarn install afterward. Only one active npm overlay per session.
  restore CONSUMER
    Restore npm backups when only overlay resolutions and their local lockfile
    entries changed (including yarn install). Refuse unrelated changes and retain
    backups for review. Remove CPM/env overlays
    and run check. No git checkout is used on consumer files.
  check REPO [--staged]
    Scan added/changed tracked lines (staged-only if requested), plus all contents
    of untracked, non-ignored files. Reports path:line:pattern and file counts.
  clean [CONSUMER] [--stamp STAMP | --all] [--apply]
    Default: list all local feed/cache/image/session subjects, without deleting.
    --apply deletes listed subjects (docker rmi, never force). Refuses more than
    CRATIS_LOCAL_CLEAN_MAX subjects (default 500), or unresolved npm backups.
    Known sessions are registered in FEED/sessions; an optional consumer also
    discovers its per-worktree session. Docker probes have a 10-second deadline.
  --self-test
    Isolated scratch HOME tests; npm uses inherited PATH to discover node.
    Other tests use system PATH. Reports passed and skipped counts.

Configuration: source ${XDG_CONFIG_HOME:-$HOME/.config}/cratis/local-packages.env
if present (trusted shell code), then use these defaults for unset variables:
  CRATIS_LOCAL_FEED=${XDG_CACHE_HOME:-$HOME/.cache}/cratis/local-feed
  CRATIS_LOCAL_SESSION_DIR=<absolute-git-dir>/cratis-local (per checkout)
  NUGET_PACKAGES=$HOME/.nuget/packages
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
case "$NUGET_CACHE" in /*) ;; *) NUGET_CACHE=$PWD/$NUGET_CACHE ;; esac
TMP=''
PACK_REPO=''
PACK_MANIFESTS=()
cleanup() {
    local status=$?
    trap - EXIT
    if [ ${#PACK_MANIFESTS[@]} -gt 0 ]; then
        if ! git -C "$PACK_REPO" checkout -- "${PACK_MANIFESTS[@]}"; then
            printf 'ERROR: producer manifest restoration failed: %s\n' "$PACK_REPO" >&2
            status=1
        fi
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
    [[ "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+-local\.[0-9]{12}\.g[0-9a-f]{7}$ ]] || die "invalid local stamp: $1"
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
            repo|stamp|env|Directory.Packages.props|nuget.config) [ -f "$entry" ] || die "unexpected session entry: $entry" ;;
            npm-backup) [ -d "$entry" ] || die "unexpected session entry: $entry" ;;
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
xml_escape() { printf '%s' "$1" | sed -e 's/\&/\&amp;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g' -e 's/"/\&quot;/g' -e "s/'/\&apos;/g"; }

stamp() {
    [ $# -eq 1 ] || die 'stamp requires a producer repository'
    local repo tag major minor patch sha
    repo=$(repo_root "$1")
    tag=$(git -C "$repo" tag --merged HEAD | awk '/^v[0-9]+\.[0-9]+\.[0-9]+$/' | sort -t. -k1.2,1n -k2,2n -k3,3n | tail -n 1)
    if [ -n "$tag" ]; then
        IFS=. read -r major minor patch <<< "${tag#v}"
        tag="$major.$minor.$((10#$patch + 1))"
    else
        printf 'WARNING: no reachable vX.Y.Z tag; using 0.1.0\n' >&2
        tag=0.1.0
    fi
    sha=$(git -C "$repo" rev-parse HEAD) || die 'producer has no HEAD commit'
    printf '%s-local.%s.g%.7s\n' "$tag" "$(date -u +%Y%m%d%H%M)" "$sha"
}

pack_nuget() {
    [ $# -ge 3 ] || die 'pack-nuget requires producer, stamp and targets'
    local repo version target path count=0
    repo=$(repo_root "$1"); version=$2; shift 2
    valid_stamp "$version"; need dotnet
    mutate mkdir -p "$FEED/nuget"
    for target in "$@"; do
        (cd "$repo" && heavy build "pack NuGet $target" dotnet pack "$target" -c Release "-p:PackageVersion=$version" "-p:Version=$version" -o "$FEED/nuget")
    done
    [ "$DRY" -eq 0 ] || return 0
    while IFS= read -r -d '' path; do
        printf '%s\n' "$path"; count=$((count + 1))
    done < <(find "$FEED/nuget" -type f -name "*.$version.nupkg" -print0)
    [ "$count" -gt 0 ] || failed 'pack produced no matching nupkgs'
    printf 'produced %s nupkgs\n' "$count"
}

pack_npm() {
    [ $# -eq 2 ] || die 'pack-npm requires producer and stamp'
    local repo version path count=0
    repo=$(repo_root "$1"); version=$2
    valid_stamp "$version"; need node; need yarn; scratch
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
    (cd "$repo" && heavy build 'pack npm workspaces' yarn workspaces foreach -A --no-private pack --out "$FEED/npm/%s-%v.tgz")
    if [ "$DRY" -eq 1 ]; then quote_command git -C "$repo" checkout -- '<versioned manifests>'; return 0; fi
    git -C "$repo" checkout -- "${PACK_MANIFESTS[@]}"
    PACK_MANIFESTS=()
    while IFS= read -r -d '' path; do
        printf '%s\n' "$path"; count=$((count + 1))
    done < <(find "$FEED/npm" -type f -name "*-$version.tgz" -print0)
    [ "$count" -gt 0 ] || failed 'pack produced no matching npm tarballs'
    printf 'produced %s npm tarballs\n' "$count"
}

# Read Include/Update attributes without assuming one element per line.
package_ids() {
    awk 'BEGIN { RS="<" } /^PackageVersion[[:space:]]/ {
        gsub(sprintf("%c",39),"\"")
        if (match($0, /(Include|Update)[[:space:]]*=[[:space:]]*"[^"]+"/)) {
            value=substr($0,RSTART,RLENGTH); sub(/^[^"]*"/,"",value); sub(/"$/,"",value); print value
        }
    }' "$1"
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

use_nuget() {
    [ $# -ge 2 ] || die 'use-nuget requires consumer and stamp'
    local repo version session props id file config='' props_key sources_key action
    local ids=()
    repo=$(repo_root "$1"); version=$2; shift 2
    valid_stamp "$version"; props=$repo/Directory.Packages.props
    [ -f "$props" ] || die 'no Directory.Packages.props (non-CPM). Manual alternative: untracked project copy, explicit PackageReference versions, dotnet restore --source FEED/nuget'
    session=$(session_path "$repo"); scratch
    package_ids "$props" > "$TMP/consumer-ids"
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
    {
        printf '<Project>\n  <Import Project="%s" />\n' "$(xml_escape "$props")"
        printf '  <PropertyGroup><CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled></PropertyGroup>\n  <ItemGroup>\n'
        for id in "${ids[@]}"; do
            action=Include
            if grep -Fxiq -- "$id" "$TMP/consumer-ids"; then action=Update; fi
            printf '    <PackageVersion %s="%s" Version="%s" />\n' "$action" "$id" "$version"
        done
        printf '  </ItemGroup>\n</Project>\n'
    } > "$session/Directory.Packages.props"
    props_key=DirectoryPackages"PropsPath"; sources_key=RestoreAdditional"ProjectSources"
    {
        printf 'export %s=%q\n' "$props_key" "$session/Directory.Packages.props"
        printf 'export %s=%q\n' "$sources_key" "$FEED/nuget"
    } > "$session/env"
    printf ' %q' "-p:$props_key=$session/Directory.Packages.props" "-p:$sources_key=$FEED/nuget"; printf '\n'
    printf 'source %q\n' "$session/env"
    if [ -n "$config" ]; then
        source_mapping "$config" "$session/nuget.config" "${ids[@]}"
        printf 'For restore, also pass: --configfile %q\n' "$session/nuget.config"
    elif [ -f "$session/nuget.config" ]; then
        rm -- "$session/nuget.config"
    fi
    printf 'overrode %s NuGet package IDs\n' "${#ids[@]}"
}

use_npm() {
    [ $# -eq 2 ] || die 'use-npm requires consumer and stamp'
    local repo version session backup
    repo=$(repo_root "$1"); version=$2
    valid_stamp "$version"; need node; need tar; scratch
    session=$(session_path "$repo"); backup=$session/npm-backup
    [ -f "$repo/package.json" ] || die 'consumer has no root package.json'
    [ ! -e "$backup" ] || die "active npm backup exists: $backup; restore first"
    node - "$repo" "$FEED/npm" "$version" "$TMP/package.json" <<'NODE'
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
        quote_command cp "$TMP/package.json" "$repo/package.json"
        return 0
    fi
    register_session "$repo" "$session" "$version"
    mkdir "$backup"
    cp -p "$repo/package.json" "$backup/package.json"
    if [ -f "$repo/yarn.lock" ]; then cp -p "$repo/yarn.lock" "$backup/yarn.lock"; fi
    git -C "$repo" diff HEAD -- package.json yarn.lock > "$backup/before.diff"
    cp "$TMP/package.json" "$backup/expected-package.json"
    cp "$TMP/package.json" "$repo/package.json"
    printf 'Next: cd %q && yarn install\n' "$repo"
    printf 'Vite: clear stale prebundles with rm -rf node_modules/.vite\n'
}

restore() {
    [ $# -eq 1 ] || die 'restore requires a consumer'
    local repo session backup
    repo=$(repo_root "$1"); session=$(session_path "$repo"); backup=$session/npm-backup
    validate_session "$session"
    if [ -f "$session/repo" ] && [ "$(cat "$session/repo")" != "$repo" ]; then die 'session belongs to another checkout'; fi
    if [ -d "$backup" ]; then
        if ! cmp -s "$repo/package.json" "$backup/expected-package.json" ||
            { [ -f "$backup/yarn.lock" ] && ! cmp -s "$repo/yarn.lock" "$backup/yarn.lock"; } ||
            { [ ! -f "$backup/yarn.lock" ] && [ -e "$repo/yarn.lock" ]; }; then
            need node
            node "$(dirname "$SELF")/npm-restore-check.cjs" "$repo" "$backup" ||
                die "npm files changed beyond the overlay; review $backup (nothing restored)"
        fi
        mutate cp -p "$backup/package.json" "$repo/package.json"
        if [ -f "$backup/yarn.lock" ]; then mutate cp -p "$backup/yarn.lock" "$repo/yarn.lock"; else mutate rm -f -- "$repo/yarn.lock"; fi
        mutate rm -rf -- "$backup"
    fi
    mutate rm -f -- "$session/Directory.Packages.props" "$session/nuget.config" "$session/env" "$session/stamp"
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
    awk -v path="$path" -v feed="$FEED" '
        function hit(pattern) { print path ":" number ":" pattern }
        {
            number=$0; sub(/:.*/,"",number); line=$0; sub(/^[^:]*:/,"",line)
            if (line ~ /-local\.[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]\.g[0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f]/) hit("local stamp")
            if (index(line,feed)) hit("local feed path")
            if (index(line,"DirectoryPackages" "PropsPath")) hit("CPM overlay property")
            if (index(line,"RestoreAdditional" "ProjectSources")) hit("additional restore sources")
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
    [[ "$1" =~ -local\.[0-9]{12}\.g[0-9a-f]{7}($|[^0-9A-Za-z]) ]]
}
clean() {
    local version='' apply=0 consumer='' arg path cache image session maximum count=0
    local subjects=() kinds=()
    while [ $# -gt 0 ]; do
        arg=$1; shift
        case "$arg" in
            --stamp) [ $# -gt 0 ] || die '--stamp requires a stamp'; [ -z "$version" ] || die 'choose --stamp or --all'; version=$1; shift; valid_stamp "$version" ;;
            --all) [ -z "$version" ] || die 'choose --stamp or --all'; version=all ;;
            --apply) apply=1 ;;
            -*) die "unknown clean option: $arg" ;;
            *) [ -z "$consumer" ] || die 'clean takes at most one consumer'; consumer=$(repo_root "$arg") ;;
        esac
    done
    maximum=${CRATIS_LOCAL_CLEAN_MAX:-500}
    [[ "$maximum" =~ ^[0-9]+$ ]] || die 'CRATIS_LOCAL_CLEAN_MAX must be a whole number'
    [ ${#maximum} -le 6 ] || die 'CRATIS_LOCAL_CLEAN_MAX is too large'
    maximum=$((10#$maximum))
    scratch
    for cache in "$FEED/nuget" "$FEED/npm"; do
        [ -d "$cache" ] || continue
        while IFS= read -r -d '' path; do
            local_artifact "${path##*/}" || continue
            if [ -n "$version" ] && [ "$version" != all ]; then case "$path" in *"$version"*) ;; *) continue ;; esac; fi
            subjects+=("$path"); kinds+=(file)
        done < <(find "$cache" -type f \( -name '*.nupkg' -o -name '*.tgz' \) -print0)
    done
    if [ -d "$NUGET_CACHE" ]; then
        for cache in "$NUGET_CACHE"/*; do
            [ -d "$cache" ] && [ ! -L "$cache" ] || continue
            for path in "$cache"/*-local.*; do
                [ -d "$path" ] && [ ! -L "$path" ] || continue
                local_artifact "${path##*/}" || continue
                if [ -n "$version" ] && [ "$version" != all ] && [ "${path##*/}" != "$version" ]; then continue; fi
                subjects+=("$path"); kinds+=(directory)
            done
        done
    fi
    if command -v yarn >/dev/null 2>&1 && [ -n "$consumer" ]; then
        cache=$(cd "$consumer" && yarn config get cacheFolder) || die 'cannot discover Yarn cache'
        if [ -d "$cache" ]; then
            while IFS= read -r -d '' path; do
                local_artifact "${path##*/}" || continue
                if [ -n "$version" ] && [ "$version" != all ]; then case "$path" in *"$version"*) ;; *) continue ;; esac; fi
                subjects+=("$path"); kinds+=(file)
            done < <(find "$cache" -type f -name '*-local.*' -print0)
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
    if [ -f "$FEED/sessions" ]; then cat "$FEED/sessions" > "$TMP/sessions"; else : > "$TMP/sessions"; fi
    if [ -n "$consumer" ]; then session_path "$consumer" >> "$TMP/sessions"; fi
    sort -u "$TMP/sessions" > "$TMP/unique-sessions"
    while IFS= read -r session; do
        [ -d "$session" ] && [ -f "$session/repo" ] && [ -f "$session/stamp" ] || continue
        local_artifact "$(cat "$session/stamp")" || continue
        if [ -n "$version" ] && [ "$version" != all ] && [ "$(cat "$session/stamp")" != "$version" ]; then continue; fi
        [ ! -L "$session" ] && [ "$session" != / ] || die "unsafe session: $session"
        validate_session "$session"
        path=$(cat "$session/repo")
        [ "$(session_path "$path")" = "$session" ] || die "session directory no longer matches repository: $session"
        [ ! -d "$session/npm-backup" ] || die "unresolved npm backup: $session; restore before cleaning"
        subjects+=("$session"); kinds+=(session)
    done < "$TMP/unique-sessions"
    count=${#subjects[@]}
    [ "$count" -le "$maximum" ] || die "clean fuse: $count subjects exceeds $maximum"
    for ((arg=0; arg<count; arg++)); do printf '%s: %s\n' "${kinds[$arg]}" "${subjects[$arg]}"; done
    if [ "$apply" -eq 1 ] && [ "$DRY" -eq 0 ]; then
        for ((arg=0; arg<count; arg++)); do
            path=${subjects[$arg]}
            case "${kinds[$arg]}" in
                image) docker rmi "$path" ;;
                file) [ -f "$path" ] && [ ! -L "$path" ] || die "clean subject changed: $path"; rm -- "$path" ;;
                directory) [ -d "$path" ] && [ ! -L "$path" ] || die "clean subject changed: $path"; rm -rf -- "$path" ;;
                session)
                    [ -f "$path/stamp" ] && [ ! -L "$path" ] && [ ! -e "$path/npm-backup" ] || die "session changed: $path"
                    validate_session "$path"
                    rm -f -- "$path/repo" "$path/stamp" "$path/env" "$path/Directory.Packages.props" "$path/nuget.config"
                    rmdir -- "$path" ;;
            esac
        done
        printf 'removed %s subjects\n' "$count"
    else printf 'dry-run: %s subjects (use --apply to remove)\n' "$count"; fi
}

self_test() {
    [ $# -eq 0 ] || die '--self-test takes no arguments'
    local root home repo feed version output status passed=0 skipped=0 test_key arg
    scratch; root=$TMP/self-test; mkdir -p "$root/home" "$root/repo"
    home=$root/home; repo=$root/repo; feed=$home/.cache/cratis/local-feed
    # System-only tests stay hermetic; npm tests inherit PATH to discover node.
    # Both isolate HOME/config and omit the host adapter.
    st() { env -i HOME="$home" PATH=/usr/bin:/bin:/usr/sbin:/sbin TMPDIR="$TMP" /bin/bash "$SELF" "$@"; }
    st_npm() { env -i HOME="$home" PATH="$PATH" TMPDIR="$TMP" /bin/bash "$SELF" "$@"; }
    git -C "$repo" init -q
    repo=$(repo_root "$repo")
    git -C "$repo" config user.email test@example.invalid
    git -C "$repo" config user.name 'Local package self-test'
    printf '<Project><ItemGroup><PackageVersion Include="Cratis.Test" Version="1.2.3" /></ItemGroup></Project>\n' > "$repo/Directory.Packages.props"
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
    grep -Fq "Import Project=\"$repo/Directory.Packages.props\"" "$output/Directory.Packages.props" || failed 'self-test: overlay import'
    grep -Fq "PackageVersion Update=\"Cratis.Test\" Version=\"$version\"" "$output/Directory.Packages.props" || failed 'self-test: pinned package Update'
    grep -Fq "PackageVersion Include=\"Cratis.Transitive\" Version=\"$version\"" "$output/Directory.Packages.props" || failed 'self-test: transitive package Include'
    grep -Fq 'CentralPackageTransitivePinningEnabled>true' "$output/Directory.Packages.props" || failed 'self-test: transitive pinning'
    test_key=DirectoryPackages"PropsPath"
    grep -Fq "export $test_key=" "$output/env" || failed 'self-test: env property'
    passed=$((passed + 5))
    st check "$repo" > "$root/output" || failed 'self-test: clean repo'
    passed=$((passed + 1))
    for test_key in stamp npm props sources override feed; do
        case "$test_key" in
            stamp) printf '%s\n' "$version" > "$repo/probe" ;;
            npm) printf '{"resolutions":{"@cratis/test":"%s%s"}}\n' 'file:/tmp/' 'package.tgz' > "$repo/package.json" ;;
            props) printf '%s=/tmp/overlay\n' DirectoryPackages"PropsPath" > "$repo/probe" ;;
            sources) printf '%s=/tmp/feed\n' RestoreAdditional"ProjectSources" > "$repo/probe" ;;
            override) printf '<PackageVersion %s="Cratis.Test" />\n' Update > "$repo/probe" ;;
            feed) printf '%s\n' "$feed" > "$repo/probe" ;;
        esac
        status=0; st check "$repo" > "$root/output" || status=$?
        [ "$status" -eq 1 ] || failed "self-test: did not find $test_key leak"
        passed=$((passed + 1))
        git -C "$repo" checkout -- probe package.json
    done
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
    if command -v node >/dev/null 2>&1; then
        # Preserve pre-existing user edits as well as the committed contents.
        st_npm use-npm "$repo" "$version"
        grep -Fq '"@cratis/transitive": "file:' "$repo/package.json" || failed 'self-test: transitive npm resolution'
        grep -Fq '"@cratis/test": "file:' "$repo/package.json" || failed 'self-test: direct npm resolution'
        st_npm restore "$repo" > "$root/output"
        passed=$((passed + 2))
        printf '{"private":true,"dependencies":{"@cratis/test":"1.2.3"},"userSetting":true}\n' > "$repo/package.json"
        cp "$repo/package.json" "$root/original-package.json"
        cp "$repo/yarn.lock" "$root/original-yarn.lock"
        st_npm use-npm "$repo" "$version"
        # Model Yarn's replacement of a remote entry with a file-tarball entry.
        node - "$repo/yarn.lock" "$feed/npm/@cratis-test-$version.tgz" "$version" <<'NODE'
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
    st clean --stamp "$version" --apply > "$root/output"
    [ ! -e "$home/.nuget/packages/cratis.test/$version" ] || failed 'self-test: clean failed to remove cache'
    [ -d "$home/.nuget/packages/cratis.test/1.2.3" ] || failed 'self-test: clean removed real version'
    passed=$((passed + 4))
    printf 'SKIP: real dotnet/Yarn/Docker packing is not a hermetic self-test\n'; skipped=$((skipped + 3))
    printf 'self-test: %s passed, %s skipped\n' "$passed" "$skipped"
}

case "$MODE" in
    stamp) stamp "$@" ;;
    pack-nuget) pack_nuget "$@" ;;
    pack-npm) pack_npm "$@" ;;
    use-nuget) use_nuget "$@" ;;
    use-npm) use_npm "$@" ;;
    restore) restore "$@" ;;
    check) check "$@" ;;
    clean) clean "$@" ;;
    --self-test) self_test "$@" ;;
    *) die "unknown command: $MODE (use --help)" ;;
esac
