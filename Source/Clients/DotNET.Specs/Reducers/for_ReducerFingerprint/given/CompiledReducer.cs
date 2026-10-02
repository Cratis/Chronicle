// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Chronicle.Reducers.for_ReducerFingerprint.given;

public static class CompiledReducer
{
    public const string Simple = "public ReadModel Reduce(Event @event, ReadModel current) => new(current.Value + @event.Value);";

    static readonly MetadataReference[] _references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    public static Type Compile(string members, string version = "1.0.0.0", string before = "", string model = "ReadModel", string culture = "")
    {
        var source = $$"""
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
        var compilation = CSharpCompilation.Create(
            "FingerprintFixture",
            [CSharpSyntaxTree.ParseText(source)],
            _references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, allowUnsafe: true));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(stream.ToArray()).GetType("FingerprintFixture.Reducer")!;
    }
}
