#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Proves every Chronicle package about to ship restores for a consumer on each framework it ships.
# A pin below a transitive dependency's requirement (NU1605) must fail the pull request, not the release.
# CI uses a non-publishable 0.0.0-pr.N version; these packed packages are never pushed.
# Each package gets its own consumer so other top-level references cannot hide dependency downgrades.
# PACKED_RESTORE_ERRORS defaults to "NU1605;NU1608". Setting it explicitly selects which warnings
# become errors instead of treating all warnings as errors (for example, PACKED_RESTORE_ERRORS=NU1605).
# Usage: verify-packed-consumer-restore.sh <version> <feed-dir>
#        verify-packed-consumer-restore.sh --self-test

set -euo pipefail

fail() { echo "::error::$*" >&2; exit 2; }

self_test() (
    local script="$1" root status
    root="$(mktemp -d /tmp/chronicle-restore-self-test.XXXXXX)"
    trap 'rm -rf "$root"' EXIT
    mkdir -p "$root/feed" "$root/bin"
    cat > "$root/package.nuspec" <<'NUSPEC'
<package><metadata><id>Cratis.Chronicle.SelfTest</id><version>0.0.0-pr.1</version><dependencies><group targetFramework="netstandard2.0" /></dependencies></metadata></package>
NUSPEC
    mkdir -p "$root/lib/netstandard2.0" "$root/analyzers/dotnet/cs"
    : > "$root/lib/netstandard2.0/_._"
    : > "$root/analyzers/dotnet/cs/MockAnalyzer.dll"
    (cd "$root" && zip -q "$root/feed/arbitrary.name.nupkg" package.nuspec lib/netstandard2.0/_._)
    cat > "$root/second.nuspec" <<'NUSPEC'
<package><metadata><id>Cratis.Chronicle.OtherPackage</id><version>0.0.0-pr.1</version></metadata></package>
NUSPEC
    (cd "$root" && zip -q "$root/feed/second.nupkg" second.nuspec analyzers/dotnet/cs/MockAnalyzer.dll)
    cat > "$root/bin/dotnet" <<'DOTNET'
#!/usr/bin/env bash
set -euo pipefail
[ "$1" = restore ]
[ "$(grep -c '<PackageReference ' "$2")" -eq 1 ]
grep -Fq "<WarningsAsErrors>${PACKED_RESTORE_ERRORS:-NU1605;NU1608}</WarningsAsErrors>" "$2"
strict=true
[ -z "${PACKED_RESTORE_ERRORS+x}" ] || strict=false
grep -Fq "<TreatWarningsAsErrors>$strict</TreatWarningsAsErrors>" "$2"
grep -Fq '<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>' "$2"
case "$2|$3" in
    */Cratis.Chronicle.SelfTest/Consumer.csproj\|-p:TargetFrameworks=net8.0) echo 'error NU1605: planted downgrade'; exit 1 ;;
esac
DOTNET
    chmod +x "$root/bin/dotnet"
    if PATH="$root/bin:$PATH" bash "$script" 0.0.0-pr.1 "$root/feed" > "$root/result" 2>&1; then
        fail 'Self-test missed the planted downgrade.'
    else
        status=$?
    fi
    [ "$status" -eq 1 ] || fail "Self-test could not run (exit $status)."
    grep -q 'NU1605: planted downgrade' "$root/result"
    grep -q 'Cratis.Chronicle.SelfTest | net8.0 | FAIL | 1' "$root/result"
    grep -q 'Cratis.Chronicle.SelfTest | net9.0 | PASS | 0' "$root/result"
    grep -q 'Cratis.Chronicle.SelfTest | net10.0 | PASS | 0' "$root/result"
    grep -q 'Cratis.Chronicle.OtherPackage | net8.0 | PASS | 0' "$root/result"
    grep -q 'Cratis.Chronicle.OtherPackage | net9.0 | PASS | 0' "$root/result"
    grep -q 'Cratis.Chronicle.OtherPackage | net10.0 | PASS | 0' "$root/result"
    echo 'Self-test: detected 1 planted downgrade and checked all 6 package/TFM outcomes.'
)

if [ "${1:-}" = --self-test ]; then
    self_test "$(cd "$(dirname "$0")" && pwd)/$(basename "$0")"
    exit 0
fi

[ "$#" -eq 2 ] || fail 'Usage: verify-packed-consumer-restore.sh <version> <feed-dir>'
VERSION="$1"
RESTORE_ERRORS="${PACKED_RESTORE_ERRORS:-NU1605;NU1608}"
TREAT_WARNINGS_AS_ERRORS=true
[ -z "${PACKED_RESTORE_ERRORS+x}" ] || TREAT_WARNINGS_AS_ERRORS=false
case "$VERSION" in ''|*[!0-9A-Za-z.+-]*) fail "Invalid package version: $VERSION" ;; esac
[ -d "$2" ] || fail "Package feed directory does not exist: $2"
LOCAL_FEED="$(cd "$2" && pwd)"
WORK_DIR="$(mktemp -d "${RUNNER_TEMP:-/tmp}/chronicle-packed-restore.XXXXXX")"
trap 'rm -rf "$WORK_DIR"' EXIT
export NUGET_PACKAGES="$WORK_DIR/packages"

