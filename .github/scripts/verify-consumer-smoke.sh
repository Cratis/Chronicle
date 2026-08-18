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
# Both testing packages are included: they carry their own type registrations, and a mismatch there
# reaches every consumer's specs rather than their production code.
cat > "$WORK_DIR/Consumer.csproj" <<PROJECT
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

echo "Installing Chronicle $CHRONICLE_VERSION beside the current Arc packages and starting it..."
if dotnet run --project "$WORK_DIR/Consumer.csproj" > "$WORK_DIR/run.log" 2>&1; then
    tail -1 "$WORK_DIR/run.log"
    echo "Chronicle $CHRONICLE_VERSION and the current Arc packages start together."
    exit 0
fi

echo "::error::Chronicle $CHRONICLE_VERSION and the current Arc packages do not work together. An application installing both would fail on startup, before any of its own code runs. If Arc is behind, bump and release Arc rather than changing Chronicle."
echo "--- output ---"
tail -30 "$WORK_DIR/run.log"
exit 1
