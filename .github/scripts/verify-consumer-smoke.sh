#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Installs the Chronicle packages about to be published into a throwaway application alongside the
# published Arc packages that build on them, and starts it.
#
# Chronicle never references the Arc packages that integrate with it, so nothing in this repository
# exercises the combination an application actually installs. Every gate here can be green while that
# combination fails on the first line it runs - and it fails at startup, in the call every service
# makes, rather than anywhere a test would look. The only way to see it is from outside, with the
# published artifacts, which is what this does.
#
# Restoring is not enough on its own: the packages resolve cleanly and still have to agree at run
# time, because type discovery loads them by name and only finds out then. So this runs the thing.
#
# Usage: verify-consumer-smoke.sh <chronicle-version> [local-package-feed]
#   chronicle-version   the Chronicle version to install, e.g. the one about to be published
#   local-package-feed  directory holding freshly packed .nupkg files; omit to use nuget.org only

set -euo pipefail

CHRONICLE_VERSION="${1:?Usage: verify-consumer-smoke.sh <chronicle-version> [local-package-feed]}"
LOCAL_FEED="${2:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

# Built outside the repository so its central package management and build props cannot reach in -
# the application has to resolve the way somebody else's application resolves.
cat > "$WORK_DIR/Directory.Build.props" <<'PROPS'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
PROPS

FEED_ENTRY=""
if [ -n "$LOCAL_FEED" ]; then
    FEED_ENTRY="<add key=\"packed\" value=\"$(cd "$LOCAL_FEED" && pwd)\" />"
fi

cat > "$WORK_DIR/nuget.config" <<CONFIG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    $FEED_ENTRY
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
CONFIG

# Arc floats to whatever is current, because that is what an application installing both gets today.
# Keep separate consumers: one embedded kernel package must not supply an assembly missing from the other.
# TestingOnly installs no Arc package explicitly: the testing package must carry its own kernel dependencies.
mkdir -p "$WORK_DIR/Testing" "$WORK_DIR/XUnitIntegration" "$WORK_DIR/TestingOnly"
cat > "$WORK_DIR/TestingOnly/Consumer.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Chronicle.Testing" Version="$CHRONICLE_VERSION" />
    <Compile Include="$SCRIPT_DIR/TestingOnlyProgram.cs" Link="Program.cs" />
  </ItemGroup>
</Project>
PROJECT
cat > "$WORK_DIR/Testing/Consumer.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Chronicle" Version="$CHRONICLE_VERSION" />
    <PackageReference Include="Cratis.Chronicle.Testing" Version="$CHRONICLE_VERSION" />
    <PackageReference Include="Cratis.Arc" Version="*" />
    <PackageReference Include="Cratis.Arc.Chronicle" Version="*" />
    <PackageReference Include="Cratis.Arc.Chronicle.Testing" Version="*" />
    <Compile Include="../Program.cs" Link="Program.cs" />
  </ItemGroup>
</Project>
PROJECT

cat > "$WORK_DIR/XUnitIntegration/Consumer.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Chronicle" Version="$CHRONICLE_VERSION" />
    <PackageReference Include="Cratis.Chronicle.XUnit.Integration" Version="$CHRONICLE_VERSION" />
    <PackageReference Include="Cratis.Arc" Version="*" />
    <PackageReference Include="Cratis.Arc.Chronicle" Version="*" />
    <Compile Include="../Program.cs" Link="Program.cs" />
  </ItemGroup>
</Project>
PROJECT

# AddCratisArcCore is where type discovery runs, so it is where a disagreement between the packages
# surfaces - the same call every real service makes on startup.
cat > "$WORK_DIR/Program.cs" <<'PROGRAM'
using Cratis.Arc;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCratisArcCore();

if (services.Count == 0)
{
    Console.Error.WriteLine("Type discovery registered nothing, which means it did not run.");
    return 1;
}

Console.WriteLine($"Registered {services.Count} services.");
return 0;
PROGRAM

# A pinned dependency that was published minutes ago may not be served by nuget.org yet (NU1101/NU1102).
# Wait for that exact version to appear, for up to 20 minutes, and run once more; any other failure, or a
# version that never appears, still fails.
run_consumer() {
    local project="$1" log="$2"
    if dotnet run --project "$project" > "$log" 2>&1; then
        return 0
    fi
    local missing package version
    missing=$(grep -oE "Unable to find package [A-Za-z0-9_.-]+ with version \(>= [0-9][0-9A-Za-z.+-]*\)" "$log" | head -1 || true)
    [ -n "$missing" ] || return 1
    package=$(echo "$missing" | awk '{print $5}')
    version=$(echo "$missing" | sed -E 's/.*\(>= ([^)]*)\)/\1/')
    echo "$package $version is not on nuget.org yet; waiting up to 20 minutes for it to be served..."
    local lower deadline
    lower=$(echo "$package" | tr '[:upper:]' '[:lower:]')
    deadline=$(( $(date +%s) + 1200 ))
    until curl -fsS "https://api.nuget.org/v3-flatcontainer/$lower/index.json" | grep -q "\"$version\""; do
        if [ "$(date +%s)" -ge "$deadline" ]; then
            echo "::error::$package $version did not appear on nuget.org within 20 minutes."
            return 1
        fi
        sleep 30
    done
    echo "$package $version is on nuget.org; running the consumer again."
    dotnet run --project "$project" -p:RestoreNoCache=true > "$log" 2>&1
}

# Exercise the actual packaged scenario entry points without any direct Arc reference. Merely restoring
# the package or resolving the testing assembly does not catch missing kernel runtime dependencies.
echo "Starting Chronicle $CHRONICLE_VERSION testing scenarios with only the Testing package referenced..."
if run_consumer "$WORK_DIR/TestingOnly/Consumer.csproj" "$WORK_DIR/TestingOnly/run.log"; then
    tail -1 "$WORK_DIR/TestingOnly/run.log"
else
    echo "::error::Chronicle $CHRONICLE_VERSION testing scenarios failed with only the Testing package referenced."
    echo "--- output ---"
    tail -30 "$WORK_DIR/TestingOnly/run.log"
    exit 1
fi

for consumer in Testing XUnitIntegration; do
    echo "Installing Chronicle $CHRONICLE_VERSION ($consumer) beside the current Arc packages and starting it..."
    if run_consumer "$WORK_DIR/$consumer/Consumer.csproj" "$WORK_DIR/$consumer/run.log"; then
        tail -1 "$WORK_DIR/$consumer/run.log"
        echo "Chronicle $CHRONICLE_VERSION ($consumer) and the current Arc packages start together."
    else
        echo "::error::Chronicle $CHRONICLE_VERSION ($consumer) and the current Arc packages failed consumer startup. Inspect the exception below before assigning the failure to either package family."
        echo "--- output ---"
        tail -30 "$WORK_DIR/$consumer/run.log"
        exit 1
    fi
done
