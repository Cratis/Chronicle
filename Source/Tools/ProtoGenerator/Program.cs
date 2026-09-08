// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Tools.ProtoGenerator;

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

IReadOnlyDictionary<string, string> schemas;

try
{
    schemas = ProtoSchemaGeneration.Generate(Assembly.LoadFrom(assemblyPath));
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to generate: {ex.Message}");
    Environment.Exit(1);
    return;
}

foreach (var path in ProtoSchemaGeneration.Write(schemas, outputDirectory))
{
    Console.WriteLine($"Generated proto file: {path}");
}

Console.WriteLine("\nWriting canonical descriptor set...");

var descriptorErrors = DescriptorSetWriter.Write(outputDirectory);
foreach (var error in descriptorErrors)
{
    Console.WriteLine($"  {(error.IsError ? "error" : "warning")}: {error}");
}

if (Array.Exists(descriptorErrors, _ => _.IsError))
{
    Console.WriteLine($"Failed to write {DescriptorSetWriter.FileName} - the generated proto files do not parse.");
    Environment.Exit(1);
}

Console.WriteLine($"Generated descriptor set: {Path.Combine(outputDirectory, DescriptorSetWriter.FileName)}");
