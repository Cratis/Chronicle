#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

PROTOBUF_SOURCE_DIR="$REPO_ROOT/Source/Kernel/Protobuf"
ELIXIR_PROTO_DIR="$SCRIPT_DIR/priv/protos"
GENERATED_OUTPUT_DIR="$SCRIPT_DIR/lib/generated"

echo "Syncing local proto files from $PROTOBUF_SOURCE_DIR to $ELIXIR_PROTO_DIR..."
mkdir -p "$ELIXIR_PROTO_DIR"
rsync -a --delete "$PROTOBUF_SOURCE_DIR/" "$ELIXIR_PROTO_DIR/"

echo "Generating Elixir gRPC sources from proto files..."
rm -rf "$GENERATED_OUTPUT_DIR"
mkdir -p "$GENERATED_OUTPUT_DIR"

cd "$SCRIPT_DIR"
mix deps.get

PROTO_FILES=$(find priv/protos -name "*.proto" | sort)
mix protobuf.generate \
  --output-path=./lib/generated \
  --include-path=./priv/protos \
  --plugin=ProtobufGenerate.Plugins.GRPC \
  $PROTO_FILES

echo "Elixir gRPC sources generated in $GENERATED_OUTPUT_DIR"
