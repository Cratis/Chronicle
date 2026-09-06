// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Kotlin;

/// <summary>
/// Renders a projection as Kotlin for the JVM client.
/// </summary>
/// <remarks>
/// Only the declarative form. The JVM client has no model-bound projection API - its own
/// documentation says as much for every model-bound snippet - so asking for one says so rather
/// than emitting Kotlin that would compile against nothing.
/// </remarks>
public class KotlinProjectionCodeGenerator : IProjectionCodeGenerator
{
    /// <inheritdoc/>
    public ProjectionCodeLanguage Language => ProjectionCodeLanguage.Kotlin;

    /// <inheritdoc/>
    public bool Supports(ProjectionCodeStyle style) => style == ProjectionCodeStyle.Declarative;

    /// <inheritdoc/>
    public string GenerateDeclarative(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title ?? string.Empty;
        var projectionName = ProjectionNaming.TypeNameFor(definition.Identifier.Value, readModelName);

        var builder = new StringBuilder();
        builder
            .AppendLine("import io.cratis.chronicle.projections.IProjectionBuilderFor")
            .AppendLine("import io.cratis.chronicle.projections.IProjectionFor")
            .AppendLine("import io.cratis.chronicle.projections.Projection")
            .AppendLine()
            .AppendLine("@Projection")
            .AppendLine($"class {projectionName} : IProjectionFor<{readModelName}> {{")
            .AppendLine($"    override fun define(builder: IProjectionBuilderFor<{readModelName}>) {{")
            .Append("        builder");

        var blocks = new List<string>();
        AppendFromBlocks(definition.From, readModelName, blocks);
        AppendJoinBlocks(definition.Join, readModelName, blocks);
        AppendRemovedWithBlocks(definition.RemovedWith, blocks);

        if (blocks.Count == 0)
        {
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine();
            foreach (var block in blocks)
            {
                builder.AppendLine($"            {block}");
            }
        }

        builder.AppendLine("    }").AppendLine("}");
        return builder.ToString();
    }

    /// <inheritdoc/>
    public string GenerateModelBound(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        throw new ProjectionCodeGenerationNotSupported(ProjectionCodeLanguage.Kotlin, ProjectionCodeStyle.ModelBound);

    static void AppendFromBlocks(IDictionary<EventType, FromDefinition> fromBlocks, string readModelName, List<string> blocks)
    {
        foreach (var (eventType, from) in fromBlocks)
        {
            var eventTypeName = LastSegment(eventType.Id.Value);
            var inner = new List<string>();

            if (from.Key is not null && !string.IsNullOrEmpty(from.Key.Value))
            {
                inner.Add($"it.usingKey {Lambda(ProjectionExpressions.ReadValue(from.Key.Value))}");
            }

            foreach (var (property, expression) in from.Properties)
            {
                inner.Add(BuilderCall(ProjectionExpressions.ReadMapping(property, expression), readModelName));
            }

            if (inner.Count == 0)
            {
                blocks.Add($".from({eventTypeName}::class)");
                continue;
            }

            blocks.Add($".from({eventTypeName}::class) {{");
            blocks.AddRange(inner.Select(call => $"    {call}"));
            blocks.Add("}");
        }
    }

    static void AppendJoinBlocks(IDictionary<EventType, JoinDefinition> joinBlocks, string readModelName, List<string> blocks)
    {
        foreach (var (eventType, join) in joinBlocks)
        {
            var eventTypeName = LastSegment(eventType.Id.Value);
            blocks.Add($".join({eventTypeName}::class) {{");
            blocks.Add($"    it.on({readModelName}::{ToCamelCase(join.On)})");

            foreach (var (property, expression) in join.Properties)
            {
                blocks.Add($"    {BuilderCall(ProjectionExpressions.ReadMapping(property, expression), readModelName)}");
            }

            blocks.Add("}");
        }
    }

    static void AppendRemovedWithBlocks(IDictionary<EventType, RemovedWithDefinition> removedWithBlocks, List<string> blocks)
    {
        foreach (var eventType in removedWithBlocks.Keys)
        {
            blocks.Add($".removedWith({LastSegment(eventType.Id.Value)}::class)");
        }
    }

    static string BuilderCall(ProjectionPropertyMapping mapping, string readModelName)
    {
        var property = $"{readModelName}::{ToCamelCase(mapping.Property)}";

        return mapping.Operation switch
        {
            ProjectionOperation.Increment => $"it.increment({property})",
            ProjectionOperation.Decrement => $"it.decrement({property})",
            ProjectionOperation.Count => $"it.count({property})",
            ProjectionOperation.Clear => $"it.clear({property})",
            ProjectionOperation.Add => $"it.add({property}).with {Lambda(mapping.Source!)}",
            ProjectionOperation.Subtract => $"it.subtract({property}).with {Lambda(mapping.Source!)}",
            _ => $"it.set({property}).to {Lambda(mapping.Source!)}"
        };
    }

    static string Lambda(ProjectionValueSource source) =>
        source.Kind switch
        {
            ProjectionValueKind.EventSourceId => "{ e -> e.eventSourceId }",
            ProjectionValueKind.EventContextProperty => $"{{ c -> c.{ToCamelCase(source.Value)} }}",
            ProjectionValueKind.Literal => $"{{ {source.Value} }}",
            ProjectionValueKind.Text => $"{{ \"{source.Value}\" }}",
            ProjectionValueKind.Nothing => "{ null }",
            _ => $"{{ e -> e.{ToCamelCase(source.Value)} }}"
        };

    static string LastSegment(string value)
    {
        var index = value.LastIndexOf('.');
        return index < 0 ? value : value[(index + 1)..];
    }

    static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var segments = value.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            if (segment.Length > 0)
            {
                segments[index] = char.ToLowerInvariant(segment[0]) + segment[1..];
            }
        }

        return string.Join('.', segments);
    }
}
