// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Generates declarative C# projection code from a <see cref="ProjectionDefinition"/> using Roslyn Syntax Factory.
/// </summary>
public class DeclarativeCodeGenerator
{
    /// <summary>
    /// Generates declarative projection C# code.
    /// </summary>
    /// <param name="definition">The projection definition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated <see cref="CompilationUnitSyntax"/> for declarative projection.</returns>
    public CompilationUnitSyntax Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title!;
        var projectionName = definition.Identifier.Value;

        var usingDirective = UsingDirective(ParseName("Cratis.Chronicle.Projections"));

        var classDeclaration = CreateProjectionClass(projectionName, readModelName, definition);

        return CompilationUnit()
            .WithUsings(SingletonList(usingDirective))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(classDeclaration))
            .NormalizeWhitespace();
    }

    static string ConvertExpression(string expression)
    {
        // Handle event context properties
        if (expression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            var property = expression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $"c => c.{property}";
        }

        // Handle literals
        if (expression == "True") return "true";
        if (expression == "False") return "false";
        if (expression.StartsWith('\"') && expression.EndsWith('\"')) return expression;
        if (double.TryParse(expression, out _)) return expression;

        // Event source ID
        if (expression == WellKnownExpressions.EventSourceId) return "e => e.EventSourceId";

        // Property path from event
        return $"e => e.{expression}";
    }

    static string ConvertExpressionForSet(string expression)
    {
        // For Set operations, event context properties need ToEventContextProperty
        if (expression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal) && expression.EndsWith(')'))
        {
            var property = expression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $"ToEventContextProperty(c => c.{property})";
        }

        return $"To({ConvertExpression(expression)})";
    }

    static string NormalizeExpression(string expression)
    {
        if (expression.StartsWith("+=", StringComparison.Ordinal))
        {
            return $"{WellKnownExpressions.Add}({expression[2..].Trim()})";
        }

        if (expression.StartsWith("-=", StringComparison.Ordinal))
        {
            return $"{WellKnownExpressions.Subtract}({expression[2..].Trim()})";
        }

        if (expression.Equals("increment", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Increment;
        }

        if (expression.Equals("decrement", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Decrement;
        }

        if (expression.Equals("count", StringComparison.Ordinal))
        {
            return WellKnownExpressions.Count;
        }

        return expression;
    }

    static string GenerateKeyExpression(string keyExpression)
    {
        if (keyExpression.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal))
        {
            // Parse: $composite(TypeName, prop1=expr1, prop2=expr2)
            var inner = keyExpression[(WellKnownExpressions.Composite.Length + 1)..^1];
            var parts = inner.Split([", "], StringSplitOptions.None);
            var typeName = parts[0];
            var propMappings = parts.Skip(1);

            var result = new List<string> { $".UsingCompositeKey<{typeName}>(_ => _" };
            foreach (var mapping in propMappings)
            {
                var keyValue = mapping.Split('=');
                var prop = keyValue[0];
                var expr = keyValue[1];
                result.Add($"    .Set(k => k.{prop}).To({ConvertExpression(expr)})");
            }
            result[^1] += ")";
            return string.Join("\n            ", result);
        }

        if (keyExpression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal))
        {
            var property = keyExpression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $".UsingKeyFromContext(c => c.{property})";
        }

        return $".UsingKey({ConvertExpression(keyExpression)})";
    }

    static string GenerateParentKeyExpression(string keyExpression)
    {
        if (keyExpression.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal))
        {
            var inner = keyExpression[(WellKnownExpressions.Composite.Length + 1)..^1];
            var parts = inner.Split([", "], StringSplitOptions.None);
            var typeName = parts[0];
            var propMappings = parts.Skip(1);

            var result = new List<string> { $".UsingParentCompositeKey<{typeName}>(_ => _" };
            foreach (var mapping in propMappings)
            {
                var keyValue = mapping.Split('=');
                var prop = keyValue[0];
                var expr = keyValue[1];
                result.Add($"    .Set(k => k.{prop}).To({ConvertExpression(expr)})");
            }
            result[^1] += ")";
            return string.Join("\n            ", result);
        }

        if (keyExpression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal))
        {
            var property = keyExpression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $".UsingParentKeyFromContext(c => c.{property})";
        }

        return $".UsingParentKey({ConvertExpression(keyExpression)})";
    }

    ClassDeclarationSyntax CreateProjectionClass(string projectionName, string readModelName, ProjectionDefinition definition)
    {
        var baseType = SimpleBaseType(
            GenericName("IProjectionFor")
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(IdentifierName(readModelName)))));

        var defineMethod = CreateDefineMethod(readModelName, definition);

        return ClassDeclaration(projectionName)
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(baseType)))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(defineMethod));
    }

    MethodDeclarationSyntax CreateDefineMethod(string readModelName, ProjectionDefinition definition)
    {
        // Build the fluent builder chain as a code string then parse it
        var builderCode = BuildFluentChain(definition);
        var builderExpression = ParseExpression($"builder{builderCode}");

        var arrowExpression = ArrowExpressionClause(builderExpression);

        return MethodDeclaration(
                PredefinedType(Token(SyntaxKind.VoidKeyword)),
                Identifier("Define"))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(
                    SingletonSeparatedList(
                        Parameter(Identifier("builder"))
                            .WithType(
                                GenericName("IProjectionBuilderFor")
                                    .WithTypeArgumentList(
                                        TypeArgumentList(
                                            SingletonSeparatedList<TypeSyntax>(
                                                IdentifierName(readModelName))))))))
            .WithExpressionBody(arrowExpression)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    string BuildFluentChain(ProjectionDefinition definition)
    {
        var lines = new List<string>();

        // Generate From blocks
        GenerateFromBlocks(definition.From, definition.AutoMap, lines);

        // Generate Join blocks
        GenerateJoinBlocks(definition.Join, definition.AutoMap, lines);

        // Generate Children blocks
        GenerateChildrenBlocks(definition.Children, lines);

        // Generate RemovedWith blocks
        GenerateRemovedWithBlocks(definition.RemovedWith, lines);

        if (lines.Count == 0)
        {
            return string.Empty;
        }

        return "\n        " + string.Join("\n        ", lines);
    }

    void GenerateFromBlocks(IDictionary<EventType, FromDefinition> fromBlocks, AutoMap autoMap, List<string> lines)
    {
        foreach (var from in fromBlocks)
        {
            var eventTypeName = from.Key.Id.Value;
            var fromDef = from.Value;
            var hasKey = fromDef.Key is not null && !string.IsNullOrEmpty(fromDef.Key.Value);
            var hasParentKey = fromDef.ParentKey is not null && !string.IsNullOrEmpty(fromDef.ParentKey);
            var hasProperties = fromDef.Properties.Count > 0;
            var needsLambda = hasKey || hasParentKey || hasProperties || autoMap == AutoMap.Disabled;

            if (!needsLambda)
            {
                lines.Add($".From<{eventTypeName}>()");
                continue;
            }

            var propLines = new List<string>();

            // Add key configuration
            if (hasKey)
            {
                propLines.Add(GenerateKeyExpression(fromDef.Key!));
            }

            // Add parent key configuration
            if (hasParentKey)
            {
                propLines.Add(GenerateParentKeyExpression(fromDef.ParentKey!));
            }

            // Add property mappings
            foreach (var prop in fromDef.Properties)
            {
                var propertyPath = prop.Key.Path;
                var normalizedExpression = NormalizeExpression(prop.Value);

                if (normalizedExpression.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                {
                    var innerExpr = normalizedExpression[(WellKnownExpressions.Add.Length + 1)..^1];
                    propLines.Add($".Add(m => m.{propertyPath}).With({ConvertExpression(innerExpr)})");
                }
                else if (normalizedExpression.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                {
                    var innerExpr = normalizedExpression[(WellKnownExpressions.Subtract.Length + 1)..^1];
                    propLines.Add($".Subtract(m => m.{propertyPath}).With({ConvertExpression(innerExpr)})");
                }
                else if (normalizedExpression == WellKnownExpressions.Increment)
                {
                    propLines.Add($".Increment(m => m.{propertyPath})");
                }
                else if (normalizedExpression == WellKnownExpressions.Decrement)
                {
                    propLines.Add($".Decrement(m => m.{propertyPath})");
                }
                else if (normalizedExpression == WellKnownExpressions.Count)
                {
                    propLines.Add($".Count(m => m.{propertyPath})");
                }
                else
                {
                    propLines.Add($".Set(m => m.{propertyPath}).{ConvertExpressionForSet(normalizedExpression)}");
                }
            }

            if (propLines.Count > 0)
            {
                lines.Add($".From<{eventTypeName}>(_ => _");
                lines.AddRange(propLines.Select(l => "    " + l));
                lines[^1] += ")";
            }
            else
            {
                lines.Add($".From<{eventTypeName}>(_ => _)");
            }
        }
    }

    void GenerateJoinBlocks(IDictionary<EventType, JoinDefinition> joinBlocks, AutoMap autoMap, List<string> lines)
    {
        foreach (var join in joinBlocks)
        {
            var eventTypeName = join.Key.Id.Value;
            var joinDef = join.Value;

            if (joinDef.Properties.Count == 0 && autoMap != AutoMap.Disabled)
            {
                lines.Add($".Join<{eventTypeName}>(j => j.On(m => m.{joinDef.On}))");
            }
            else
            {
                var propLines = new List<string>();
                foreach (var prop in joinDef.Properties)
                {
                    var propertyPath = prop.Key;
                    var expression = prop.Value;
                    propLines.Add($"    .Set(m => m.{propertyPath}).To({ConvertExpression(expression)})");
                }

                if (propLines.Count > 0)
                {
                    lines.Add($".Join<{eventTypeName}>(j => j");
                    lines.Add($"    .On(m => m.{joinDef.On})");
                    lines.AddRange(propLines);
                    lines[^1] += ")";
                }
                else
                {
                    lines.Add($".Join<{eventTypeName}>(j => j.On(m => m.{joinDef.On}))");
                }
            }
        }
    }

    void GenerateChildrenBlocks(IDictionary<PropertyPath, ChildrenDefinition> childrenBlocks, List<string> lines)
    {
        foreach (var child in childrenBlocks)
        {
            var propertyName = child.Key.Path;
            var childDef = child.Value;

            lines.Add($".Children(m => m.{propertyName}, children => children");
            lines.Add($"    .IdentifiedBy(e => e.{childDef.IdentifiedBy})");

            // Generate child From blocks
            if (childDef.From.Count > 0)
            {
                var childLines = new List<string>();
                GenerateFromBlocks(childDef.From, childDef.AutoMap, childLines);
                lines.AddRange(childLines.Select(l => "    " + l));
            }

            // Generate child Join blocks
            if (childDef.Join.Count > 0)
            {
                var childLines = new List<string>();
                GenerateJoinBlocks(childDef.Join, childDef.AutoMap, childLines);
                lines.AddRange(childLines.Select(l => "    " + l));
            }

            // Generate nested children
            if (childDef.Children.Count > 0)
            {
                var childLines = new List<string>();
                GenerateChildrenBlocks(childDef.Children, childLines);
                lines.AddRange(childLines.Select(l => "    " + l));
            }

            lines[^1] += ")";
        }
    }

    void GenerateRemovedWithBlocks(IDictionary<EventType, RemovedWithDefinition> removedWithBlocks, List<string> lines)
    {
        foreach (var removed in removedWithBlocks)
        {
            var eventTypeName = removed.Key.Id.Value;
            var removedDef = removed.Value;
            lines.Add($".RemovedWith<{eventTypeName}>(e => e.{removedDef.Key})");
        }
    }
}
