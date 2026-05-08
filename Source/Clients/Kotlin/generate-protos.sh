#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Script to generate Kotlin gRPC client sources from local proto files.
# Usage: ./generate-protos.sh
# Run from the Source/Clients/Kotlin directory or the repository root.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

PROTOBUF_SOURCE_DIR="$REPO_ROOT/Source/Kernel/Protobuf"
KOTLIN_PROTO_DIR="$SCRIPT_DIR/src/main/proto"

echo "Syncing local proto files from $PROTOBUF_SOURCE_DIR to $KOTLIN_PROTO_DIR..."
mkdir -p "$KOTLIN_PROTO_DIR"
rsync -a --delete "$PROTOBUF_SOURCE_DIR/" "$KOTLIN_PROTO_DIR/"

echo "Generating Kotlin gRPC sources from proto files..."
GRADLE_WRAPPER_JAR="$SCRIPT_DIR/gradle/wrapper/gradle-wrapper.jar"
GRADLE_WRAPPER_PROPERTIES="$SCRIPT_DIR/gradle/wrapper/gradle-wrapper.properties"
if [[ -f "$GRADLE_WRAPPER_JAR" ]]; then
	GRADLE_CMD=("$SCRIPT_DIR/gradlew")
elif command -v gradle >/dev/null 2>&1; then
	echo "Gradle wrapper JAR not found. Falling back to system Gradle."
	GRADLE_CMD=(gradle)
else
	if [[ ! -f "$GRADLE_WRAPPER_PROPERTIES" ]]; then
		echo "Error: Missing gradle-wrapper.jar, no 'gradle' in PATH, and no gradle-wrapper.properties found."
		exit 1
	fi

	distribution_url=$(grep '^distributionUrl=' "$GRADLE_WRAPPER_PROPERTIES" | cut -d= -f2- | sed 's#\\:#:#g')
	if [[ -z "$distribution_url" ]]; then
		echo "Error: Could not read distributionUrl from $GRADLE_WRAPPER_PROPERTIES."
		exit 1
	fi

	gradle_bootstrap_root="$SCRIPT_DIR/.gradle-bootstrap"
	mkdir -p "$gradle_bootstrap_root"

	gradle_distribution_name=$(basename "$distribution_url" .zip)
	gradle_bootstrap_dir="$gradle_bootstrap_root/$gradle_distribution_name"
	gradle_home_dir="$gradle_bootstrap_dir/${gradle_distribution_name%-bin}"

	gradle_zip="$gradle_bootstrap_root/${gradle_distribution_name}.zip"
	echo "Gradle wrapper JAR not found and no system Gradle available."
	if [[ ! -x "$gradle_home_dir/bin/gradle" ]]; then
		echo "Bootstrapping Gradle from $distribution_url..."
		curl -fL -sS "$distribution_url" -o "$gradle_zip"
		rm -rf "$gradle_bootstrap_dir"
		mkdir -p "$gradle_bootstrap_dir"
		unzip -q "$gradle_zip" -d "$gradle_bootstrap_dir"
	fi

	if [[ ! -x "$gradle_home_dir/bin/gradle" ]]; then
		echo "Error: Failed to locate unpacked Gradle distribution."
		exit 1
	fi

	GRADLE_CMD=("$gradle_home_dir/bin/gradle")
fi

"${GRADLE_CMD[@]}" --no-daemon clean generateProto

echo "Kotlin gRPC sources generated in $SCRIPT_DIR/build/generated/source/proto/main"
