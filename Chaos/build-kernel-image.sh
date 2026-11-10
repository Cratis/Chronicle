#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

# Run only through pi-phase run --kind build, after the disk hold is lifted.
cd "$(dirname "$0")/.."
image=cratis/chronicle:local-development-slim
if [[ $# -gt 1 || ( $# -eq 1 && "$1" != --force ) ]]; then
    echo 'Usage: bash Chaos/build-kernel-image.sh [--force]' >&2
    exit 1
fi
if [[ $# -eq 0 ]] && docker image inspect --format '{{.Id}}' "$image" >/dev/null 2>&1; then
    echo "$image already exists; use --force to rebuild"
    exit 0
fi
rid=linux-x64
if [[ "$(uname -m)" == arm64 || "$(uname -m)" == aarch64 ]]; then
    rid=linux-arm64
fi
# PublishKernel uses --no-restore, so restore that same runtime first.
dotnet restore Source/Kernel/Server/Server.csproj -r "$rid" -p:DisableDockerBuild=true
dotnet msbuild Integration/LocalKernelPublish.proj -t:PublishKernel -p:KernelPublishRid="$rid" -p:DisableDockerBuild=true
docker build -t "$image" -f Docker/LocalSlim/Dockerfile .
