// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Cratis.ProxyGenerator;
using Cratis.ProxyGenerator.Templates;
using Microsoft.AspNetCore.Mvc;

var overallStopwatch = Stopwatch.StartNew();

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

var typesInvolved = new List<Type>();
var directories = new List<string>();

var commandDescriptors = commands.ConvertAll(_ => _.ToCommandDescriptor(targetPath));
await commandDescriptors.Write(targetPath, typesInvolved, TemplateTypes.Command, directories, "commands");

var queryDescriptors = queries.ConvertAll(_ => _.ToQueryDescriptor(targetPath));
var enumerableQueries = queryDescriptors.Where(_ => _.IsEnumerable).ToList();
await enumerableQueries.Write(targetPath, typesInvolved, TemplateTypes.Query, directories, "queries");
var observableQueries = queryDescriptors.Where(_ => _.IsObservable).ToList();
await observableQueries.Write(targetPath, typesInvolved, TemplateTypes.ObservableQuery, directories, "observable queries");

typesInvolved = typesInvolved.Distinct().ToList();
var enums = typesInvolved.Where(_ => _.IsEnum).ToList();

var typeDescriptors = typesInvolved.Where(_ => !enums.Contains(_)).ToList().ConvertAll(_ => _.ToTypeDescriptor(targetPath));
await typeDescriptors.Write(targetPath, typesInvolved, TemplateTypes.Type, directories, "types");

var enumDescriptors = enums.ConvertAll(_ => _.ToEnumDescriptor());
await enumDescriptors.Write(targetPath, typesInvolved, TemplateTypes.Enum, directories, "enums");

var stopwatch = Stopwatch.StartNew();
var directoriesWithContent = directories.Distinct().Select(_ => new DirectoryInfo(_));
foreach (var directory in directoriesWithContent)
{
    var exports = directory.GetFiles("*.ts").Select(_ => $"./{Path.GetFileNameWithoutExtension(_.Name)}");
    var descriptor = new IndexDescriptor(exports);
    var content = TemplateTypes.Index(descriptor);
    await File.WriteAllTextAsync(Path.Join(directory.FullName, "index.ts"), content);
}

Console.WriteLine($"{directoriesWithContent.Count()} index files written in {stopwatch.Elapsed}");

Console.WriteLine($"Overall time: {overallStopwatch.Elapsed}");
