// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Cil;

var assemblyPath = args[0];
var resourceFile = args[1];
var tempAssemblyPath = Path.GetTempFileName();

Console.WriteLine($"Injecting resource {resourceFile} into {assemblyPath}");

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
var resourceBytes = await File.ReadAllBytesAsync(resourceFile);
var resourceName = Path.GetFileName(resourceFile);
var resource = new EmbeddedResource(resourceName, ManifestResourceAttributes.Public, resourceBytes);
assembly.MainModule.Resources.Add(resource);

assembly.Write(tempAssemblyPath, new WriterParameters { WriteSymbols = false, SymbolWriterProvider = new PortablePdbWriterProvider() });
assembly.Dispose();

var tempPdbPath = Path.ChangeExtension(tempAssemblyPath, ".pdb");
var assemblyPdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
File.Copy(tempAssemblyPath, assemblyPath, true);
File.Copy(tempPdbPath, assemblyPdbPath, true);

Console.WriteLine($"Resource {resourceName} injected successfully.");
