// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelGraphs = KernelConcepts::Cratis.Chronicle.Concepts.Graphs;
using KernelProjectionEngine = KernelCore::Cratis.Chronicle.Projections.Engine;
using KernelProjections = KernelConcepts::Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Represents a graph registration running against an in-memory graph sink, through the kernel's graph projection engine.
/// </summary>
internal sealed class RunningGraph
{
    static readonly TypeFormats _typeFormats = new();
    static readonly KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers _eventValues = new(
        _typeFormats,
        NullLogger<KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers>.Instance);

    static readonly KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers _properties = new(
        _eventValues,
        _typeFormats,
        NullLogger<KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers>.Instance);

    readonly KernelProjectionEngine::Graphs.GraphChangesetBuilder _builder;

    RunningGraph(KernelGraphs::GraphDefinition definition, KernelProjections::ProjectionId owner, InMemoryGraphSink sink, KernelProjectionEngine::Graphs.GraphChangesetBuilder builder)
    {
        Definition = definition;
        Owner = owner;
        Sink = sink;
        _builder = builder;
    }

    /// <summary>
    /// Gets the definition of the graph.
    /// </summary>
    public KernelGraphs::GraphDefinition Definition { get; }

    /// <summary>
    /// Gets the identifier of the projection that owns what is written.
    /// </summary>
    public KernelProjections::ProjectionId Owner { get; }

    /// <summary>
    /// Gets the sink the graph is stored in.
    /// </summary>
    public InMemoryGraphSink Sink { get; }

    /// <summary>
    /// Create a running graph from a registration.
    /// </summary>
    /// <param name="registration">The <see cref="GraphRegistration"/> to run.</param>
    /// <returns>The <see cref="RunningGraph"/>.</returns>
    public static RunningGraph Create(GraphRegistration registration)
    {
        var schema = JsonSchema.FromJson(registration.ReadModel.Schema);
        var definition = KernelGraphs::GraphDefinition.FromSchema(schema);
        var projection = KernelGrpc::Cratis.Chronicle.Services.Projections.Definitions.ProjectionDefinitionConverters.ToChronicle(
            registration.Projection,
            KernelProjections::ProjectionOwner.Client);
        var plan = new KernelProjectionEngine::Graphs.GraphProjectionPlanFactory(_properties, _eventValues).Create(projection.Identifier, projection.Graph!, definition);
        var sink = new InMemoryGraphSink(definition);
        return new(definition, projection.Identifier, sink, new(plan, sink, projection.Identifier));
    }

    /// <summary>
    /// Project an event onto the graph.
    /// </summary>
    /// <param name="event">The event to project.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Project(KernelEvents::AppendedEvent @event)
    {
        var changeset = await _builder.Build(@event);
        if (changeset.HasChanges)
        {
            await Sink.ApplyGraphChanges(changeset, @event.Context.SequenceNumber);
        }
    }
}
