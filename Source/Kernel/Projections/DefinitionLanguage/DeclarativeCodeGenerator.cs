// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Generates declarative C# projection code from a <see cref="ProjectionDefinition"/>.
/// </summary>
public class DeclarativeCodeGenerator
{
    /// <summary>
    /// Generates declarative projection C# code.
    /// </summary>
    /// <param name="definition">The projection definition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated C# code for declarative projection.</returns>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var sb = new StringBuilder();
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title;
        var projectionName = definition.Identifier.Value;

        sb.AppendLine("using Cratis.Chronicle.Projections;")
          .AppendLine()
            .AppendLine($"public class {projectionName} : IProjectionFor<{readModelName}>")
            .AppendLine("{")
            .AppendLine($"    public void Define(IProjectionBuilderFor<{readModelName}> builder) => builder");

        var lines = new List<string>();

        // Generate From blocks
        GenerateFromBlocks(definition.From, definition.AutoMap, lines, 8);

        // Generate Join blocks
        GenerateJoinBlocks(definition.Join, definition.AutoMap, lines, 8);

        // Generate Children blocks
        GenerateChildrenBlocks(definition.Children, lines, 8);

        // Generate RemovedWith blocks
        GenerateRemovedWithBlocks(definition.RemovedWith, lines, 8);

        if (lines.Count > 0)
        {
            lines[^1] += ";";  // Add semicolon to last line
        }

        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    static string ConvertExpression(string expression)
    {
        // Handle event context properties - these return ToEventContextProperty syntax
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

    void GenerateFromBlocks(IDictionary<EventType, FromDefinition> fromBlocks, AutoMap autoMap, List<string> lines, int indent)
    {
        var indentStr = new string(' ', indent);

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
                lines.Add($"{indentStr}.From<{eventTypeName}>()");
                continue;
            }

            var propLines = new List<string>();

            // Add key configuration
            if (hasKey)
            {
                var keyLine = GenerateKeyExpression(fromDef.Key!, indentStr);
                propLines.Add(keyLine);
            }

            // Add parent key configuration
            if (hasParentKey)
            {
                var parentKeyLine = GenerateParentKeyExpression(fromDef.ParentKey!, indentStr);
                propLines.Add(parentKeyLine);
            }

            // Add property mappings
            foreach (var prop in fromDef.Properties)
            {
                var propertyPath = prop.Key.Path;
                var normalizedExpression = NormalizeExpression(prop.Value);

                if (normalizedExpression.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                {
                    var innerExpr = normalizedExpression[(WellKnownExpressions.Add.Length + 1)..^1];
                    propLines.Add($"{indentStr}    .Add(m => m.{propertyPath}).With({ConvertExpression(innerExpr)})");
                }
                else if (normalizedExpression.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                {
                    var innerExpr = normalizedExpression[(WellKnownExpressions.Subtract.Length + 1)..^1];
                    propLines.Add($"{indentStr}    .Subtract(m => m.{propertyPath}).With({ConvertExpression(innerExpr)})");
                }
                else if (normalizedExpression == WellKnownExpressions.Increment)
                {
                    propLines.Add($"{indentStr}    .Increment(m => m.{propertyPath})");
                }
                else if (normalizedExpression == WellKnownExpressions.Decrement)
                {
                    propLines.Add($"{indentStr}    .Decrement(m => m.{propertyPath})");
                }
                else if (normalizedExpression == WellKnownExpressions.Count)
                {
                    propLines.Add($"{indentStr}    .Count(m => m.{propertyPath})");
                }
                else
                {
                    propLines.Add($"{indentStr}    .Set(m => m.{propertyPath}).{ConvertExpressionForSet(normalizedExpression)}");
                }
            }

            if (propLines.Count > 0)
            {
                lines.Add($"{indentStr}.From<{eventTypeName}>(_ => _");
                lines.AddRange(propLines);
                lines[^1] += ")";
            }
            else
            {
                lines.Add($"{indentStr}.From<{eventTypeName}>(_ => _)");
            }
        }
    }

    string GenerateKeyExpression(string keyExpression, string indentStr)
    {
        if (keyExpression.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal))
        {
            // Parse: $composite(TypeName, prop1=expr1, prop2=expr2)
            var inner = keyExpression[(WellKnownExpressions.Composite.Length + 1)..^1];
            var parts = inner.Split([", "], StringSplitOptions.None);
            var typeName = parts[0];
            var propMappings = parts.Skip(1);

            var result = new List<string> { $"{indentStr}    .UsingCompositeKey<{typeName}>(_ => _" };
            foreach (var mapping in propMappings)
            {
                var keyValue = mapping.Split('=');
                var prop = keyValue[0];
                var expr = keyValue[1];
                result.Add($"{indentStr}        .Set(k => k.{prop}).To({ConvertExpression(expr)})");
            }
            result[^1] += ")";
            return string.Join(Environment.NewLine, result);
        }

        if (keyExpression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal))
        {
            var property = keyExpression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $"{indentStr}    .UsingKeyFromContext(c => c.{property})";
        }

