// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Tools.ProtoGenerator;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Reflection;

Console.WriteLine("\nProto Generator\n");

if (args.Length != 2)
{
    Console.WriteLine("Usage: ProtoGenerator <grpc contracts assembly> <output directory>");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var outputDirectory = args[1];

if (!File.Exists(assemblyPath))
{
    Console.WriteLine($"Assembly not found: {assemblyPath}");
    Environment.Exit(1);
}

Directory.CreateDirectory(outputDirectory);

var assembly = Assembly.LoadFrom(assemblyPath);
var services = assembly.ExportedTypes
    .Where(_ => _.IsInterface && Attribute.IsDefined(_, typeof(ServiceAttribute)))
    .ToArray();

Console.WriteLine($"Found {services.Length} service interfaces");

var generator = new SchemaGenerator();

// Group services by namespace to handle multiple packages
var servicesByNamespace = services.GroupBy(_ => _.Namespace);

foreach (var group in servicesByNamespace)
{
    var packageName = group.Key ?? "default";
    var packageServices = group.ToArray();

    Console.WriteLine($"Generating proto for package: {packageName} with {packageServices.Length} services");

    try
    {
        var schema = generator.GetSchema(packageServices);

        // Fix RPC method name conflicts where method name == input message type name.
        // In proto3 this causes a scoping ambiguity; fix by qualifying the input type with the package name.
        schema = ProtoSchemaHelper.FixRpcMethodNameConflicts(schema);

        // Fix enum value naming conflicts.
        // In proto3, enum values use C++ scoping rules and must be unique within the package.
        // When two enums in the same file share value names, prefix the conflicting values
        // with an UPPER_SNAKE_CASE version of their parent enum name.
        schema = ProtoSchemaHelper.FixEnumValueConflicts(schema);

        // Add ISO 8601 format comment to SerializableDateTimeOffset message definitions.
        schema = ProtoSchemaHelper.AddSerializableDateTimeOffsetComment(schema);

        var fileName = packageName.Replace("Cratis.Chronicle.Contracts.", string.Empty).Replace('.', '_').ToLowerInvariant();
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "chronicle";
        }
        var protoFilePath = Path.Combine(outputDirectory, $"{fileName}.proto");
        await File.WriteAllTextAsync(protoFilePath, schema);
        Console.WriteLine($"Generated proto file: {protoFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error generating proto for package {packageName}: {ex.Message}");
    }
}
