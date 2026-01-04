// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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

        sb.AppendLine("// Copyright (c) Cratis. All rights reserved.");
        sb.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
        sb.AppendLine();
        sb.AppendLine("using Cratis.Chronicle.Keys;");
        sb.AppendLine("using Cratis.Chronicle.Projections.ModelBound;");
        sb.AppendLine();
        sb.AppendLine($"public record {readModelName}(");

        // Generate properties
        var properties = new List<string>();

        // Get all properties from schema
        if (schema.Properties?.Count > 0)
        {
            var isFirst = true;
            foreach (var prop in schema.Properties)
            {
                var propType = GetCSharpType(prop.Value);
                if (isFirst)
                {
                    properties.Add($"    [Key]");
                    properties.Add($"    {propType} {prop.Key}");
                    isFirst = false;
                }
                else
                {
                    properties.Add($"    {propType} {prop.Key}");
                }
            }
        }

        sb.AppendLine(string.Join($",{Environment.NewLine}", properties));
        sb.AppendLine(");");

        return sb.ToString();
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
}
