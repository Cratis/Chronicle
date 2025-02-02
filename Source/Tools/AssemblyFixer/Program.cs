// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;

Console.WriteLine("\nAssembly fixer\n");

if (args.Length < 2)
{
    Console.WriteLine("Usage: AssemblyFixer <assembly> <internal assembly names - separated by ;>");
    Environment.Exit(1);
}

var assemblyPath = args[0];
var assembliesToFix = args[1].Split(';');
var tempAssemblyPath = Path.GetTempFileName();

Console.WriteLine($"Fixing assembly {assemblyPath} - removing ApplicationPart attribute for assemblies");
assembliesToFix.ForEach(_ => Console.WriteLine($"  Assembly assembly: {_}"));

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true, ReadSymbols = true });
var customAttributes = assembly.CustomAttributes.Where(_ => _.AttributeType.FullName == "Orleans.ApplicationPartAttribute");
var attributesToRemove = customAttributes.Where(attribute =>
{
    var argument = attribute.ConstructorArguments[0].Value.ToString() ?? string.Empty;
    return attribute.ConstructorArguments.Count == 1 && assembliesToFix.Any(ia => ia == argument);
}).ToList();

attributesToRemove.ForEach(attribute =>
{
    Console.WriteLine($"Removing assembly part attribute for '{attribute.ConstructorArguments[0].Value}'");
    assembly.CustomAttributes.Remove(attribute);
});

assembly.Write(tempAssemblyPath, new WriterParameters { WriteSymbols = true, SymbolWriterProvider = new PortablePdbWriterProvider() });
assembly.Dispose();

File.Copy(tempAssemblyPath, assemblyPath, true);
