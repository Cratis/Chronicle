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

        sb.AppendLine("// Copyright (c) Cratis. All rights reserved.");
        sb.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
        sb.AppendLine();
        sb.AppendLine("using Cratis.Chronicle.Projections;");
        sb.AppendLine();
        sb.AppendLine($"public class {projectionName} : IProjectionFor<{readModelName}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public ProjectionId Identifier => \"{readModelName}\";");
        sb.AppendLine();
        sb.AppendLine($"    public void Define(IProjectionBuilderFor<{readModelName}> builder) => builder");

        // Generate from blocks
        var fromBlocks = new List<string>();
        foreach (var from in definition.From)
        {
            var eventTypeName = from.Key.Id.Value;
            fromBlocks.Add($"        .From<{eventTypeName}>(_ => _.AutoMap())");
        }

        sb.AppendLine(string.Join(Environment.NewLine, fromBlocks) + ";");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
