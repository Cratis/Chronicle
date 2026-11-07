// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

public partial class Generator
{
    static string EventTypeName(string eventType) => Concepts.Events.EventType.Parse(eventType).Id.Value;

    static void GeneratePropertyDeclarations(StringBuilder sb, JsonSchema element, int indent, string? key)
    {
        foreach (var (name, property) in element.Properties)
        {
            var (type, collection, optional) = DescribeGraphProperty(property);
            var suffix = (collection ? "[]" : string.Empty) + Modifier(name == key, optional);
            sb.AppendLine($"{Indent(indent)}{EscapeIfKeyword(name)} {type}{suffix}");
        }
    }

    static (string Type, bool Collection, bool Optional) DescribeGraphProperty(JsonSchemaProperty property)
    {
        var schema = property.ActualSchema;
        var optional = schema.Type.HasFlag(JsonObjectType.Null) || property.Type.HasFlag(JsonObjectType.Null);
        var collection = schema.Type.HasFlag(JsonObjectType.Array);
        var scalar = collection && property.Item is { } item ? item.ActualSchema : schema;
        var type = scalar.Format switch
        {
            "guid" or "uuid" => "Uuid",
            "date-time" => "DateTime",
            "date" => "Date",
            _ when scalar.Type.HasFlag(JsonObjectType.Integer) => "Int",
            _ when scalar.Type.HasFlag(JsonObjectType.Number) => "Decimal",
            _ when scalar.Type.HasFlag(JsonObjectType.Boolean) => "Bool",
            _ => "String"
        };

        return (type, collection, optional);
    }

    static string Modifier(bool isKey, bool optional)
    {
        if (isKey)
        {
            return " key";
        }

        return optional ? " optional" : string.Empty;
    }

    /// <summary>
    /// Generates the declaration of a graph projection: the graph the read model schema describes followed by the projection that builds it.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/> with a graph.</param>
    /// <param name="readModelDefinition">The <see cref="ReadModelDefinition"/> holding the graph schema.</param>
    /// <returns>The declaration.</returns>
    string GenerateGraph(ProjectionDefinition definition, ReadModelDefinition readModelDefinition)
    {
        var sb = new StringBuilder();
        var graph = GraphDefinition.FromSchema(readModelDefinition.GetSchemaForLatestGeneration());
        var projection = definition.Graph!;

        sb.AppendLine($"graph {graph.Name}");
        foreach (var node in graph.Nodes)
        {
            sb.AppendLine($"{Indent(1)}node {node.Label}");
            GeneratePropertyDeclarations(sb, node.Schema, 2, node.KeyProperty);
        }

        foreach (var edge in graph.Edges)
        {
            var identity = edge.IdentifyingProperty is null ? string.Empty : $" identified by {edge.IdentifyingProperty}";
            sb.AppendLine($"{Indent(1)}edge {edge.Type} source {edge.SourceLabel} target {edge.TargetLabel}{identity}");
            GeneratePropertyDeclarations(sb, edge.Schema, 2, null);
        }

        sb.AppendLine().AppendLine($"projection {definition.Identifier.Value} => graph {graph.Name}");
        foreach (var node in graph.Nodes.Where(_ => projection.Nodes.TryGetValue(_.Label, out var block) && (block.From.Count > 0 || block.RemovedWith.Count > 0)))
        {
            var block = projection.Nodes[node.Label];
            sb.AppendLine($"{Indent(1)}node {node.Label}");
            foreach (var (eventType, from) in block.From)
            {
                sb.AppendLine($"{Indent(2)}from {EventTypeName(eventType)} key {ConvertExpressionForOutput(from.Key.Value)}");
                GenerateGraphMappings(sb, from.Properties, node.KeyProperty);
            }

            foreach (var (eventType, removal) in block.RemovedWith)
            {
                var detach = removal.Detach ? " detach" : string.Empty;
                sb.AppendLine($"{Indent(2)}remove with {EventTypeName(eventType)} key {ConvertExpressionForOutput(removal.Key.Value)}{detach}");
            }
        }

        foreach (var edge in graph.Edges.Where(_ => projection.Edges.TryGetValue(_.Type, out var block) && (block.From.Count > 0 || block.RemovedWith.Count > 0)))
        {
            var block = projection.Edges[edge.Type];
            sb.AppendLine($"{Indent(1)}edge {edge.Type}");
            foreach (var (eventType, from) in block.From)
            {
                sb.AppendLine($"{Indent(2)}from {EventTypeName(eventType)} source {ConvertExpressionForOutput(from.SourceKey.Value)} target {ConvertExpressionForOutput(from.TargetKey.Value)}{IdentityOutput(from.Identity)}");
                GenerateGraphMappings(sb, from.Properties, edge.IdentifyingProperty);
            }

            foreach (var (eventType, removal) in block.RemovedWith)
            {
                sb.AppendLine($"{Indent(2)}remove with {EventTypeName(eventType)} source {ConvertExpressionForOutput(removal.SourceKey.Value)} target {ConvertExpressionForOutput(removal.TargetKey.Value)}{IdentityOutput(removal.Identity)}");
            }
        }

        return sb.ToString();
    }

    string IdentityOutput(PropertyExpression? identity) =>
        identity is null ? string.Empty : $" identified by {ConvertExpressionForOutput(identity.Value)}";

    void GenerateGraphMappings(StringBuilder sb, IDictionary<string, string> properties, string? skip)
    {
        foreach (var (name, expression) in properties)
        {
            if (name == skip)
            {
                continue;
            }

            GeneratePropertyMapping(sb, new PropertyPath(name), expression, 3);
        }
    }
}
