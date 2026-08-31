// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.TypeScript;

/// <summary>
/// Renders a projection as TypeScript for the TypeScript client.
/// </summary>
/// <remarks>
/// The declarative form mirrors the client's <c>IProjectionFor</c> builder, and the model-bound form
/// its decorators - both as the client's own samples write them.
/// </remarks>
public class TypeScriptProjectionCodeGenerator : IProjectionCodeGenerator
{
    /// <inheritdoc/>
    public ProjectionCodeLanguage Language => ProjectionCodeLanguage.TypeScript;

    /// <inheritdoc/>
    public bool Supports(ProjectionCodeStyle style) => true;

    /// <inheritdoc/>
    public string GenerateDeclarative(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var readModelName = readModelDefinition.GetSchemaForLatestGeneration().Title ?? string.Empty;
        var projectionName = ProjectionNaming.TypeNameFor(definition.Identifier.Value, readModelName);
        var eventTypes = CollectEventTypes(definition);

        var builder = new StringBuilder();
        builder.AppendLine("import { IProjectionBuilderFor, IProjectionFor, projection } from '@cratis/chronicle';");

        if (eventTypes.Count > 0)
        {
            builder.AppendLine($"import {{ {string.Join(", ", eventTypes)} }} from './events';");
        }

        builder
            .AppendLine()
            .AppendLine($"@projection('', {readModelName})")
            .AppendLine($"export class {projectionName} implements IProjectionFor<{readModelName}> {{")
            .AppendLine($"    define(builder: IProjectionBuilderFor<{readModelName}>): void {{")
            .Append("        builder");

        var blocks = new List<string>();
        AppendFromBlocks(definition.From, blocks);
        AppendJoinBlocks(definition.Join, blocks);
        AppendRemovedWithBlocks(definition.RemovedWith, blocks);

        if (blocks.Count == 0)
        {
            builder.AppendLine(";");
        }
        else
        {
            builder.AppendLine();
            for (var index = 0; index < blocks.Count; index++)
            {
                var suffix = index == blocks.Count - 1 ? ";" : string.Empty;
                builder.AppendLine($"            {blocks[index]}{suffix}");
            }
        }

        builder.AppendLine("    }").AppendLine("}");
        return builder.ToString();
    }

    /// <inheritdoc/>
    public string GenerateModelBound(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        var readModelName = schema.Title ?? string.Empty;
        var eventTypes = CollectEventTypes(definition);

        var decorators = new List<string>();
        var setFroms = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (eventType, from) in definition.From)
        {
            var eventTypeName = LastSegment(eventType.Id.Value);
            decorators.Add($"@fromEvent({eventTypeName})");

            foreach (var (property, expression) in from.Properties)
            {
                var mapping = ProjectionExpressions.ReadMapping(property, expression);
                var decorator = ModelBoundDecorator(mapping, eventTypeName);
                if (decorator is null) continue;

                if (!setFroms.TryGetValue(mapping.Property, out var forProperty))
                {
                    forProperty = [];
                    setFroms[mapping.Property] = forProperty;
                }

                forProperty.Add(decorator);
            }
        }

        var builder = new StringBuilder();
        builder.AppendLine($"import {{ {ModelBoundImports(setFroms)} }} from '@cratis/chronicle';");

        if (eventTypes.Count > 0)
        {
            builder.AppendLine($"import {{ {string.Join(", ", eventTypes)} }} from './events';");
        }

        builder.AppendLine().AppendLine("@readModel()");
        foreach (var decorator in decorators)
        {
            builder.AppendLine(decorator);
        }

        builder.AppendLine($"export class {readModelName} {{");

        foreach (var (name, property) in schema.ActualProperties)
        {
            if (setFroms.TryGetValue(name, out var propertyDecorators))
            {
                foreach (var decorator in propertyDecorators)
                {
                    builder.AppendLine($"    {decorator}");
                }
            }

            builder.AppendLine($"    {ToCamelCase(name)}: {TypeFor(property)} = {DefaultFor(property)};");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    static string ModelBoundImports(Dictionary<string, List<string>> setFroms)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal) { "fromEvent", "readModel" };
        foreach (var decorator in setFroms.Values.SelectMany(_ => _))
        {
            names.Add(decorator[1..decorator.IndexOf('(', StringComparison.Ordinal)]);
        }

