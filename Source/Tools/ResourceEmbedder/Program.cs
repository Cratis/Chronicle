// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

var assemblyPath = args[0];
var resourceFile = args[1];
var tempAssemblyPath = Path.GetTempFileName();

Console.WriteLine($"Injecting resource {resourceFile} into {assemblyPath}");

var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
var resourceBytes = await File.ReadAllBytesAsync(resourceFile);
var resourceName = Path.GetFileName(resourceFile);
var resource = new EmbeddedResource(resourceName, ManifestResourceAttributes.Public, resourceBytes);
assembly.MainModule.Resources.Add(resource);
assembly.Write(tempAssemblyPath);
assembly.Dispose();

File.Copy(tempAssemblyPath, assemblyPath, true);

Console.WriteLine($"Resource {resourceName} injected successfully.");
