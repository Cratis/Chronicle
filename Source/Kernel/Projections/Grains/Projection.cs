// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Pipelines;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Events.Store.Grains.Observation;
using Aksio.Cratis.Events.Store.Observation;
using Orleans;
using Orleans.Streams;
using EngineProjection = Aksio.Cratis.Events.Projections.IProjection;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
public class Projection : Grain, IProjection
{
    readonly IProjectionDefinitions _projectionDefinitions;
    readonly IProjectionPipelineDefinitions _projectionPipelineDefinitions;
    readonly IProjectionFactory _projectionFactory;
    readonly IProjectionPipelineFactory _projectionPipelineFactory;
    readonly IObjectsComparer _objectsComparer;
    readonly IEventLogStorageProvider _eventProvider;
    EngineProjection? _projection;
    IProjectionPipeline? _pipeline;
    IObserver? _observer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Projection"/> class.
    /// </summary>
    /// <param name="projectionDefinitions"><see cref="IProjectionDefinitions"/>.</param>
    /// <param name="projectionPipelineDefinitions"><see cref="IProjectionPipelineDefinitions"/> for working with pipelines.</param>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
    /// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> for creating the pipeline for the projection.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
    /// <param name="eventProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
    public Projection(
        IProjectionDefinitions projectionDefinitions,
        IProjectionPipelineDefinitions projectionPipelineDefinitions,
        IProjectionFactory projectionFactory,
        IProjectionPipelineFactory projectionPipelineFactory,
        IObjectsComparer objectsComparer,
        IEventLogStorageProvider eventProvider)
    {
        _projectionDefinitions = projectionDefinitions;
        _projectionPipelineDefinitions = projectionPipelineDefinitions;
        _projectionFactory = projectionFactory;
        _projectionPipelineFactory = projectionPipelineFactory;
        _objectsComparer = objectsComparer;
        _eventProvider = eventProvider;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        var projectionId = this.GetPrimaryKey(out var keyAsString);
        var key = ProjectionKey.Parse(keyAsString);
        var projectionDefinition = await _projectionDefinitions.GetFor(projectionId);
        var pipelineDefinition = await _projectionPipelineDefinitions.GetFor(projectionId);
        _projection = await _projectionFactory.CreateFrom(projectionDefinition);
        _pipeline = _projectionPipelineFactory.CreateFrom(_projection, pipelineDefinition);

        _observer = GrainFactory.GetGrain<IObserver>(projectionId, new ObserverKey(key.MicroserviceId, key.TenantId, key.EventSequenceId));

        var observerNamespace = new ObserverNamespace(projectionId.ToString());
        var streamProvider = GetStreamProvider(WellKnownProviders.ObserverHandlersStreamProvider);
        var stream = streamProvider.GetStream<AppendedEvent>(projectionId, key);
        await stream.SubscribeAsync(HandleEvent);

        await _observer.Subscribe(projectionDefinition.Name.Value, _projection.EventTypes, key.ToString());
    }

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task<JsonObject> GetModelInstanceById(EventSourceId eventSourceId)
    {
        if (_projection is null)
        {
            return new JsonObject();
        }
        var cursor = await _eventProvider.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId, _projection.EventTypes);
        var state = new ExpandoObject();
        while (await cursor.MoveNext())
        {
            if (!cursor.Current.Any())
            {
                break;
            }

            foreach (var @event in cursor.Current)
            {
                var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectsComparer, @event, state);
                var keyResolver = _projection.GetKeyResolverFor(@event.Metadata.Type);
                var key = await keyResolver(_eventProvider!, @event);
                var context = new ProjectionEventContext(key, @event, changeset);

                await HandleEventFor(_projection!, context);

                foreach (var change in changeset.Changes)
                {
                    switch (change)
                    {
                        case PropertiesChanged<ExpandoObject> propertiesChanged:
                            state = state.OverwriteWith((change.State as ExpandoObject)!);
                            break;

                        case ChildAdded childAdded:
                            var items = state.EnsureCollection<ExpandoObject>(childAdded.ChildrenProperty, key.ArrayIndexers);
                            items.Add(childAdded.Child.AsExpandoObject());
                            break;
                    }
                }
            }
        }

        // TODO: Conversion from ExpandoObject to JsonObject can be improved - they're effectively both just Dictionary<string, object>
        var json = JsonSerializer.Serialize(state);
        var jsonObject = JsonNode.Parse(json)!;
        return (jsonObject as JsonObject)!;
    }

    /// <inheritdoc/>
    public Task Rewind()
    {
        _observer?.Rewind();
        return Task.CompletedTask;
    }

    Task HandleEvent(AppendedEvent @event, StreamSequenceToken token) => _pipeline?.Handle(@event) ?? Task.CompletedTask;

    async Task HandleEventFor(EngineProjection projection, ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Metadata.Type))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            await HandleEventFor(child, context);
        }
    }
}
