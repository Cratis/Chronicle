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
string[] typesToIgnore = [];

if (args.Length == 3)
{
    typesToIgnore = args[2].Split(';');
    if (typesToIgnore.Length > 0)
    {
        Console.WriteLine($"Ignoring types: {string.Join(", ", typesToIgnore)}\n");
    }
}

Console.WriteLine($"Verifying internals for {assemblyPath}");

AssemblyDefinition assembly = null!;
var retryCount = 0;
const int maxRetries = 5;
const int retryDelay = 500;

while (retryCount < maxRetries)
{
    try
    {
        assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = true });
        break;
    }
    catch (IOException)
    {
        retryCount++;
        if (retryCount >= maxRetries)
        {
            Console.WriteLine("Failed to read the assembly after multiple attempts.");
            throw;
        }
        Console.WriteLine($"IOException encountered. Retrying {retryCount}/{maxRetries}...");
        await Task.Delay(retryDelay);
    }
}

var violationCount = 0;
var violatingTypesCount = 0;
internalAssemblies = internalAssemblies.Where(File.Exists).ToArray();
foreach (var internalAssemblyPath in internalAssemblies)
{
    var internalAssembly = AssemblyDefinition.ReadAssembly(internalAssemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = true });

    var internalTypes = internalAssembly.MainModule.Types.Where(type =>
        !type.FullName.Contains('<') &&
        !typesToIgnore.Any(ignore => ignore == type.FullName) &&
        !typesToIgnore.Any(ignore => ignore == type.Name)).ToArray();
    var typesToCheck = assembly.MainModule.Types.Where(type => !internalTypes.Any(internalType => internalType.FullName == type.FullName)).ToArray();
    foreach (var internalType in internalTypes)
    {
        var typesReferencingInternalType = typesToCheck.GetTypesReferencingInternalType(internalType);
        if (typesReferencingInternalType.Length > 0)
        {
            violationCount++;
            Console.WriteLine($"\nInternal type '{internalType.FullName}' is used with a public element by the following types:");
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
    Console.WriteLine("\nMake the element exposing the types internal for the type to remain internal.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("------------------------------------");
    Console.WriteLine("No violations found");
}