# Outside the repository: its central package management and build props must not affect consumers.
cat > "$WORK_DIR/Directory.Build.props" <<'PROPS'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
PROPS
FEED_XML="$(printf '%s' "$LOCAL_FEED" | sed -e 's/\&/\&amp;/g' -e 's/"/\&quot;/g' -e 's/</\&lt;/g' -e 's/>/\&gt;/g')"
cat > "$WORK_DIR/nuget.config" <<CONFIG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="packed" value="$FEED_XML" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
CONFIG

: > "$WORK_DIR/consumers"

count=0
shopt -s nullglob
for package in "$LOCAL_FEED"/*.nupkg; do
    unzip -p "$package" '*.nuspec' > "$WORK_DIR/package.nuspec" || fail "Cannot read nuspec: $package"
    metadata="$(tr '\r\n' '  ' < "$WORK_DIR/package.nuspec")"
    id="$(printf '%s' "$metadata" | sed -n 's:.*<id>\([^<]*\)</id>.*:\1:p')"
    version="$(printf '%s' "$metadata" | sed -n 's:.*<version>\([^<]*\)</version>.*:\1:p')"
    [ -n "$id" ] && [ -n "$version" ] || fail "Missing package id/version: $package"
    case "$id" in Cratis.Chronicle*) ;; *) continue ;; esac
    [ "$version" = "$VERSION" ] || continue
    case "$id" in *[!0-9A-Za-z_.-]*) fail "Invalid package id: $id" ;; esac
    count=$((count + 1))
    echo "Discovered: $id $version"

    unzip -Z1 "$package" > "$WORK_DIR/entries"
    lib_frameworks="$(sed -n 's:^lib/\([^/][^/]*\)/.*[^/]$:\1:p' "$WORK_DIR/entries" | sort -u)"
    if [ -z "$lib_frameworks" ]; then
        # The only no-lib exception is an analyzer package; restore it on all shipped consumer TFMs.
        grep -q '^analyzers/.*\.dll$' "$WORK_DIR/entries" || fail "$id $version has neither a lib/ TFM nor analyzers/."
        frameworks=""
    else
        # Dependency groups describe shipped TFMs; fall back to lib folders if groups do not name any.
        frameworks="$(tr '<' '\n' < "$WORK_DIR/package.nuspec" | sed -n 's/^group[[:space:]][^>]*targetFramework="\([^"]*\)".*/\1/p')"
        [ -n "$frameworks" ] || frameworks="$lib_frameworks"
    fi
    applicable_tfms=""
    for tfm in net8.0 net9.0 net10.0; do
        applies=0
        # An analyzer-only package applies to all supported frameworks.
        [ -n "$frameworks" ] || applies=1
        while IFS= read -r framework; do
            framework="$(printf '%s' "$framework" | tr '[:upper:]' '[:lower:]')"
            case "$framework" in
                "$tfm"|netstandard*|.netstandard*) applies=1 ;;
            esac
        done <<< "$frameworks"
        if [ "$applies" -eq 1 ]; then
            applicable_tfms="${applicable_tfms:+$applicable_tfms;}$tfm"
            printf '%s %s\n' "$id" "$tfm" >> "$WORK_DIR/consumers"
            echo "$id $tfm references: $id [$VERSION]"
        fi
    done
    [ -n "$applicable_tfms" ] || fail "$id $version applies to no consumer TFM (shipped: $frameworks)."
    mkdir -p "$WORK_DIR/$id"
    cat > "$WORK_DIR/$id/Consumer.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>$applicable_tfms</TargetFrameworks>
    <TreatWarningsAsErrors>$TREAT_WARNINGS_AS_ERRORS</TreatWarningsAsErrors>
    <WarningsAsErrors>$RESTORE_ERRORS</WarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="$id" Version="[$VERSION]" />
  </ItemGroup>
</Project>
PROJECT
    # Validate the generated project, not just the discovery list, before any restore can pass.
    grep -q '<PackageReference ' "$WORK_DIR/$id/Consumer.csproj" || fail "$id consumer references zero packages."
done
[ "$count" -gt 0 ] || fail "No Cratis.Chronicle packages at version $VERSION found in $LOCAL_FEED."
echo "Discovered $count Chronicle packages; no packages excluded."
[ -s "$WORK_DIR/consumers" ] || fail 'Zero consumers were generated.'

failed=0
printf 'Package | TFM | Result | Exit\n' > "$WORK_DIR/summary"
while read -r id tfm; do
    echo "Restoring $id $tfm consumer..."
    # Select one framework from this package's multi-targeted project for an exact per-TFM verdict.
    if dotnet restore "$WORK_DIR/$id/Consumer.csproj" "-p:TargetFrameworks=$tfm" --configfile "$WORK_DIR/nuget.config"; then
        status=0
        result=PASS
    else
        status=$?
        result=FAIL
        failed=1
    fi
    printf '%s | %s | %s | %s\n' "$id" "$tfm" "$result" "$status" >> "$WORK_DIR/summary"
done < "$WORK_DIR/consumers"
echo "Restore summary ($count packages discovered):"
cat "$WORK_DIR/summary"
exit "$failed"