        return $"{indentStr}    .UsingKey({ConvertExpression(keyExpression)})";
    }

    string GenerateParentKeyExpression(string keyExpression, string indentStr)
    {
        if (keyExpression.StartsWith($"{WellKnownExpressions.Composite}(", StringComparison.Ordinal))
        {
            var inner = keyExpression[(WellKnownExpressions.Composite.Length + 1)..^1];
            var parts = inner.Split([", "], StringSplitOptions.None);
            var typeName = parts[0];
            var propMappings = parts.Skip(1);

            var result = new List<string> { $"{indentStr}    .UsingParentCompositeKey<{typeName}>(_ => _" };
            foreach (var mapping in propMappings)
            {
                var keyValue = mapping.Split('=');
                var prop = keyValue[0];
                var expr = keyValue[1];
                result.Add($"{indentStr}        .Set(k => k.{prop}).To({ConvertExpression(expr)})");
            }
            result[^1] += ")";
            return string.Join(Environment.NewLine, result);
        }

        if (keyExpression.StartsWith($"{WellKnownExpressions.EventContext}(", StringComparison.Ordinal))
        {
            var property = keyExpression[(WellKnownExpressions.EventContext.Length + 1)..^1];
            return $"{indentStr}    .UsingParentKeyFromContext(c => c.{property})";
        }

        return $"{indentStr}    .UsingParentKey({ConvertExpression(keyExpression)})";
    }

    void GenerateJoinBlocks(IDictionary<EventType, JoinDefinition> joinBlocks, AutoMap autoMap, List<string> lines, int indent)
    {
        var indentStr = new string(' ', indent);

        foreach (var join in joinBlocks)
        {
            var eventTypeName = join.Key.Id.Value;
            var joinDef = join.Value;

            if (joinDef.Properties.Count == 0 && autoMap != AutoMap.Disabled)
            {
                lines.Add($"{indentStr}.Join<{eventTypeName}>(j => j.On(m => m.{joinDef.On}))");
            }
            else
            {
                var propLines = new List<string>();
                foreach (var prop in joinDef.Properties)
                {
                    var propertyPath = prop.Key;
                    var expression = prop.Value;
                    propLines.Add($"{indentStr}        .Set(m => m.{propertyPath}).To({ConvertExpression(expression)})");
                }

                if (propLines.Count > 0)
                {
                    lines.Add($"{indentStr}.Join<{eventTypeName}>(j => j");
                    lines.Add($"{indentStr}    .On(m => m.{joinDef.On})");
                    lines.AddRange(propLines);
                    lines[^1] += ")";
                }
                else
                {
                    lines.Add($"{indentStr}.Join<{eventTypeName}>(j => j.On(m => m.{joinDef.On}))");
                }
            }
        }
    }

    void GenerateChildrenBlocks(IDictionary<PropertyPath, ChildrenDefinition> childrenBlocks, List<string> lines, int indent)
    {
        var indentStr = new string(' ', indent);

        foreach (var child in childrenBlocks)
        {
            var propertyName = child.Key.Path;
            var childDef = child.Value;

            lines.Add($"{indentStr}.Children(m => m.{propertyName}, children => children");
            lines.Add($"{indentStr}    .IdentifiedBy(e => e.{childDef.IdentifiedBy})");

            // Generate child From blocks
            if (childDef.From.Count > 0)
            {
                GenerateFromBlocks(childDef.From, childDef.AutoMap, lines, indent + 4);
            }

            // Generate child Join blocks
            if (childDef.Join.Count > 0)
            {
                GenerateJoinBlocks(childDef.Join, childDef.AutoMap, lines, indent + 4);
            }

            // Generate nested children
            if (childDef.Children.Count > 0)
            {
                GenerateChildrenBlocks(childDef.Children, lines, indent + 4);
            }

            lines[^1] += ")";
        }
    }

    void GenerateRemovedWithBlocks(IDictionary<EventType, RemovedWithDefinition> removedWithBlocks, List<string> lines, int indent)
    {
        var indentStr = new string(' ', indent);

        foreach (var removed in removedWithBlocks)
        {
            var eventTypeName = removed.Key.Id.Value;
            var removedDef = removed.Value;
            lines.Add($"{indentStr}.RemovedWith<{eventTypeName}>(e => e.{removedDef.Key})");
        }
    }
}
