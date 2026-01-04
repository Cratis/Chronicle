// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

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
        var projectionName = $"{readModelName}Projection";

        sb.AppendLine("using Cratis.Chronicle.Projections;")
          .AppendLine();
        sb.AppendLine($"public class {projectionName} : IProjectionFor<{readModelName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public void Define(IProjectionBuilderFor<{readModelName}> builder) => builder");

        var lines = new List<string>();

        // Generate From blocks
        foreach (var from in definition.From)
        {
            var eventTypeName = from.Key.Id.Value;
            var fromDef = from.Value;

            if (fromDef.Properties.Count == 0 && fromDef.AutoMap != AutoMap.Disabled)
            {
                // Simple AutoMap case
                lines.Add($"        .From<{eventTypeName}>()");
            }
            else if (fromDef.Properties.Count == 0 && fromDef.AutoMap == AutoMap.Disabled)
            {
                // Empty From block
                lines.Add($"        .From<{eventTypeName}>(_ => _)");
            }
            else
            {
                // From with property mappings
                var propLines = new List<string>();
                foreach (var prop in fromDef.Properties)
                {
                    var propertyPath = prop.Key.Path;
                    var expression = prop.Value;

                    if (expression.StartsWith("$add(") && expression.EndsWith(')'))
                    {
                        var innerExpr = expression.Substring(5, expression.Length - 6);
                        propLines.Add($"            .Add(m => m.{propertyPath}).With(e => {ConvertExpression(innerExpr)})");
                    }
                    else if (expression.StartsWith("$subtract(") && expression.EndsWith(')'))
                    {
                        var innerExpr = expression.Substring(10, expression.Length - 11);
                        propLines.Add($"            .Subtract(m => m.{propertyPath}).With(e => {ConvertExpression(innerExpr)})");
                    }
                    else if (expression == "$increment")
                    {
                        propLines.Add($"            .Increment(m => m.{propertyPath})");
                    }
                    else if (expression == "$decrement")
                    {
                        propLines.Add($"            .Decrement(m => m.{propertyPath})");
                    }
                    else if (expression == "$count")
                    {
                        propLines.Add($"            .Count(m => m.{propertyPath})");
                    }
                    else
                    {
                        propLines.Add($"            .Set(m => m.{propertyPath}).To({ConvertExpression(expression)})");
                    }
                }

                if (propLines.Count > 0)
                {
                    lines.Add($"        .From<{eventTypeName}>(_ => _");
                    lines.AddRange(propLines);
                    lines[^1] += ")";  // Close the lambda
                }
                else
                {
                    lines.Add($"        .From<{eventTypeName}>()");
                }
            }
        }

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
        // Handle event context properties
        if (expression.StartsWith("$eventContext(") && expression.EndsWith(')'))
        {
            var property = expression.Substring(14, expression.Length - 15);
            return $"c => c.{property}";
        }

        // Handle literals
        if (expression == "True") return "true";
        if (expression == "False") return "false";
        if (expression.StartsWith('\"') && expression.EndsWith('\"')) return expression;
        if (double.TryParse(expression, out _)) return expression;

        // Event source ID
        if (expression == "$eventSourceId") return "e => e.EventSourceId";

        // Property path from event
        return $"e => e.{expression}";
    }
}
