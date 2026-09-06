#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Regenerates the .proto files and chronicle.desc in this directory from the Contracts assembly.
#
# Building Contracts already does this - the GenerateProtoFiles target in Contracts.csproj runs after every build -
# so this script exists only to do it on demand without thinking about which project to build. Either way the
# generator deletes what was here before writing: these files are generated, and a hand-applied edit to one of them
# does not survive the next build. See .agents/PROJECT.md.
#
# Usage: ./generate-protos.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

dotnet build "$REPO_ROOT/Source/Kernel/Contracts/Contracts.csproj" -c Release

echo "Proto files and descriptor set regenerated in $SCRIPT_DIR"
