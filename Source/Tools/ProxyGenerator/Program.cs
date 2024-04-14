// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Cratis.ProxyGenerator;
using Cratis.ProxyGenerator.Templates;
using Microsoft.AspNetCore.Mvc;

var stopwatch = Stopwatch.StartNew();

var assemblyFile = "../../API/bin/Debug/net8.0/Cratis.Api.dll";
assemblyFile = Path.GetFullPath(assemblyFile);
var assemblyFolder = Path.GetDirectoryName(assemblyFile)!;

var assembly = Assembly.LoadFile(assemblyFile);
var assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly)!;
assemblyLoadContext.Resolving += (context, name) =>
{
    var assemblyPath = Path.Combine(assemblyFolder, name.Name + ".dll");
    return Assembly.LoadFile(assemblyPath);
};

var commands = new List<MethodInfo>();
var queries = new List<MethodInfo>();

var controllers = assembly.DefinedTypes.Where(_ => _.IsAssignableTo(typeof(ControllerBase)));
foreach (var controller in controllers)
{
    var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance);

    methods.Where(_ => _.IsQueryMethod()).ToList().ForEach(queries.Add);
    methods.Where(_ => _.IsCommandMethod()).ToList().ForEach(commands.Add);
}

var targetPath = Path.GetFullPath("../../Workbench/API");
if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

var commandDescriptors = commands.ConvertAll(_ => _.ToCommandDescriptor());
foreach (var command in commandDescriptors)
{
    var path = command.Controller.ResolveTargetPath();
    var fullPath = Path.Join(targetPath, path, $"{command.Name}.ts");
    var directory = Path.GetDirectoryName(fullPath)!;
    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    var proxyContent = TemplateTypes.Command(command);
    await File.WriteAllTextAsync(fullPath, proxyContent);
}

Console.WriteLine($"{commands.Count} commands in ${stopwatch.Elapsed}");
stopwatch.Restart();

var queryDescriptors = queries.ConvertAll(_ => _.ToQueryDescriptor());
foreach (var query in queryDescriptors)
{
    var path = query.Controller.ResolveTargetPath();
    var fullPath = Path.Join(targetPath, path, $"{query.Name}.ts");
    var directory = Path.GetDirectoryName(fullPath)!;
    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    var proxyContent = TemplateTypes.Query(query);
    await File.WriteAllTextAsync(fullPath, proxyContent);
}

Console.WriteLine($"{queries.Count} queries in ${stopwatch.Elapsed}");
stopwatch.Restart();

var typesInvolved = new List<Type>();
typesInvolved.AddRange(commandDescriptors.SelectMany(_ => _.TypesInvolved));
typesInvolved = typesInvolved.Distinct().ToList();

var typeDescriptors = typesInvolved.ConvertAll(_ => _.ToTypeDescriptor());
foreach (var type in typeDescriptors)
{
    var path = type.Type.ResolveTargetPath();
    var fullPath = Path.Join(targetPath, path, $"{type.Name}.ts");
    var directory = Path.GetDirectoryName(fullPath)!;
    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
    var proxyContent = TemplateTypes.Type(type);
    await File.WriteAllTextAsync(fullPath, proxyContent);
}

Console.WriteLine($"{typesInvolved.Count} types in ${stopwatch.Elapsed}");
