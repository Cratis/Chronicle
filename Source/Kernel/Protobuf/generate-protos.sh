#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Generates .proto files from the Contracts assembly into Source/Kernel/Protobuf.
# Usage: ./generate-protos.sh
# Run from Source/Kernel/Protobuf or repository root.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

CONTRACTS_PROJECT="$REPO_ROOT/Source/Kernel/Contracts/Contracts.csproj"
PROTO_GENERATOR_PROJECT="$REPO_ROOT/Source/Tools/ProtoGenerator/ProtoGenerator.csproj"
PROTOBUF_OUTPUT_DIR="$SCRIPT_DIR"

echo "Building Contracts..."
dotnet build "$CONTRACTS_PROJECT" -c Release

CONTRACTS_DLL="$REPO_ROOT/Source/Kernel/Contracts/bin/Release/net10.0/Cratis.Chronicle.Contracts.dll"

echo "Generating proto files into $PROTOBUF_OUTPUT_DIR..."
dotnet run --project "$PROTO_GENERATOR_PROJECT" -- "$CONTRACTS_DLL" "$PROTOBUF_OUTPUT_DIR"

echo "Proto files generated in $PROTOBUF_OUTPUT_DIR"