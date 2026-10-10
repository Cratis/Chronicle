// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Generates declarative C# projection code from a <see cref="ProjectionDefinition"/> using Roslyn Syntax Factory.
/// </summary>
public class DeclarativeCodeGenerator
{
    const int FluentChainIndentation = 12;
    const string BuilderParameterName = "builder";

    /// <summary>
    /// Generates declarative projection C# code.
    /// </summary>
    /// <param name="definition">The projection definition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated <see cref="CompilationUnitSyntax"/> for declarative projection.</returns>
    public CompilationUnitSyntax Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        var readModelName = schema.Title!;
        var projectionName = ProjectionNaming.TypeNameFor(definition.Identifier.Value, readModelName);
        var lines = new List<string>();
        GenerateBlocks(definition, schema, lines, false);
        var chain = string.Join('\n', lines);
        var declaration = ParseMemberDeclaration($"public class {projectionName} : IProjectionFor<{readModelName}> {{ public void Define(IProjectionBuilderFor<{readModelName}> builder) => builder{chain}; }}")!;
        var compilationUnit = CompilationUnit()
            .WithUsings(SingletonList(UsingDirective(ParseName("Cratis.Chronicle.Projections"))))
            .WithMembers(SingletonList(declaration))
            .NormalizeWhitespace();

        return (CompilationUnitSyntax)new FluentChainFormatter(BuilderParameterName, FluentChainIndentation).Visit(compilationUnit);
    }

    static void GenerateBlocks(ProjectionDefinition definition, JsonSchema schema, List<string> lines, bool isChild)
    {
        if (definition.AutoMap == AutoMap.Disabled)
        {
            lines.Add(".NoAutoMap()");
        }
        GenerateFromBlocks(definition, schema, lines);
        DeclarativeRelationships.Joins(definition.Join, lines, isChild, DeclarativeExpressions.Set);
        DeclarativeRelationships.Children(definition.Children, lines, schema, false, GenerateBlocks);
        if (definition.Nested is not null)
        {
            DeclarativeRelationships.Children(definition.Nested, lines, schema, true, GenerateBlocks);
        }
        GenerateFromEvery(definition, lines);
        DeclarativeRelationships.Removals(definition, lines, schema, DeclarativeExpressions.Key, DeclarativeExpressions.ParentKey);
    }

    static void GenerateFromBlocks(ProjectionDefinition definition, JsonSchema schema, List<string> lines)
    {
        foreach (var from in definition.From)
        {
            var operations = new List<string>();
            if (!CSharpDefinitionSupport.IsDefaultKey(from.Value.Key))
            {
                operations.Add(DeclarativeExpressions.Key(from.Value.Key, schema));
            }
            if (!string.IsNullOrEmpty(from.Value.ParentKey?.Value))
            {
                operations.Add(DeclarativeExpressions.ParentKey(from.Value.ParentKey, schema));
            }
            operations.AddRange(from.Value.Properties.Select(property => DeclarativeExpressions.Operation(property.Key.Path, property.Value)));
            var callback = operations.Count == 0 ? string.Empty : $"_ => _{string.Concat(operations)}";
            lines.Add($".From<{from.Key.Id.Value}>({callback})");
        }
    }

    static void GenerateFromEvery(ProjectionDefinition definition, List<string> lines)
    {
        var every = definition.FromEvery;
        if (every.Properties.Count == 0 && !definition.SubscribesToAllEvents)
        {
            return;
        }

        var operations = every.Properties.Select(property => EveryOperation(property.Key.Path, property.Value)).ToList();
        if (!definition.SubscribesToAllEvents && !every.IncludeChildren)
        {
            operations.Add(".ExcludeChildProjections()");
        }
        var method = definition.SubscribesToAllEvents ? "FromAll" : "FromEvery";
        lines.Add($".{method}(_ => _{string.Concat(operations)})");
    }

    static string EveryOperation(string property, string expression)
    {
        var parts = property.Split([$".{WellKnownExpressions.EventContext}."], StringSplitOptions.None);
        if (parts.Length == 1)
        {
            return DeclarativeExpressions.Operation(property, expression);
        }

        var operation = DeclarativeExpressions.Normalize(expression) switch
        {
            WellKnownExpressions.Count => "Count",
            WellKnownExpressions.Increment => "Increment",
            WellKnownExpressions.Decrement => "Decrement",
            _ => "Set"
        };
        return $".{operation}(m => m.{parts[0]}, c => c.{CSharpDefinitionSupport.ContextPath(parts[1])})";
    }
}
