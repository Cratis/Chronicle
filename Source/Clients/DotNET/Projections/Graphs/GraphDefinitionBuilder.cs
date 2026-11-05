// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Turns a <see cref="GraphModel"/> into the definitions that are registered with the kernel.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for resolving event types.</param>
/// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for the schema of the node and edge types.</param>
internal class GraphDefinitionBuilder(INamingPolicy namingPolicy, IEventTypes eventTypes, IJsonSchemaGenerator schemaGenerator)
{
    readonly GraphSchemaWriter _schemaWriter = new(schemaGenerator);

    /// <summary>
    /// Build the registration of a graph.
    /// </summary>
    /// <param name="model">The <see cref="GraphModel"/>.</param>
    /// <param name="projectionId">The identifier of the projection that fills the graph.</param>
    /// <param name="sinkTypeId">The identifier of the sink type that stores the graph.</param>
    /// <returns>The <see cref="GraphRegistration"/>.</returns>
    public GraphRegistration Build(GraphModel model, string projectionId, string sinkTypeId)
    {
        var nodes = model.Nodes.ToDictionary(_ => _.Label, Node);
        var edges = model.Edges.ToDictionary(_ => _.EdgeType, Edge);
        return new(
            model.Name,
            new ReadModelDefinition
            {
                Type = new() { Identifier = model.Name, Generation = ReadModelGeneration.First },
                ContainerName = model.Name,
                DisplayName = model.Name,
                Sink = new() { ConfigurationId = Guid.Empty, TypeId = sinkTypeId },
                Schema = _schemaWriter.Write(model),
                Indexes = [],
                ObserverType = ReadModelObserverType.Projection,
                ObserverIdentifier = projectionId
            },
            new ProjectionDefinition
            {
                EventSequenceId = EventSequenceId.Log,
                Identifier = projectionId,
                ReadModel = model.Name,
                IsActive = true,
                IsRewindable = true,
                InitialModelState = "{}",
                All = new FromEveryDefinition(),
                Graph = new GraphProjectionDefinition { Nodes = nodes, Edges = edges }
            });
    }

    static bool IsComplex(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        var element = actual.IsArray ? actual.GetElementType()! : actual;
        return !(element.IsPrimitive || element.IsEnum || element == typeof(string) || element == typeof(decimal) || element == typeof(Guid) ||
            element == typeof(DateTime) || element == typeof(DateTimeOffset) || element == typeof(DateOnly) || element == typeof(TimeOnly) || element == typeof(TimeSpan) ||
            element.IsGenericType || IsConcept(element));
    }

    static bool IsConcept(Type type) => type.BaseType is { IsGenericType: true } baseType && baseType.GetGenericTypeDefinition().Name.StartsWith("ConceptAs", StringComparison.Ordinal);

    string EventTypeOf(Type eventType) => eventTypes.GetEventTypeFor(eventType).ToString();

    GraphNodeProjectionDefinition Node(GraphNodeModel node)
    {
        var properties = _schemaWriter.PropertiesOf(node.Type, []);
        var result = new GraphNodeProjectionDefinition();
        foreach (var from in node.From)
        {
            result.From[EventTypeOf(from.EventType)] = new GraphNodeFromDefinition
            {
                Key = from.Key ?? WellKnownExpressions.EventSourceId,
                Properties = Properties(from.EventType, properties.Keys.Where(_ => _ != node.KeyProperty), from.Properties),
                UpdateOnly = from.UpdateOnly
            };
        }

        foreach (var removal in node.RemovedWith)
        {
            result.RemovedWith[EventTypeOf(removal.EventType)] = new GraphNodeRemovedWithDefinition
            {
                Key = removal.Key ?? WellKnownExpressions.EventSourceId,
                Detach = removal.Detach
            };
        }

        return result;
    }

    GraphEdgeProjectionDefinition Edge(GraphEdgeModel edge)
    {
        var properties = _schemaWriter.PropertiesOf(edge.Type, edge.ExcludedProperties);
        var result = new GraphEdgeProjectionDefinition();
        foreach (var from in edge.From)
        {
            result.From[EventTypeOf(from.EventType)] = new GraphEdgeFromDefinition
            {
                SourceKey = from.SourceKey ?? WellKnownExpressions.EventSourceId,
                TargetKey = from.TargetKey ?? WellKnownExpressions.EventSourceId,
                Identity = Identity(edge, from.EventType, from.Identity),
                Properties = Properties(from.EventType, properties.Keys.Where(_ => _ != edge.IdentifyingProperty), from.Properties)
            };
        }

        foreach (var removal in edge.RemovedWith)
        {
            result.RemovedWith[EventTypeOf(removal.EventType)] = new GraphEdgeRemovedWithDefinition
            {
                SourceKey = removal.SourceKey ?? WellKnownExpressions.EventSourceId,
                TargetKey = removal.TargetKey ?? WellKnownExpressions.EventSourceId,
                Identity = Identity(edge, removal.EventType, removal.Identity)
            };
        }

        return result;
    }

    string? Identity(GraphEdgeModel edge, Type eventType, string? identity)
    {
        if (edge.IdentifyingProperty is null) return null;
        return identity ?? EventPropertyNamed(eventType, edge.IdentifyingProperty) ?? throw new UnknownGraphEventProperty(edge.Type, eventType, edge.IdentifyingProperty);
    }

    Dictionary<string, string> Properties(Type eventType, IEnumerable<string> candidates, IReadOnlyDictionary<string, string> explicitProperties)
    {
        var result = new Dictionary<string, string>();
        foreach (var name in candidates)
        {
            if (explicitProperties.TryGetValue(name, out var expression)) result[name] = expression;
            else if (EventPropertyNamed(eventType, name) is { } eventProperty) result[name] = eventProperty;
        }

        foreach (var (name, expression) in explicitProperties)
        {
            result.TryAdd(name, expression);
        }

        return result;
    }

    string? EventPropertyNamed(Type eventType, string graphPropertyName)
    {
        var property = eventType.GetProperties().FirstOrDefault(_ => namingPolicy.GetPropertyName(_.Name) == graphPropertyName);
        return property is null || IsComplex(property.PropertyType) ? null : (string)ProjectionPropertyPaths.Resolve(property.Name, namingPolicy, ProjectionPathRole.Source);
    }
}
