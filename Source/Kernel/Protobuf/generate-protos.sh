#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Regenerates the .proto files and chronicle.desc in this directory from the Contracts assembly.
#
# Building Contracts does this when its inputs change. This script forces generation on demand even with cached
# outputs: stale files are removed and hand-applied edits overwritten, while unchanged files keep their timestamps.
# These files are generated, and a hand-applied edit does not survive the next build. See .agents/PROJECT.md.
#
# Usage: ./generate-protos.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

dotnet build "$REPO_ROOT/Source/Kernel/Contracts/Contracts.csproj" -c Release -p:ForceCodeGeneration=true

echo "Proto files and descriptor set regenerated in $SCRIPT_DIR"
