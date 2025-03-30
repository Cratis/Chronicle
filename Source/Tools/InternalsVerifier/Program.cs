// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using InternalsVerifier;
using Mono.Cecil;

Console.WriteLine("\nInternals Verifier\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: InternalsVerifier <assembly> <internal assemblies - separated by ;>");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var internalAssemblies = args[1].Split(';');

Console.WriteLine($"Verifying internals for {assemblyPath}");

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = true });

var violationCount = 0;
var violatingTypesCount = 0;
foreach (var internalAssemblyPath in internalAssemblies.Where(File.Exists))
{
    Console.WriteLine($"\nChecking for internal type usage for '{internalAssemblyPath}'");
    var internalAssembly = AssemblyDefinition.ReadAssembly(internalAssemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = true });

    foreach (var internalType in internalAssembly.MainModule.Types.Where(type => type.IsNotPublic && !type.FullName.Contains('<')).ToArray())
    {
        violationCount++;
        var typesReferencingInternalType = assembly.GetTypesReferencingInternalType(internalType);
        if (typesReferencingInternalType.Length > 0)
        {
            Console.WriteLine($"\nInternal type '{internalType.FullName}' is used directly or indirectly by the following types:");
            foreach (var type in typesReferencingInternalType)
            {
                Console.WriteLine($"  {type.FullName}");
                violatingTypesCount++;
            }
        }
    }
}

if (violatingTypesCount > 0)
{
    Console.WriteLine($"\nFound {violationCount} violated types");
    Console.WriteLine("\n\nTypes listed as using directly or indirectly is an approximation and may not be complete.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("------------------------------------");
    Console.WriteLine("No violations found");
}
