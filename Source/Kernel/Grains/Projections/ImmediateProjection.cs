// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Engines.Projections;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;
using EngineProjection = Aksio.Cratis.Kernel.Engines.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjection"/>.
/// </summary>
public class ImmediateProjection : Grain, IImmediateProjection
{
    readonly IProjectionFactory _projectionFactory;
    readonly IObjectComparer _objectComparer;
    readonly IEventSequenceStorage _eventProvider;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly IExecutionContextManager _executionContextManager;
    EngineProjection? _projection;
    ImmediateProjectionKey? _projectionKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjection"/> class.
    /// </summary>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating engine projections.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> to compare objects with.</param>
    /// <param name="eventProvider"><see cref="IEventSequenceStorage"/> for getting events from storage.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between JSON and ExpandoObject.</param>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/>.</param>
    public ImmediateProjection(
        IProjectionFactory projectionFactory,
        IObjectComparer objectComparer,
        IEventSequenceStorage eventProvider,
        IExpandoObjectConverter expandoObjectConverter,
        IExecutionContextManager executionContextManager)
    {
        _projectionFactory = projectionFactory;
        _objectComparer = objectComparer;
        _eventProvider = eventProvider;
        _expandoObjectConverter = expandoObjectConverter;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
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

        _projection ??= await _projectionFactory.CreateFrom(projectionDefinition);

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
                var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, @event, state);
                var keyResolver = _projection.GetKeyResolverFor(@event.Metadata.Type);
                var key = await keyResolver(_eventProvider!, @event);
                var context = new ProjectionEventContext(key, @event, changeset);

                await HandleEventFor(_projection!, context);

                projectedEventsCount++;

                state = ApplyActualChanges(key, changeset.Changes, changeset.InitialState, affectedProperties);
            }
        }

        var jsonObject = _expandoObjectConverter.ToJsonObject(state, _projection.Model.Schema);
        return new(jsonObject, affectedProperties, projectedEventsCount);
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

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state, HashSet<PropertyPath> affectedProperties)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    foreach (var difference in propertiesChanged.Differences)
                    {
                        affectedProperties.Add(difference.PropertyPath);
                    }
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case ChildAdded childAdded:
                    var items = state.EnsureCollection<object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    items.Add(childAdded.Child);
                    break;

                case Joined joined:
                    state = ApplyActualChanges(key, joined.Changes, state, affectedProperties);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(key, resolvedJoin.Changes, state, affectedProperties);
                    break;
            }
        }

        return state;
    }
}
