// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Properties;
using Orleans;
using EngineProjection = Aksio.Cratis.Events.Projections.IProjection;

namespace Aksio.Cratis.Events.Projections.Grains;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjection"/>.
/// </summary>
public class ImmediateProjection : Grain, IImmediateProjection
{
    readonly IProjectionFactory _projectionFactory;
    readonly IObjectsComparer _objectsComparer;
    readonly IEventSequenceStorageProvider _eventProvider;
    readonly IExecutionContextManager _executionContextManager;
    EngineProjection? _projection;
    ImmediateProjectionKey? _projectionKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjection"/> class.
    /// </summary>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
    /// <param name="eventProvider"><see cref="IEventSequenceStorageProvider"/> for getting events from storage.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    public ImmediateProjection(
        IProjectionFactory projectionFactory,
        IObjectsComparer objectsComparer,
        IEventSequenceStorageProvider eventProvider,
        IExecutionContextManager executionContextManager)
    {
        _projectionFactory = projectionFactory;
        _objectsComparer = objectsComparer;
        _eventProvider = eventProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        this.GetPrimaryKey(out var keyAsString);
        _projectionKey = ImmediateProjectionKey.Parse(keyAsString);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetModelInstance(ProjectionDefinition projectionDefinition)
    {
        if (_projectionKey is null)
        {
            return new ImmediateProjectionResult(new JsonObject(), Array.Empty<PropertyPath>(), 0);
        }

        if (_projection is null)
        {
            // TODO: This can be optimized by extracting out to a separate singleton service that holds these in-memory
            _projection = await _projectionFactory.CreateFrom(projectionDefinition);
        }

        // TODO: This is a temporary work-around till we fix #264 & #265
        _executionContextManager.Establish(_projectionKey.TenantId, CorrelationId.New(), _projectionKey.MicroserviceId);

        var affectedProperties = new HashSet<PropertyPath>();

        var modelKey = _projectionKey.ModelKey.IsSpecified ? (EventSourceId)_projectionKey.ModelKey.Value : null!;

        var cursor = await _eventProvider.GetFromSequenceNumber(EventSequenceId.Log, EventSequenceNumber.First, modelKey, _projection.EventTypes);
        var projectedEventsCount = 0;
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

                projectedEventsCount++;

                foreach (var change in changeset.Changes)
                {
                    switch (change)
                    {
                        case PropertiesChanged<ExpandoObject> propertiesChanged:
                            foreach (var difference in propertiesChanged.Differences)
                            {
                                affectedProperties.Add(difference.PropertyPath);
                            }

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
        return new((jsonObject as JsonObject)!, affectedProperties, projectedEventsCount);
    }

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
