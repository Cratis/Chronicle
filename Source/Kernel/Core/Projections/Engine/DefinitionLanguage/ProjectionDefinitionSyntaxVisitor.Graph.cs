// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

public partial class ProjectionDefinitionSyntaxVisitor
{
    /// <summary>
    /// Lowers a graph projection into the projection definition the graph projection engine runs.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphProjectionSyntax"/> to lower.</param>
    /// <param name="graph">The <see cref="GraphSyntax"/> the projection builds.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    /// <remarks>
    /// A node or edge the graph declares gets an entry even when the projection has no block for it, the way the
    /// .NET client registers every element of a graph. Properties an event shares by name with a node or edge are
    /// mapped automatically when the event type schemas are known, as they are for a model-bound graph.
    /// An 'every' block has no counterpart in the definition, so its mappings are applied to each
    /// 'from' of the same block.
    /// </remarks>
    public ProjectionDefinition Visit(GraphProjectionSyntax syntax, GraphSyntax graph)
    {
        var nodes = graph.Nodes.ToDictionary(node => node.Label, node => LowerNode(node, syntax.Blocks.OfType<GraphNodeBlockSyntax>().Where(_ => _.Label == node.Label)));
        var edges = graph.Edges.ToDictionary(edge => edge.Type, edge => LowerEdge(edge, syntax.Blocks.OfType<GraphEdgeBlockSyntax>().Where(_ => _.Type == edge.Type)));

        return new ProjectionDefinition(
            owner,
            EventSequenceId.Log,
            new ProjectionId(syntax.Name),
            new ReadModelIdentifier(graph.Name),
            IsActive: true,
            IsRewindable: true,
            new JsonObject(),
            new Dictionary<Concepts.Events.EventType, FromDefinition>(),
            new Dictionary<Concepts.Events.EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), IncludeChildren: false),
            new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(),
            new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>(),
            FromEventProperty: null,
            LastUpdated: DateTimeOffset.UtcNow,
            Tags: default,
            AutoMap: AutoMap.Enabled,
            Nested: null,
            SubscribesToAllEvents: false)
        {
            GenerationDelivery = generationDelivery,
            Graph = new GraphProjectionDefinition(nodes, edges)
        };
    }

    static IEnumerable<MappingSyntax> EveryMappings(IEnumerable<ProjectionBlockSyntax> blocks) =>
        blocks.OfType<EverySyntax>().SelectMany(_ => _.Mappings);

    GraphNodeProjectionDefinition LowerNode(GraphNodeSyntax declaration, IEnumerable<GraphNodeBlockSyntax> blocks)
    {
        var key = declaration.Properties.First(_ => _.IsKey).Name;
        var from = new Dictionary<string, GraphNodeFromDefinition>();
        var removedWith = new Dictionary<string, GraphNodeRemovedWithDefinition>();
        foreach (var block in blocks)
        {
            var every = EveryMappings(block.Blocks);
            foreach (var handler in block.Blocks.OfType<GraphFromSyntax>())
            {
                var eventType = ResolveEventType(handler.Event);
                var properties = GraphProperties(eventType, declaration.Properties.Where(_ => _.Name != key), every, handler.Mappings);
                from[eventType.ToString()] = new GraphNodeFromDefinition(ConvertKeyExpression(handler.Key!), properties);
            }

            foreach (var removal in block.Blocks.OfType<GraphRemoveSyntax>())
            {
                removedWith[ResolveEventType(removal.Event).ToString()] = new GraphNodeRemovedWithDefinition(ConvertKeyExpression(removal.Key!), removal.Detach);
            }
        }

        return new GraphNodeProjectionDefinition(from, removedWith);
    }

    GraphEdgeProjectionDefinition LowerEdge(GraphEdgeSyntax declaration, IEnumerable<GraphEdgeBlockSyntax> blocks)
    {
        var from = new Dictionary<string, GraphEdgeFromDefinition>();
        var removedWith = new Dictionary<string, GraphEdgeRemovedWithDefinition>();
        foreach (var block in blocks)
        {
            var every = EveryMappings(block.Blocks);
            foreach (var handler in block.Blocks.OfType<GraphFromSyntax>())
            {
                var eventType = ResolveEventType(handler.Event);
                var properties = GraphProperties(eventType, declaration.Properties.Where(_ => _.Name != declaration.IdentifiedBy), every, handler.Mappings);
                from[eventType.ToString()] = new GraphEdgeFromDefinition(
                    ConvertKeyExpression(handler.Source!),
                    ConvertKeyExpression(handler.Target!),
                    handler.Identity is null ? null : ConvertKeyExpression(handler.Identity),
                    properties);
            }

            foreach (var removal in block.Blocks.OfType<GraphRemoveSyntax>())
            {
                removedWith[ResolveEventType(removal.Event).ToString()] = new GraphEdgeRemovedWithDefinition(
                    ConvertKeyExpression(removal.Source!),
                    ConvertKeyExpression(removal.Target!),
                    removal.Identity is null ? null : ConvertKeyExpression(removal.Identity));
            }
        }

        return new GraphEdgeProjectionDefinition(from, removedWith);
    }

    Dictionary<string, string> GraphProperties(
        Concepts.Events.EventType eventType,
        IEnumerable<PropertySyntax> candidates,
        IEnumerable<MappingSyntax> every,
        IEnumerable<MappingSyntax> mappings)
    {
        var explicitMappings = ProcessMappings(every.Concat(mappings)).ToDictionary(_ => _.Key.Path, _ => _.Value);
        var result = new Dictionary<string, string>();
        var eventProperties = EventPropertiesOf(eventType);
        foreach (var candidate in candidates)
        {
            if (explicitMappings.ContainsKey(candidate.Name))
            {
                continue;
            }

            if (eventProperties.Contains(candidate.Name))
            {
                result[candidate.Name] = candidate.Name;
            }
        }

        foreach (var (property, expression) in explicitMappings)
        {
            result[property] = expression;
        }

        return result;
    }

    HashSet<string> EventPropertiesOf(Concepts.Events.EventType eventType)
    {
        var schema = (eventTypeSchemas ?? []).Where(_ => _.Type.Id == eventType.Id)
            .OrderByDescending(_ => _.Type.Generation.Value)
            .Select(_ => _.Schema)
            .FirstOrDefault();

        var result = new HashSet<string>(StringComparer.Ordinal);
        if (schema is null)
        {
            return result;
        }

        foreach (var (name, property) in schema.Properties)
        {
            var isComplex = property.ActualSchema.Type.HasFlag(JsonObjectType.Object) ||
                (property.Item is { } item && item.ActualSchema.Type.HasFlag(JsonObjectType.Object));
            if (!isComplex)
            {
                result.Add(name);
            }
        }

        return result;
    }
}
