// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

/// <summary>
/// Creates Roslyn projects for analyzer and code fix tests.
/// </summary>
public static class TestProject
{
    /// <summary>
    /// Create an in-memory project for the supplied source.
    /// </summary>
    /// <param name="source">The source to include.</param>
    /// <returns>The created project.</returns>
    public static Project CreateProject(string source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "TestProject",
            "TestProject",
            LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview));

        var workspace = new AdhocWorkspace();
        var solution = workspace.CurrentSolution
            .AddProject(projectInfo)
            .AddMetadataReferences(projectId, GetMetadataReferences())
            .AddDocument(documentId, "Test0.cs", SourceText.From(source));

        return solution.GetProject(projectId)!;
    }

    static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        return
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Immutable.ImmutableArray).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location)
        ];
    }
}
