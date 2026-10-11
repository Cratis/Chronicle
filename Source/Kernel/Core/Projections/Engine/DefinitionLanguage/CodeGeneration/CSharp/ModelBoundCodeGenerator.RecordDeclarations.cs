// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Reuses record declarations and identifies incompatible uses of a shared type.
/// </summary>
public partial class ModelBoundCodeGenerator
{
    static IEnumerable<RecordDeclarationSyntax> CollectUniqueRecordDeclarations(RecordScope scope)
    {
        var records = new RecordDeclarations();
        CollectRecordScope(scope, records);
        return records.Declarations.Select(record => records.Conflicts.Contains(record.Key)
            ? WithoutProjectionAttributes(record.Value)
            : record.Value);
    }

    static void CollectRecordScope(RecordScope scope, RecordDeclarations records)
    {
        if (!RegisterDeclaration(scope, records)) return;
        foreach (var child in ChildScopes(scope))
        {
            CollectRecordScope(child, records);
        }
    }

    static bool RegisterDeclaration(RecordScope scope, RecordDeclarations records)
    {
        var declaration = CreateRecordDeclaration(scope);
        if (!records.Declarations.TryGetValue(scope.Name, out var existing))
        {
            records.Declarations.Add(scope.Name, declaration);
            return true;
        }
        if (!declaration.IsEquivalentTo(existing)) records.Conflicts.Add(scope.Name);
        return scope.Definition.Children.Count > 0 || scope.Definition.Nested?.Count > 0;
    }

    static RecordDeclarationSyntax WithoutProjectionAttributes(RecordDeclarationSyntax declaration)
    {
        var parameters = declaration.ParameterList!.Parameters.Select(parameter => parameter.WithAttributeLists([]));
        return declaration.WithAttributeLists([])
            .WithParameterList(declaration.ParameterList.WithParameters(SeparatedList(parameters)))
            .WithLeadingTrivia(Comment($"// Not expressible with model-bound attributes: conflicting projection scopes for record '{declaration.Identifier.Text}'. Use the declarative projection."), ElasticCarriageReturnLineFeed);
    }

    sealed class RecordDeclarations
    {
        public Dictionary<string, RecordDeclarationSyntax> Declarations { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Conflicts { get; } = new(StringComparer.Ordinal);
    }
}