        return string.Join(", ", names);
    }

    static string? ModelBoundDecorator(ProjectionPropertyMapping mapping, string eventTypeName) =>
        (mapping.Operation, mapping.Source?.Kind) switch
        {
            (ProjectionOperation.Count, _) => $"@count({eventTypeName})",
            (ProjectionOperation.Increment, _) => $"@increment({eventTypeName})",
            (ProjectionOperation.Decrement, _) => $"@decrement({eventTypeName})",
            (ProjectionOperation.Add, ProjectionValueKind.EventProperty) =>
                $"@addFrom({eventTypeName}, '{ToCamelCase(mapping.Source.Value)}')",
            (ProjectionOperation.Subtract, ProjectionValueKind.EventProperty) =>
                $"@subtractFrom({eventTypeName}, '{ToCamelCase(mapping.Source.Value)}')",
            (ProjectionOperation.Set, ProjectionValueKind.EventContextProperty) =>
                $"@setFromContext('{ToCamelCase(mapping.Source.Value)}')",
            (ProjectionOperation.Set, ProjectionValueKind.Literal) => $"@setValue({mapping.Source.Value})",
            (ProjectionOperation.Set, ProjectionValueKind.Text) => $"@setValue('{mapping.Source.Value}')",
            (ProjectionOperation.Set, ProjectionValueKind.EventProperty) =>
                $"@setFrom({eventTypeName}, '{ToCamelCase(mapping.Source.Value)}')",
            _ => null
        };

    static void AppendFromBlocks(IDictionary<Concepts.Events.EventType, FromDefinition> fromBlocks, List<string> blocks)
    {
        foreach (var (eventType, from) in fromBlocks)
        {
            var eventTypeName = LastSegment(eventType.Id.Value);
            var inner = new List<string>();

            if (from.Key is not null && !string.IsNullOrEmpty(from.Key.Value))
            {
                inner.Add($".usingKey({Lambda(ProjectionExpressions.ReadValue(from.Key.Value))})");
            }

            foreach (var (property, expression) in from.Properties)
            {
                var mapping = ProjectionExpressions.ReadMapping(property, expression);
                inner.Add(BuilderCall(mapping));
            }

            blocks.Add(inner.Count == 0
                ? $".from({eventTypeName})"
                : $".from({eventTypeName}, fb => fb{string.Concat(inner)})");
        }
    }

    static void AppendJoinBlocks(IDictionary<Concepts.Events.EventType, JoinDefinition> joinBlocks, List<string> blocks)
    {
        foreach (var (eventType, join) in joinBlocks)
        {
            var eventTypeName = LastSegment(eventType.Id.Value);
            var inner = new List<string> { $".on(m => m.{ToCamelCase(join.On)})" };

            foreach (var (property, expression) in join.Properties)
            {
                inner.Add(BuilderCall(ProjectionExpressions.ReadMapping(property, expression)));
            }

            blocks.Add($".join({eventTypeName}, j => j{string.Concat(inner)})");
        }
    }

    static void AppendRemovedWithBlocks(IDictionary<Concepts.Events.EventType, RemovedWithDefinition> removedWithBlocks, List<string> blocks)
    {
        foreach (var (eventType, _) in removedWithBlocks)
        {
            blocks.Add($".removedWith({LastSegment(eventType.Id.Value)})");
        }
    }

    static string BuilderCall(ProjectionPropertyMapping mapping)
    {
        var property = $"m => m.{ToCamelCase(mapping.Property)}";

        return mapping.Operation switch
        {
            ProjectionOperation.Increment => $".increment({property})",
            ProjectionOperation.Decrement => $".decrement({property})",
            ProjectionOperation.Count => $".count({property})",
            ProjectionOperation.Clear => $".clear({property})",
            ProjectionOperation.Add => $".add({property}).with({Lambda(mapping.Source!)})",
            ProjectionOperation.Subtract => $".subtract({property}).with({Lambda(mapping.Source!)})",
            _ => $".set({property}).to({Lambda(mapping.Source!)})"
        };
    }

    static string Lambda(ProjectionValueSource source) =>
        source.Kind switch
        {
            ProjectionValueKind.EventSourceId => "e => e.eventSourceId",
            ProjectionValueKind.EventContextProperty => $"c => c.{ToCamelCase(source.Value)}",
            ProjectionValueKind.Literal => $"() => {source.Value}",
            ProjectionValueKind.Text => $"() => '{source.Value}'",
            ProjectionValueKind.Nothing => "() => null",
            _ => $"e => e.{ToCamelCase(source.Value)}"
        };

    static List<string> CollectEventTypes(ProjectionDefinition definition)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var eventType in definition.From.Keys) names.Add(LastSegment(eventType.Id.Value));
        foreach (var eventType in definition.Join.Keys) names.Add(LastSegment(eventType.Id.Value));
        foreach (var eventType in definition.RemovedWith.Keys) names.Add(LastSegment(eventType.Id.Value));
        return [.. names];
    }

    static string TypeFor(JsonSchemaProperty property) =>
        property.Type switch
        {
            JsonObjectType.String => "string",
            JsonObjectType.Integer or JsonObjectType.Number => "number",
            JsonObjectType.Boolean => "boolean",
            _ => "unknown"
        };

    static string DefaultFor(JsonSchemaProperty property) =>
        property.Type switch
        {
            JsonObjectType.String => "''",
            JsonObjectType.Integer or JsonObjectType.Number => "0",
            JsonObjectType.Boolean => "false",
            _ => "undefined"
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
