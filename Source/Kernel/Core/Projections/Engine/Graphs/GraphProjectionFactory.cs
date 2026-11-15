// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphProjectionFactory"/>.
/// </summary>
/// <remarks>
/// The engine projection it creates is a <see cref="Projection"/> without children, so the observer subscribes to, routes
/// and replays it like every other projection. What it does for an event is in <see cref="IProjection.Graph"/>.
/// </remarks>
/// <param name="plans"><see cref="IGraphProjectionPlanFactory"/> for resolving the graph mappings.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers"/> for the key the observer partitions events by.</param>
[Singleton]
public class GraphProjectionFactory(IGraphProjectionPlanFactory plans, IKeyResolvers keyResolvers) : IGraphProjectionFactory
{
    /// <inheritdoc/>
    public IProjection Create(ProjectionDefinition definition, ReadModelDefinition readModel, string fingerprint)
    {
        var graph = GraphDefinition.FromSchema(readModel.GetSchemaForLatestGeneration());
        var plan = plans.Create(definition.Identifier, definition.Graph!, graph);

        var projection = new Projection(
            definition.EventSequenceId,
            definition.Identifier,
            new ExpandoObject(),
            ProjectionPath.GetRootFor(definition.Identifier),
            PropertyPath.Root,
            PropertyPath.Root,
            readModel,
            readModel.GetTargetSchema(),
            definition.IsRewindable,
            AutoMap.Disabled,
            new HashSet<string>(),
            [],
            scope: definition.Scope)
        {
            GenerationDelivery = definition.GenerationDelivery,
            Graph = plan,
            DefinitionFingerprint = fingerprint
        };

        var eventTypes = plan.EventTypes.ToArray();
        projection.SetEventTypesWithKeyResolvers(
            eventTypes.Select(eventType => new EventTypeWithKeyResolver(eventType, keyResolvers.FromEventSourceId, true)),
            eventTypes,
            eventTypes.ToDictionary(eventType => eventType, _ => ProjectionOperationType.From));
        return projection;
    }
}
