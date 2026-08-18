// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Tools.GrpcCodeGenerator;

Console.WriteLine("\nGrpc Code Generator\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: GrpcCodeGenerator <assembly> <output-directory> [options]");
    Console.WriteLine();
    Console.WriteLine("  --skip-namespaces <n>              Leading namespace segments to drop from the artifact namespace.");
    Console.WriteLine("  --base-namespace <ns>              Base namespace for the generated contracts.");
    Console.WriteLine("  --implementations <dir>            Where to write the generated service implementations.");
    Console.WriteLine("  --implementations-namespace <ns>   Base namespace for the generated implementations.");
    Console.WriteLine("  --registrations <file>             Where to write the generated service registrations.");
    Console.WriteLine("  --registrations-namespace <ns>     Namespace for the generated registrations.");
    Console.WriteLine("  --exclude <a,b,c>                  Services that are not derived - neither contract nor implementation.");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var outputDirectory = args[1];
var skipNamespaces = 0;
var baseNamespace = string.Empty;
var implementationsDirectory = string.Empty;
var implementationsNamespace = string.Empty;
var registrationsFile = string.Empty;
var registrationsNamespace = string.Empty;
var excluded = new HashSet<string>(StringComparer.Ordinal);

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
        case "--implementations":
            implementationsDirectory = args[++i];
            break;
        case "--implementations-namespace":
            implementationsNamespace = args[++i];
            break;
        case "--registrations":
            registrationsFile = args[++i];
            break;
        case "--registrations-namespace":
            registrationsNamespace = args[++i];
            break;
        case "--exclude":
            excluded.UnionWith(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            break;
    }
}

if (!File.Exists(assemblyPath))
{
    Console.WriteLine($"Assembly not found: {assemblyPath}");
    Environment.Exit(1);
}

Directory.CreateDirectory(outputDirectory);

var loadContext = new IsolatedAssemblyLoadContext(assemblyPath);

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
var generatesImplementations = !string.IsNullOrEmpty(implementationsDirectory);
var implementationGenerator = new ServiceImplementationGenerator(skipNamespaces, baseNamespace, implementationsNamespace);
var generated = new List<GeneratedService>();
var hasError = false;

foreach (var (_, serviceDefinition) in serviceGroups)
{
    Console.WriteLine($"\nService: {serviceDefinition.ServiceName} (namespace: {serviceDefinition.Namespace})");
    Console.WriteLine($"  Commands: {serviceDefinition.Commands.Count}");
    Console.WriteLine($"  Queries: {serviceDefinition.Queries.Count}");

    if (excluded.Contains(serviceDefinition.ServiceName))
    {
        Console.WriteLine($"  Skipped {serviceDefinition.ServiceName} - it is not derived yet and keeps its hand-written contract and implementation.");
        continue;
    }

    try
    {
        generator.Generate(serviceDefinition, outputDirectory);
        Console.WriteLine($"  Generated interface for {serviceDefinition.ServiceName}");
    }
    catch (NamespaceMismatchException ex)
    {
        Console.Error.WriteLine($"  ERROR: Namespace mismatch in service '{serviceDefinition.ServiceName}': {ex.Message}");
        hasError = true;
        continue;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR generating service '{serviceDefinition.ServiceName}': {ex.Message}");
        hasError = true;
        continue;
    }

    if (!generatesImplementations)
    {
        continue;
    }

    try
    {
        var service = implementationGenerator.Generate(serviceDefinition, implementationsDirectory);
        generated.Add(service);
        Console.WriteLine($"  Generated implementation for {serviceDefinition.ServiceName}");
    }
    catch (UnsupportedServiceShape ex)
    {
        Console.Error.WriteLine($"  ERROR implementing service '{serviceDefinition.ServiceName}': {ex.Message}");
        hasError = true;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR implementing service '{serviceDefinition.ServiceName}': {ex.Message}");
        hasError = true;
    }
}

if (hasError)
{
    Environment.Exit(1);
}

if (generatesImplementations && !string.IsNullOrEmpty(registrationsFile))
{
    new ServiceRegistrationsGenerator(registrationsNamespace).Generate(generated, registrationsFile);
    Console.WriteLine($"\nGenerated registrations for {generated.Count} service(s).");
}

Console.WriteLine("\nGeneration complete.");
