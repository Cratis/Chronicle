#!/bin/bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Script to generate gRPC schema from Contracts project
# Usage: ./generate-grpc-schema.sh <output-file>

set -e

OUTPUT_FILE="${1:-grpc-schema.proto}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "Generating gRPC schema from Contracts..."
echo "Repository root: $REPO_ROOT"

# Create a temporary project to generate the schema
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

cat > "$TEMP_DIR/SchemaGenerator.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$REPO_ROOT/Source/Kernel/Contracts/Contracts.csproj" />
    <PackageReference Include="protobuf-net.Grpc.Reflection" Version="1.2.2" />
  </ItemGroup>
</Project>
EOF

cat > "$TEMP_DIR/Program.cs" << 'EOF'
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;
using System.Text;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Host;
using Cratis.Chronicle.Contracts.Identities;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Contracts.Seeding;

// Group service types by their namespace (package)
var serviceTypesByPackage = new Dictionary<string, List<Type>>
{
    ["Cratis.Chronicle.Contracts"] = new List<Type> { typeof(IEventStores), typeof(INamespaces) },
    ["Cratis.Chronicle.Contracts.Clients"] = new List<Type> { typeof(IConnectionService) },
    ["Cratis.Chronicle.Contracts.Events"] = new List<Type> { typeof(IEventTypes) },
    ["Cratis.Chronicle.Contracts.Events.Constraints"] = new List<Type> { typeof(IConstraints) },
    ["Cratis.Chronicle.Contracts.EventSequences"] = new List<Type> { typeof(IEventSequences) },
    ["Cratis.Chronicle.Contracts.Host"] = new List<Type> { typeof(IServer) },
    ["Cratis.Chronicle.Contracts.Identities"] = new List<Type> { typeof(IIdentities) },
    ["Cratis.Chronicle.Contracts.Jobs"] = new List<Type> { typeof(IJobs) },
    ["Cratis.Chronicle.Contracts.Observation"] = new List<Type> { typeof(IObservers), typeof(IFailedPartitions) },
    ["Cratis.Chronicle.Contracts.Observation.Reactors"] = new List<Type> { typeof(IReactors) },
    ["Cratis.Chronicle.Contracts.Observation.Reducers"] = new List<Type> { typeof(IReducers) },
    ["Cratis.Chronicle.Contracts.Observation.Webhooks"] = new List<Type> { typeof(IWebhooks) },
    ["Cratis.Chronicle.Contracts.Projections"] = new List<Type> { typeof(IProjections) },
    ["Cratis.Chronicle.Contracts.ReadModels"] = new List<Type> { typeof(IReadModels) },
    ["Cratis.Chronicle.Contracts.Recommendations"] = new List<Type> { typeof(IRecommendations) },
    ["Cratis.Chronicle.Contracts.Security"] = new List<Type> { typeof(IApplications), typeof(IUsers) },
    ["Cratis.Chronicle.Contracts.Seeding"] = new List<Type> { typeof(IEventSeeding) }
};

var combinedSchema = new StringBuilder();
combinedSchema.AppendLine("syntax = \"proto3\";");
combinedSchema.AppendLine();

foreach (var kvp in serviceTypesByPackage)
{
    var generator = new SchemaGenerator
    {
        ProtoSyntax = ProtoSyntax.Proto3
    };
    
    try
    {
        var schema = generator.GetSchema(kvp.Value.ToArray());
        
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
        Console.Error.WriteLine($"Error generating schema for package {kvp.Key}: {ex.Message}");
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
mv temp-output.txt "$FINAL_OUTPUT"

echo "Schema generated successfully: $FINAL_OUTPUT"

