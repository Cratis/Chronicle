// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Generates model-bound C# read model code from a <see cref="ProjectionDefinition"/>.
/// </summary>
public class ModelBoundCodeGenerator
{
    /// <summary>
    /// Generates model-bound read model C# code.
    /// </summary>
    /// <param name="definition">The projection definition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated C# code for model-bound read model.</returns>
    public string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var sb = new StringBuilder();
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title;
        var schema = readModelDefinition.GetSchemaForLatestGeneration();

        sb.AppendLine("using Cratis.Chronicle.Keys;")
          .AppendLine("using Cratis.Chronicle.Projections.ModelBound;")
          .AppendLine();

        // Add [FromEvent<>] attributes at class level for each event
        foreach (var from in definition.From)
        {
            var eventTypeName = from.Key.Id.Value;
            sb.AppendLine($"[FromEvent<{eventTypeName}>]");
        }

        sb.AppendLine($"public record {readModelName}(");

        var properties = new List<string>();

        if (schema.Properties?.Count > 0)
        {
            var propertyInfos = new Dictionary<string, PropertyInfo>();

            // Collect all property operations from all events
            foreach (var from in definition.From)
            {
                var eventTypeName = from.Key.Id.Value;
                var fromDef = from.Value;

                foreach (var prop in fromDef.Properties)
                {
                    var propertyName = prop.Key.Path;
                    var normalizedExpression = NormalizeExpression(prop.Value);

                    if (!propertyInfos.TryGetValue(propertyName, out var propInfo))
                    {
                        propInfo = new PropertyInfo { PropertyName = propertyName };
                        propertyInfos[propertyName] = propInfo;
                    }

                    if (normalizedExpression.StartsWith($"{WellKnownExpressions.Add}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                    {
                        var innerExpr = normalizedExpression[(WellKnownExpressions.Add.Length + 1)..^1];
                        propInfo.AddFroms.Add((eventTypeName, GetEventPropertyName(innerExpr)));
                    }
                    else if (normalizedExpression.StartsWith($"{WellKnownExpressions.Subtract}(", StringComparison.Ordinal) && normalizedExpression.EndsWith(')'))
                    {
                        var innerExpr = normalizedExpression[(WellKnownExpressions.Subtract.Length + 1)..^1];
                        propInfo.SubtractFroms.Add((eventTypeName, GetEventPropertyName(innerExpr)));
                    }
                    else
                    {
                        propInfo.SetFroms.Add((eventTypeName, GetEventPropertyName(normalizedExpression)));
                    }
                }
            }

            var isFirst = true;
            foreach (var schemaProp in schema.Properties)
            {
                var propName = schemaProp.Key;
                var propType = GetCSharpType(schemaProp.Value);
                var attributes = new List<string>();

                if (isFirst)
                {
                    attributes.Add("[Key]");
                    isFirst = false;
                }

                if (propertyInfos.TryGetValue(propName, out var propInfo))
                {
                    // Only add property-level attributes for Add/Subtract operations
                    // SetFrom is handled by [FromEvent<>] at class level with AutoMap
                    foreach (var addFrom in propInfo.AddFroms)
                    {
                        if (addFrom.EventPropertyName == propName)
                        {
                            attributes.Add($"[AddFrom<{addFrom.EventTypeName}>]");
                        }
                        else
                        {
                            attributes.Add($"[AddFrom<{addFrom.EventTypeName}>(nameof({addFrom.EventTypeName}.{addFrom.EventPropertyName}))]");
                        }
                    }

                    foreach (var subtractFrom in propInfo.SubtractFroms)
                    {
                        if (subtractFrom.EventPropertyName == propName)
                        {
                            attributes.Add($"[SubtractFrom<{subtractFrom.EventTypeName}>]");
                        }
                        else
                        {
                            attributes.Add($"[SubtractFrom<{subtractFrom.EventTypeName}>(nameof({subtractFrom.EventTypeName}.{subtractFrom.EventPropertyName}))]");
                        }
                    }
                }

                if (attributes.Count > 0)
                {
                    foreach (var attr in attributes)
                    {
                        properties.Add($"    {attr}");
                    }
                }

                properties.Add($"    {propType} {propName}");

                if (schemaProp.Key != schema.Properties.Last().Key)
                {
                    properties[^1] += ",";
                    properties.Add(string.Empty);
                }
            }
        }

        sb.AppendJoin(Environment.NewLine, properties)
          .AppendLine()
          .AppendLine(");");

        return sb.ToString();
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

        return expression;
    }

    static string GetCSharpType(JsonSchemaProperty propertySchema)
    {
        return propertySchema.Type switch
        {
            JsonObjectType.String => "string",
            JsonObjectType.Integer => "int",
            JsonObjectType.Number => "decimal",
            JsonObjectType.Boolean => "bool",
            _ => "object"
        };
    }

    static string GetEventPropertyName(string expression)
    {
        // Strip event source ID
        if (expression == WellKnownExpressions.EventSourceId) return "Id";

        // Event property path
        return expression;
    }

    sealed class PropertyInfo
    {
        public string PropertyName { get; set; } = string.Empty;
        public List<(string EventTypeName, string EventPropertyName)> SetFroms { get; } = [];
        public List<(string EventTypeName, string EventPropertyName)> AddFroms { get; } = [];
        public List<(string EventTypeName, string EventPropertyName)> SubtractFroms { get; } = [];
    }
}
