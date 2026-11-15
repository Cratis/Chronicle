// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.Graphs;
using Cratis.Chronicle.Projections.Engine.Graphs.Views;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents an implementation of <see cref="IGraphProjectionPreviewer"/> that runs the events of the event store through
/// the graph projection engine against an in-memory graph sink.
/// </summary>
/// <remarks>
/// The preview builds the changesets of the saved pipeline with the same <see cref="GraphChangesetBuilder"/> and applies them
/// to the in-memory graph sink, so counters, key expressions, removals and replay-free semantics are those of a registered
/// graph projection. Nothing is written to a graph database, and the graph is discarded afterwards.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> to read events from.</param>
/// <param name="languageService">The <see cref="ILanguageService"/> that compiles the declaration.</param>
/// <param name="plans">The <see cref="IGraphProjectionPlanFactory"/> that resolves the mappings.</param>
/// <param name="eventCompliance">The <see cref="IEventCompliance"/> that releases protected values in the events.</param>
/// <param name="namespaceCasing">The <see cref="INamespaceCasing"/> that resolves the namespace as the store knows it.</param>
/// <param name="sinkFactories">The <see cref="ISinkFactory">sink factories</see>, one of which creates the in-memory graph sink.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
[Singleton]
internal sealed class GraphProjectionPreviewer(
    IStorage storage,
    ILanguageService languageService,
    IGraphProjectionPlanFactory plans,
    IEventCompliance eventCompliance,
    INamespaceCasing namespaceCasing,
    IInstancesOf<ISinkFactory> sinkFactories,
    ILogger<GraphProjectionPreviewer> logger) : IGraphProjectionPreviewer
{
    /// <summary>
    /// The number of events a preview runs through the projection at most, as for a preview of a projection onto a read model.
    /// </summary>
    internal const int EventLimit = 1000;

    /// <inheritdoc/>
    public async Task<GraphProjectionPreview> Preview(PreviewGraphProjection request)
    {
        if (!languageService.IsGraphDeclaration(request.Declaration))
        {
            return Failed(new ProjectionDeclarationSyntaxError("A graph declaration needs 'projection <Name> => graph <GraphName>' and the graph it builds", 1, 1));
        }

        var ns = await namespaceCasing.Canonicalize(request.EventStore, request.Namespace);
        var eventStore = storage.GetEventStore(request.EventStore);
        var eventTypeSchemas = (await eventStore.EventTypes.GetLatestForAllEventTypes()).ToArray();

        var compiled = languageService.CompileGraph(request.Declaration, ProjectionOwner.Server, eventTypeSchemas);
        return await compiled.Match(
            graph => Run(request, ns, graph, eventTypeSchemas),
            errors => Task.FromResult(Failed([.. errors.Errors.Select(_ => new ProjectionDeclarationSyntaxError(_.Message, _.Line, _.Column))])));
    }

    static GraphProjectionPreview Failed(params ProjectionDeclarationSyntaxError[] errors) =>
        new(null, [], [], [], 0, [], [], errors);

    static GraphPreviewSchema SchemaOf(GraphDefinition graph) =>
        new(
            graph.Name,
            [.. graph.Nodes.Select(node => new GraphPreviewNodeType(node.Label, node.KeyProperty, [.. node.Schema.Properties.Keys]))],
            [.. graph.Edges.Select(edge => new GraphPreviewEdgeType(edge.Type, edge.SourceLabel, edge.TargetLabel, edge.IdentifyingProperty, [.. edge.Schema.Properties.Keys]))]);

    async Task<GraphProjectionPreview> Run(PreviewGraphProjection request, EventStoreNamespaceName ns, CompiledGraph compiled, EventTypeSchema[] eventTypeSchemas)
    {
        var graph = GraphDefinition.FromSchema(compiled.ReadModel.GetSchemaForLatestGeneration());
        GraphProjectionPlan plan;
        try
        {
            plan = plans.Create(compiled.Projection.Identifier, compiled.Projection.Graph!, graph);
        }
        catch (InvalidGraphProjection ex)
        {
            return Failed(new ProjectionDeclarationSyntaxError(ex.Message, 1, 1));
        }

        if (sinkFactories.OfType<IGraphSinkFactory>().FirstOrDefault(_ => _.TypeId == WellKnownSinkTypes.InMemoryGraph) is not { } factory)
        {
            return Failed(new ProjectionDeclarationSyntaxError("This server cannot preview a graph projection: it has no in-memory graph sink", 1, 1));
        }

        var sink = factory.CreateGraphFor(request.EventStore, ns, compiled.ReadModel);
        await sink.EnsureSchema();

        var events = await ReadEvents(request.EventStore, ns, plan, eventTypeSchemas);
        var eventErrors = await Apply(plan, sink, compiled.Projection.Identifier, events);

        var nodes = new List<GraphPreviewNode>();
        foreach (var node in graph.Nodes)
        {
            nodes.AddRange((await sink.GetNodes(node.Label, 0, int.MaxValue)).Elements
                .Select(_ => new GraphPreviewNode(node.Label, GraphValues.TextOf(_.Reference.Key), _.Properties.ToJson())));
        }

        var edges = new List<GraphPreviewEdge>();
        foreach (var edge in graph.Edges)
        {
            edges.AddRange((await sink.GetEdges(edge.Type, 0, int.MaxValue)).Elements
                .Select(_ => new GraphPreviewEdge(
                    edge.Type,
                    _.Reference.Source.Label,
                    GraphValues.TextOf(_.Reference.Source.Key),
                    _.Reference.Target.Label,
                    GraphValues.TextOf(_.Reference.Target.Key),
                    _.Reference.Identity is null ? null : GraphValues.TextOf(_.Reference.Identity),
                    _.Properties.ToJson())));
        }

        var viewErrors = new List<GraphViewError>();
        var rows = await ReadView(request, graph, sink, viewErrors);
        return new(SchemaOf(graph), nodes, edges, rows, events.Length, eventErrors, viewErrors, []);
    }

    async Task<AppendedEvent[]> ReadEvents(EventStoreName eventStore, EventStoreNamespaceName ns, GraphProjectionPlan plan, EventTypeSchema[] eventTypeSchemas)
    {
        var eventSequence = storage.GetEventStore(eventStore).GetNamespace(ns).GetEventSequence(EventSequenceId.Log);
        using var cursor = await eventSequence.GetEventsWithLimit(EventSequenceNumber.First, EventLimit, eventTypes: plan.EventTypes.ToArray());
        var events = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            events.AddRange(cursor.Current);
        }

        return await eventCompliance.Release(events, eventTypeSchemas.ToDictionary(_ => _.Type));
    }

    async Task<List<GraphPreviewEventError>> Apply(GraphProjectionPlan plan, IGraphSink sink, ProjectionId owner, AppendedEvent[] events)
    {
        var builder = new GraphChangesetBuilder(plan, sink, owner);
        var errors = new List<GraphPreviewEventError>();
        foreach (var @event in events)
        {
            try
            {
                var changeset = await builder.Build(@event);
                if (changeset.HasChanges)
                {
                    await sink.ApplyGraphChanges(changeset, @event.Context.SequenceNumber);
                }
            }
            catch (Exception ex)
            {
                logger.EventCouldNotBeApplied(@event.Context.SequenceNumber, ex);
                errors.Add(new(@event.Context.SequenceNumber, ex.Message));
            }
        }

        return errors;
    }

    async Task<IEnumerable<System.Text.Json.Nodes.JsonObject>> ReadView(PreviewGraphProjection request, GraphDefinition graph, IGraphSink sink, List<GraphViewError> viewErrors)
    {
        var views = request.Views.ToViews(viewErrors);
        if (views.Count == 0 || viewErrors.Count > 0)
        {
            return [];
        }

        var viewName = string.IsNullOrEmpty(request.View) ? views[0].Name : request.View;
        var clauses = request.Clauses.ToClauses(viewName, viewErrors);
        if (viewErrors.Count > 0)
        {
            return [];
        }

        try
        {
            var rows = await GraphViewEvaluator.Evaluate(sink, graph, views, new(viewName, clauses, request.FirstOnly));
            return [.. rows.Select(_ => _.ToJson())];
        }
        catch (InvalidGraphViews ex)
        {
            viewErrors.AddRange(ex.Issues.Select(_ => new GraphViewError(_.Reason, _.View, _.Member, _.Message)));
            return [];
        }
    }
}
