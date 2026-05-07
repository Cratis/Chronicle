#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Script to generate proto files locally from the Contracts project and copy them into the Kotlin project.
# Usage: ./generate-protos.sh
# Run from the Source/Clients/Kotlin directory or the repository root.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

CONTRACTS_PROJECT="$REPO_ROOT/Source/Kernel/Contracts/Contracts.csproj"
PROTO_GENERATOR_PROJECT="$REPO_ROOT/Source/Tools/ProtoGenerator/ProtoGenerator.csproj"
PROTOBUF_OUTPUT_DIR="$REPO_ROOT/Source/Kernel/Protobuf"
KOTLIN_PROTO_DIR="$SCRIPT_DIR/src/main/proto"

echo "Building Contracts..."
dotnet build "$CONTRACTS_PROJECT" -c Release

CONTRACTS_DLL="$REPO_ROOT/Source/Kernel/Contracts/bin/Release/net10.0/Cratis.Chronicle.Contracts.dll"

echo "Generating proto files..."
dotnet run --project "$PROTO_GENERATOR_PROJECT" -- "$CONTRACTS_DLL" "$PROTOBUF_OUTPUT_DIR"

echo "Copying proto files into Kotlin project..."
mkdir -p "$KOTLIN_PROTO_DIR"
cp -r "$PROTOBUF_OUTPUT_DIR"/* "$KOTLIN_PROTO_DIR/"

echo "Proto files generated and copied to $KOTLIN_PROTO_DIR"
