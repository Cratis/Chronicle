// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;
using Cratis.Chronicle.Tools.GrpcCodeGenerator;

Console.WriteLine("\nGrpc Code Generator\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: GrpcCodeGenerator <assembly> <output-directory> [--skip-namespaces <n>] [--base-namespace <ns>]");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var outputDirectory = args[1];
var skipNamespaces = 0;
var baseNamespace = string.Empty;

for (var i = 2; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--skip-namespaces":
            skipNamespaces = int.Parse(args[++i]);
            break;
        case "--base-namespace":
            baseNamespace = args[++i];
            break;
    }
}

if (!File.Exists(assemblyPath))
{
    Console.WriteLine($"Assembly not found: {assemblyPath}");
    Environment.Exit(1);
}

Directory.CreateDirectory(outputDirectory);

var loadContext = new AssemblyLoadContext("GrpcCodeGenerator", isCollectible: true);
loadContext.Resolving += (context, name) =>
{
    var assemblyDir = Path.GetDirectoryName(assemblyPath)!;
    var dllPath = Path.Combine(assemblyDir, $"{name.Name}.dll");

    if (File.Exists(dllPath))
    {
        return context.LoadFromAssemblyPath(dllPath);
    }

    return null;
};

Assembly assembly;
try
{
    assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load assembly: {ex.Message}");
    Environment.Exit(1);
    return;
}

var typeDiscovery = new TypeDiscovery(assembly);
var serviceGroups = typeDiscovery.DiscoverServices();

if (serviceGroups.Count == 0)
{
    Console.WriteLine("No services found. Make sure commands and queries have [BelongsTo] attribute.");
    return;
}

Console.WriteLine($"Found {serviceGroups.Count} service group(s)");

var generator = new ServiceInterfaceGenerator(skipNamespaces, baseNamespace);
var hasError = false;

foreach (var (_, serviceDefinition) in serviceGroups)
{
    Console.WriteLine($"\nService: {serviceDefinition.ServiceName} (namespace: {serviceDefinition.Namespace})");
    Console.WriteLine($"  Commands: {serviceDefinition.Commands.Count}");
    Console.WriteLine($"  Queries: {serviceDefinition.Queries.Count}");

    try
    {
        generator.Generate(serviceDefinition, outputDirectory);
        Console.WriteLine($"  Generated interface for {serviceDefinition.ServiceName}");
    }
    catch (NamespaceMismatchException ex)
    {
        Console.Error.WriteLine($"  ERROR: Namespace mismatch in service '{serviceDefinition.ServiceName}': {ex.Message}");
        hasError = true;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR generating service '{serviceDefinition.ServiceName}': {ex.Message}");
        hasError = true;
    }
}

if (hasError)
{
    Environment.Exit(1);
}

Console.WriteLine("\nGeneration complete.");
