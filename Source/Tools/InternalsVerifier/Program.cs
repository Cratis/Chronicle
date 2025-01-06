// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using InternalsVerifier;
using Mono.Cecil;

Console.WriteLine("\nInternals Verifier\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: InternalsVerifier <assembly> <internal namespaces - separated by ;>");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var internalNamespaces = args[1].Split(';');

Console.WriteLine($"Verifying internals for {assemblyPath}");

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = true });

var violationCount = 0;
foreach (var @namespace in internalNamespaces)
{
    var namespaceCount = 0;
    Console.WriteLine($"Checking namespace '{@namespace}'");

    // We're looking for types that has a namespace that starts with the namespace we're looking for and is Public - meaning available for any consumer
    // We want to ignore types that are in the same namespace as the assembly itself, as they are not internal to the assembly
    // This is based on a convention and assumption that assembly == namespace, pretty much
    foreach (var internalType in assembly.MainModule.Types.Where(_ =>
        _.Namespace.StartsWith(@namespace) &&
        !_.Namespace.StartsWith(assembly.Name.Name) &&
        _.IsPublic).ToArray())
    {
        violationCount++;
        namespaceCount++;
        var typesReferencingInternalType = assembly.GetTypesReferencingInternalType(internalType, @namespace);
        if (typesReferencingInternalType.Length > 0)
        {
            Console.WriteLine($"\nInternal type '{internalType.FullName}' is used directly or indirectly by the following types:");
            foreach (var type in typesReferencingInternalType)
            {
                Console.WriteLine($"  {type.FullName}");
            }
        }
        else
        {
            Console.WriteLine($"\nInternal type '{internalType.FullName}' is used directly or indirectly by an unknown type.");
        }

        Console.WriteLine();
    }

    if (namespaceCount == 0)
    {
        Console.WriteLine("\nNo internal type violations found\n");
    }
}

if (violationCount > 0)
{
    Console.WriteLine($"\nFound {violationCount} violations");
    Console.WriteLine("\n\nTypes listed as using directly or indirectly is an approximation and may not be complete.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("------------------------------------");
    Console.WriteLine("No violations found");
}
