// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphProjectionPlanFactory"/> that reuses the expression resolvers of the projection engine.
/// </summary>
/// <param name="propertyExpressionResolvers"><see cref="IReadModelPropertyExpressionResolvers"/> for resolving property expressions, including counters.</param>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving keys from events.</param>
[Singleton]
public class GraphProjectionPlanFactory(
    IReadModelPropertyExpressionResolvers propertyExpressionResolvers,
    IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers) : IGraphProjectionPlanFactory
{
    /// <inheritdoc/>
    public GraphProjectionPlan Create(ProjectionId projection, GraphProjectionDefinition definition, GraphDefinition graph)
    {
        var byEventType = new Dictionary<EventType, GraphEventMappings>();
        GraphEventMappings For(string eventType)
        {
            var type = EventType.Parse(eventType);
            if (!byEventType.TryGetValue(type, out var mappings)) byEventType[type] = mappings = new();
            return mappings;
        }

        foreach (var (label, node) in definition.Nodes)
        {
            var nodeDefinition = Find(projection, () => graph.GetNode(label));
            foreach (var (eventType, from) in node.From) For(eventType).Nodes.Add(NodeMapping(projection, nodeDefinition, from));
            foreach (var (eventType, removal) in node.RemovedWith) For(eventType).NodeRemovals.Add(new(label, Key(nodeDefinition.Schema, nodeDefinition.KeyProperty, removal.Key), removal.Detach, ForEach(removal.ForEach)));
        }

        foreach (var (type, edge) in definition.Edges)
        {
            var edgeDefinition = Find(projection, () => graph.GetEdge(type));
            var source = Find(projection, () => graph.GetNode(edgeDefinition.SourceLabel));
            var target = Find(projection, () => graph.GetNode(edgeDefinition.TargetLabel));
            foreach (var (eventType, from) in edge.From) For(eventType).Edges.Add(EdgeMapping(projection, edgeDefinition, source, target, from));
            foreach (var (eventType, removal) in edge.RemovedWith) For(eventType).EdgeRemovals.Add(EdgeRemoval(projection, edgeDefinition, source, target, removal));
        }

        return new(graph, byEventType);
    }

    static bool ReadsCurrentState(IDictionary<string, string> properties) => properties.Values.Any(expression =>
        expression.StartsWith(WellKnownExpressions.Add, StringComparison.Ordinal) ||
        expression.StartsWith(WellKnownExpressions.Subtract, StringComparison.Ordinal) ||
        expression == WellKnownExpressions.Count ||
        expression == WellKnownExpressions.Increment ||
        expression == WellKnownExpressions.Decrement);

    static string? ForEach(PropertyExpression? expression) => expression?.IsSet() == true ? expression.Value : null;

    static T Find<T>(ProjectionId projection, Func<T> find)
    {
        try
        {
            return find();
        }
        catch (UnknownGraphElement ex)
        {
            throw new InvalidGraphProjection(projection.Value, ex.Message);
        }
    }

    GraphNodeMapping NodeMapping(ProjectionId projection, GraphNodeDefinition node, GraphNodeFromDefinition from)
    {
        var (names, mappers) = Properties(projection, node.Label, node.Schema, node.KeyProperty, from.Properties);
        return new(node.Label, node.KeyProperty, Key(node.Schema, node.KeyProperty, from.Key), names, mappers, from.UpdateOnly, ReadsCurrentState(from.Properties), ForEach(from.ForEach));
    }

    GraphEdgeMapping EdgeMapping(ProjectionId projection, GraphEdgeDefinition edge, GraphNodeDefinition source, GraphNodeDefinition target, GraphEdgeFromDefinition from)
    {
        var (names, mappers) = Properties(projection, edge.Type, edge.Schema, edge.IdentifyingProperty, from.Properties);
        return new(
            edge.Type,
            edge.SourceLabel,
            edge.TargetLabel,
            Key(source.Schema, source.KeyProperty, from.SourceKey),
            Key(target.Schema, target.KeyProperty, from.TargetKey),
            edge.IdentifyingProperty,
            Identity(projection, edge, from.Identity),
            names,
            mappers,
            ReadsCurrentState(from.Properties),
            ForEach(from.ForEach));
    }

    GraphEdgeRemoval EdgeRemoval(ProjectionId projection, GraphEdgeDefinition edge, GraphNodeDefinition source, GraphNodeDefinition target, GraphEdgeRemovedWithDefinition removal) =>
        new(
            edge.Type,
            edge.SourceLabel,
            edge.TargetLabel,
            Key(source.Schema, source.KeyProperty, removal.SourceKey),
            Key(target.Schema, target.KeyProperty, removal.TargetKey),
            Identity(projection, edge, removal.Identity),
            ForEach(removal.ForEach));

    ValueProvider<AppendedEvent>? Identity(ProjectionId projection, GraphEdgeDefinition edge, PropertyExpression? expression)
    {
        if (edge.IdentifyingProperty is null) return null;
        if (expression?.IsSet() != true)
        {
            throw new InvalidGraphProjection(projection.Value, $"the edge '{edge.Type}' is identified by '{edge.IdentifyingProperty}' and every mapping must provide it.");
        }

        return Key(edge.Schema, edge.IdentifyingProperty, expression);
    }

    ValueProvider<AppendedEvent> Key(JsonSchema schema, string property, PropertyExpression expression)
    {
        var propertySchema = schema.Properties.TryGetValue(property, out var found) ? found : new JsonSchemaProperty { Type = JsonObjectType.String };
        return eventValueProviderExpressionResolvers.Resolve(propertySchema, expression.IsSet() ? expression.Value : WellKnownExpressions.EventSourceId);
    }

    (IReadOnlyList<string> Names, IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> Mappers) Properties(
        ProjectionId projection,
        string element,
        JsonSchema schema,
        string? reservedProperty,
        IDictionary<string, string> properties)
    {
        var names = new List<string>();
        var mappers = new List<PropertyMapper<AppendedEvent, ExpandoObject>>();
        foreach (var (name, expression) in properties)
        {
            if (!schema.Properties.TryGetValue(name, out var propertySchema))
            {
                throw new InvalidGraphProjection(projection.Value, $"'{name}' is not a property of '{element}'.");
            }

            if (name == reservedProperty)
            {
                throw new InvalidGraphProjection(projection.Value, $"'{name}' identifies '{element}' and cannot be mapped as a property.");
            }

            if (propertySchema.ActualSchema.Type.HasFlag(JsonObjectType.Object))
            {
                throw new InvalidGraphProjection(projection.Value, $"'{element}.{name}' is an object. A graph stores primitives and arrays of primitives only.");
            }

            names.Add(name);
            mappers.Add(propertyExpressionResolvers.Resolve(new PropertyPath(name), propertySchema, expression));
        }

        return (names, mappers);
    }
}
