// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.given;

public static class CompiledReducer
{
    public const string Simple = "public ReadModel Reduce(Event @event, ReadModel current) => new(current.Value + @event.Value);";

    static readonly ConcurrentDictionary<byte[], Lazy<Assembly>> _dependencyAssemblies = new();
    static readonly MetadataReference[] _references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    public static Type Compile(string members, string version = "1.0.0.0", string before = "", string model = "ReadModel", string culture = "", (string Name, byte[] Image)[]? dependencies = null, bool deterministic = false)
    {
        dependencies ??= [];
        var source = $$"""
            {{string.Join(Environment.NewLine, dependencies.Select(_ => $"extern alias {_.Name};"))}}
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using Cratis.Chronicle.Reducers;
            [assembly: System.Reflection.AssemblyVersion("{{version}}")]
            [assembly: System.Reflection.AssemblyCulture("{{culture}}")]
            namespace FingerprintFixture;
            {{before}}
            public record Event(int Value);
            public record OtherEvent(int Value);
            public record ReadModel(int Value);
            public record OtherReadModel(int Value);
            public class Reducer : IReducerFor<{{model}}>
            {
                {{members}}
            }
            """;
        var references = dependencies.Select(_ => MetadataReference.CreateFromImage(_.Image, MetadataReferenceProperties.Assembly.WithAliases([_.Name])));
        var image = CompileAssembly("FingerprintFixture", source, references, deterministic);
        var assembly = Assembly.Load(image);
        AssemblyLoadContext.GetLoadContext(assembly)!.Resolving += (_, name) =>
        {
            var dependency = dependencies.SingleOrDefault(_ => _.Name == name.Name);
            return dependency.Image is null ? null : _dependencyAssemblies.GetOrAdd(dependency.Image, image => new Lazy<Assembly>(() => Assembly.Load(image))).Value;
        };
        return assembly.GetType("FingerprintFixture.Reducer")!;
    }

    public static byte[] CompileAssembly(string name, string source, IEnumerable<MetadataReference>? references = null, bool deterministic = false)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            _references.Concat(references ?? []),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, allowUnsafe: true, deterministic: deterministic));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return stream.ToArray();
    }
}
