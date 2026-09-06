// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Elixir;

/// <summary>
/// Renders a projection as Elixir for the Elixir client.
/// </summary>
/// <remarks>
/// Both forms are the same <c>from</c> macros; what differs is where they live. The declarative form
/// puts them in a projection module that names the read model it targets, the model-bound form puts
/// them on the read model itself - as the client's own samples write them.
/// </remarks>
public class ElixirProjectionCodeGenerator : IProjectionCodeGenerator
{
    /// <inheritdoc/>
    public ProjectionCodeLanguage Language => ProjectionCodeLanguage.Elixir;

    /// <inheritdoc/>
    public bool Supports(ProjectionCodeStyle style) => true;

    /// <inheritdoc/>
    public string GenerateDeclarative(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title ?? string.Empty;
        var projectionName = ProjectionNaming.TypeNameFor(definition.Identifier.Value, readModelName);

        var builder = new StringBuilder();
        builder
            .AppendLine($"defmodule {projectionName} do")
            .AppendLine($"  use Chronicle.Projections.Projection, model: {readModelName}")
            .AppendLine();

        AppendAliases(definition, builder);
        AppendFromMacros(definition, builder);

        builder.AppendLine("end");
        return builder.ToString();
    }

    /// <inheritdoc/>
    public string GenerateModelBound(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        var readModelName = schema.Title ?? string.Empty;

        var builder = new StringBuilder();
        builder
            .AppendLine($"defmodule {readModelName} do")
            .AppendLine("  use Chronicle.ReadModels.ReadModel")
            .AppendLine();

        AppendAliases(definition, builder);

        var fields = schema.ActualProperties
            .Select(property => $"{ToSnakeCase(property.Key)}: {DefaultFor(property.Value)}")
            .ToList();

        if (fields.Count > 0)
        {
            builder.AppendLine($"  defstruct {fields[0]},");
            for (var index = 1; index < fields.Count; index++)
            {
                var suffix = index == fields.Count - 1 ? string.Empty : ",";
                builder.AppendLine($"            {fields[index]}{suffix}");
            }

            builder.AppendLine();
        }

        AppendFromMacros(definition, builder);

        builder.AppendLine("end");
        return builder.ToString();
    }

    static void AppendAliases(ProjectionDefinition definition, StringBuilder builder)
    {
        var eventTypes = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var eventType in definition.From.Keys) eventTypes.Add(LastSegment(eventType.Id.Value));
        foreach (var eventType in definition.Join.Keys) eventTypes.Add(LastSegment(eventType.Id.Value));
        foreach (var eventType in definition.RemovedWith.Keys) eventTypes.Add(LastSegment(eventType.Id.Value));

        if (eventTypes.Count == 0)
        {
            return;
        }

        builder
            .AppendLine($"  alias Events.{{{string.Join(", ", eventTypes)}}}")
            .AppendLine();
    }

    static void AppendFromMacros(ProjectionDefinition definition, StringBuilder builder)
    {
        foreach (var (eventType, from) in definition.From)
        {
            var eventTypeName = LastSegment(eventType.Id.Value);
            var options = new List<string>();

            if (from.Key is not null && !string.IsNullOrEmpty(from.Key.Value))
            {
                options.Add($"key: {KeywordValue(ProjectionExpressions.ReadValue(from.Key.Value))}");
            }

            var sets = new List<string>();
            var counts = new List<string>();
            foreach (var (property, expression) in from.Properties)
            {
                var mapping = ProjectionExpressions.ReadMapping(property, expression);
                var field = ToSnakeCase(mapping.Property);

                switch (mapping.Operation)
                {
                    case ProjectionOperation.Count:
                        counts.Add(field);
                        break;
                    case ProjectionOperation.Increment:
                        options.Add($"increment: :{field}");
                        break;
                    case ProjectionOperation.Decrement:
                        options.Add($"decrement: :{field}");
                        break;
                    case ProjectionOperation.Add:
                        options.Add($"add: [{field}: {KeywordValue(mapping.Source!)}]");
                        break;
                    case ProjectionOperation.Subtract:
                        options.Add($"subtract: [{field}: {KeywordValue(mapping.Source!)}]");
                        break;
                    case ProjectionOperation.Clear:
                        options.Add($"clear: :{field}");
                        break;
                    default:
                        sets.Add($"{field}: {KeywordValue(mapping.Source!)}");
                        break;
                }
            }

            if (sets.Count > 0)
            {
                options.Insert(0, $"set: [{string.Join(", ", sets)}]");
            }

            foreach (var field in counts)
            {
                options.Add($"count: :{field}");
            }

            builder.AppendLine(options.Count == 0
                ? $"  from {eventTypeName}"
                : $"  from {eventTypeName}, {string.Join(", ", options)}");
        }

        foreach (var (eventType, join) in definition.Join)
        {
            builder.AppendLine($"  join {LastSegment(eventType.Id.Value)}, on: :{ToSnakeCase(join.On)}");
        }

        foreach (var eventType in definition.RemovedWith.Keys)
        {
            builder.AppendLine($"  removed_with {LastSegment(eventType.Id.Value)}");
        }
    }

    static string KeywordValue(ProjectionValueSource source) =>
        source.Kind switch
        {
            ProjectionValueKind.EventSourceId => ":event_source_id",
            ProjectionValueKind.EventContextProperty => $"{{:context, :{ToSnakeCase(source.Value)}}}",
            ProjectionValueKind.Literal => source.Value,
            ProjectionValueKind.Text => $"\"{source.Value}\"",
            ProjectionValueKind.Nothing => "nil",
            _ => $":{ToSnakeCase(source.Value)}"
        };

    static string DefaultFor(JsonSchemaProperty property) =>
        property.Type switch
        {
            JsonObjectType.String => "\"\"",
            JsonObjectType.Integer or JsonObjectType.Number => "0",
            JsonObjectType.Boolean => "false",
            _ => "nil"
        };

    static string LastSegment(string value)
    {
        var index = value.LastIndexOf('.');
        return index < 0 ? value : value[(index + 1)..];
    }

    static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '.')
            {
                builder.Append('_');
                continue;
            }

            if (char.IsUpper(character) && index > 0 && value[index - 1] != '.')
            {
                builder.Append('_');
            }

            builder.Append(char.ToLower(character, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
