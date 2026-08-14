#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Script to generate gRPC schema from Contracts project
# Usage: ./generate-grpc-schema.sh <output-file> [repo-root]

set -e

OUTPUT_FILE="${1:-grpc-schema.proto}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Allow repo root to be specified as second argument, otherwise auto-detect
if [ -n "$2" ]; then
    REPO_ROOT="$(cd "$2" && pwd)"
else
    REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
fi

echo "Generating gRPC schema from Contracts..."
echo "Repository root: $REPO_ROOT"

# Create a temporary project inside the repo root so MSBuild can find Directory.Packages.props
TEMP_DIR=$(mktemp -d "$REPO_ROOT/.grpc-schema-gen-XXXXXX")
trap "rm -rf $TEMP_DIR" EXIT

# Create an empty Directory.Build.props to prevent inheriting repo-wide build settings
# (e.g. analyzer packages that expect CPM). The referenced Contracts.csproj is inside
# the repo and will still pick up the real Directory.Build.props from its own directory
# traversal.
cat > "$TEMP_DIR/Directory.Build.props" << 'EOF'
<Project />
EOF

cat > "$TEMP_DIR/SchemaGenerator.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$REPO_ROOT/Source/Kernel/Contracts/Contracts.csproj" />
    <PackageReference Include="protobuf-net.Grpc.Reflection" Version="1.2.2" />
  </ItemGroup>
</Project>
EOF

cat > "$TEMP_DIR/Program.cs" << 'EOF'
using System.Reflection;
using System.Text;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

// Discover all gRPC service interfaces by reflection so the schema stays in
// sync with the Contracts assembly regardless of how services move between
// namespaces. Deterministic ordering keeps baseline/current schemas comparable.
var assembly = Assembly.Load("Cratis.Chronicle.Contracts");
var serviceTypesByPackage = assembly.ExportedTypes
    .Where(_ => _.IsInterface && Attribute.IsDefined(_, typeof(ServiceAttribute)))
    .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
    .ThenBy(_ => _.Name, StringComparer.Ordinal)
    .GroupBy(_ => _.Namespace ?? "default");

var combinedSchema = new StringBuilder();
combinedSchema.AppendLine("syntax = \"proto3\";");
combinedSchema.AppendLine();

foreach (var group in serviceTypesByPackage)
{
    var generator = new SchemaGenerator
    {
        ProtoSyntax = ProtoSyntax.Proto3
    };

    try
    {
        var schema = generator.GetSchema(group.ToArray());

        // Remove the syntax line from individual schemas as we add it once at the top
        var lines = schema.Split('\n');
        foreach (var line in lines)
        {
            if (!line.StartsWith("syntax =") && !string.IsNullOrWhiteSpace(line))
            {
                combinedSchema.AppendLine(line);
            }
        }

        combinedSchema.AppendLine();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error generating schema for package {group.Key}: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

Console.WriteLine(combinedSchema.ToString());
EOF

# Build and run the schema generator
cd "$TEMP_DIR"
echo "Building schema generator..."
dotnet build --verbosity minimal 2>&1

echo "Running schema generator..."
dotnet run --no-build --verbosity quiet > temp-output.txt 2>&1 || {
    echo "Error running schema generator"
    cat temp-output.txt
    exit 1
}

# Determine the final output path
if [[ "$OUTPUT_FILE" == /* ]]; then
    # Absolute path
    FINAL_OUTPUT="$OUTPUT_FILE"
else
    # Relative path - relative to repo root or current directory
    if [ -n "$REPO_ROOT" ]; then
        FINAL_OUTPUT="$REPO_ROOT/$OUTPUT_FILE"
    else
        FINAL_OUTPUT="$OLDPWD/$OUTPUT_FILE"
    fi
fi

# Copy the output file
mkdir -p "$(dirname "$FINAL_OUTPUT")"
mv temp-output.txt "$FINAL_OUTPUT"

echo "Schema generated successfully: $FINAL_OUTPUT"

