// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
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
            ? WithoutProjectionAttributes(record.Value.Declaration)
            : record.Value.Declaration);
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
        var registration = new RecordRegistration(CreateRecordDeclaration(scope), HasProjectionNeeds(scope.Definition));
        if (records.Declarations.TryAdd(scope.Name, registration)) return true;
        if (!registration.HasProjectionNeeds) return false;
        RegisterProjectedDeclaration(scope.Name, registration, records);
        return scope.Definition.Children.Count > 0 || scope.Definition.Nested?.Count > 0;
    }

    static void RegisterProjectedDeclaration(string name, RecordRegistration registration, RecordDeclarations records)
    {
        var existing = records.Declarations[name];
        if (!existing.HasProjectionNeeds)
        {
            records.Declarations[name] = registration;
        }
        else if (!registration.Declaration.IsEquivalentTo(existing.Declaration))
        {
            records.Conflicts.Add(name);
        }
    }

    static bool HasProjectionNeeds(ProjectionDefinition definition)
    {
        var counts = new[]
        {
            definition.From.Count,
            definition.Join.Count,
            definition.Children.Count,
            definition.Nested?.Count ?? 0,
            definition.FromEvery.Properties.Count,
            definition.RemovedWith.Count,
            definition.RemovedWithJoin.Count
        };
        return definition.SubscribesToAllEvents || counts.Any(count => count > 0);
    }

    static RecordDeclarationSyntax WithoutProjectionAttributes(RecordDeclarationSyntax declaration)
    {
        var parameters = declaration.ParameterList!.Parameters.Select(parameter => parameter.WithAttributeLists([]));
        return declaration.WithAttributeLists([])
            .WithParameterList(declaration.ParameterList.WithParameters(SeparatedList(parameters)))
            .WithLeadingTrivia(Comment($"// Not expressible with model-bound attributes: conflicting projection scopes for record '{declaration.Identifier.Text}'. Use the declarative projection."), ElasticCarriageReturnLineFeed);
    }

    sealed record RecordRegistration(RecordDeclarationSyntax Declaration, bool HasProjectionNeeds);

    sealed class RecordDeclarations
    {
        public Dictionary<string, RecordRegistration> Declarations { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Conflicts { get; } = new(StringComparer.Ordinal);
    }
}
