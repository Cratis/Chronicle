// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Dynamic;
using Cratis.Events;
using Cratis.Json;
using Cratis.Kernel.Grains.EventSequences;
using Cratis.Kernel.Keys;
using Cratis.Kernel.Projections;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Projections;
using Cratis.Projections.Definitions;
using Cratis.Properties;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Kernel.Projections.IProjection;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjection"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ImmediateProjection"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> to compare objects with.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between JSON and ExpandoObject.</param>
/// <param name="logger">Logger for logging.</param>
public class ImmediateProjection(
    IKernel kernel,
    IStorage storage,
    IObjectComparer objectComparer,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ImmediateProjection> logger) : Grain, IImmediateProjection
{
    IEventSequenceStorage? _eventSequenceStorage;
    ImmediateProjectionKey? _projectionKey;
    EventSequenceNumber _lastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
    ExpandoObject? _initialState;
    ProjectionId _projectionId = ProjectionId.NotSet;
    DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        _projectionKey = ImmediateProjectionKey.Parse(keyAsString);
        _eventSequenceStorage = storage
                                    .GetEventStore(_projectionKey.EventStore)
                                    .GetNamespace(_projectionKey.Namespace)
                                    .GetEventSequence(_projectionKey.EventSequenceId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetModelInstance()
    {
        using var scope = logger.BeginImmediateProjectionScope(_projectionId, _projectionKey!);

        logger.GettingModelInstance();

        try
        {
            var eventStore = kernel.GetEventStore(_projectionKey!.EventStore);
            var @namespace = eventStore.GetNamespace(_projectionKey!.Namespace);

            var projectionChanged = false;

            var projection = @namespace.ProjectionManager.Get(_projectionId);
            var (foundProjection, definition) = await eventStore.ProjectionDefinitions.TryGetFor(_projectionId);

            var fromSequenceNumber = _lastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : _lastHandledEventSequenceNumber.Next();
            if (foundProjection && definition is not null)
            {
                projectionChanged = definition.LastUpdated > _lastUpdated;
                _lastUpdated = definition.LastUpdated ?? DateTimeOffset.UtcNow;
                fromSequenceNumber = EventSequenceNumber.First;
            }

            var eventSequence = GrainFactory.GetGrain<IEventSequence>(_projectionKey.EventSequenceId, new EventStoreAndNamespace(_projectionKey.EventStore, _projectionKey.Namespace));
            var tail = await eventSequence.GetTailSequenceNumberForEventTypes(projection.EventTypes);
            if (tail != EventSequenceNumber.Unavailable && tail < fromSequenceNumber && _initialState != null && !projectionChanged)
            {
                logger.UsingCachedModelInstance();
                var initialStateAsJson = expandoObjectConverter.ToJsonObject(_initialState, projection.Model.Schema);
                return new(initialStateAsJson, [], 0);
            }

            if (!projection.EventTypes.Any())
            {
                logger.NoEventTypes();
                return ImmediateProjectionResult.Empty;
            }

            var affectedProperties = new HashSet<PropertyPath>();

            var modelKey = _projectionKey.ModelKey.IsSpecified ? (EventSourceId)_projectionKey.ModelKey.Value : null!;
            var cursor = await _eventSequenceStorage!.GetFromSequenceNumber(fromSequenceNumber, modelKey, projection.EventTypes);
            var projectedEventsCount = 0;
            var state = GetInitialState(projection, definition);
            while (await cursor.MoveNext())
            {
                if (!cursor.Current.Any())
                {
                    break;
                }

                var events = cursor.Current.ToArray();
                var result = await HandleEvents(projection, affectedProperties, state, events);
                projectedEventsCount += result.ProjectedEventsCount;
                state = result.State;

                _lastHandledEventSequenceNumber = events[^1].Metadata.SequenceNumber;
            }

            _initialState = state;
            var jsonObject = expandoObjectConverter.ToJsonObject(state, projection.Model.Schema);
            return new(jsonObject, affectedProperties, projectedEventsCount);
        }
        catch (Exception ex)
        {
            logger.FailedGettingModelInstance(ex);
            return ImmediateProjectionResult.Empty;
        }
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetCurrentModelInstanceWithAdditionalEventsApplied(IEnumerable<EventToApply> events)
    {
        var @namespace = kernel.GetEventStore(_projectionKey!.EventStore).GetNamespace(_projectionKey!.Namespace);

        var projection = @namespace.ProjectionManager.Get(_projectionId);
        var affectedProperties = new HashSet<PropertyPath>();

        var eventTypesStorage = storage.GetEventStore(_projectionKey!.EventStore).EventTypes;
        var eventsToApplyTasks = events.Select(async _ =>
        {
            var eventSchema = await eventTypesStorage.GetFor(_.EventType.Id, _.EventType.Generation);
            return AppendedEvent.EmptyWithEventType(_.EventType) with
            {
                Content = expandoObjectConverter.ToExpandoObject(_.Content, eventSchema.Schema)
            };
        }).ToArray();
        var eventsToApply = await Task.WhenAll(eventsToApplyTasks);
        var initialState = _initialState ?? new ExpandoObject();
        var result = await HandleEvents(projection, affectedProperties, initialState, eventsToApply);
        var jsonObject = expandoObjectConverter.ToJsonObject(result.State, projection.Model.Schema);
        return new(jsonObject, affectedProperties, result.ProjectedEventsCount);
    }

    /// <inheritdoc/>
    public Task Dehydrate()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    ExpandoObject GetInitialState(EngineProjection projection, ProjectionDefinition? projectionDefinition)
    {
        if (_initialState is not null)
        {
            return _initialState;
        }

        if (projectionDefinition?.InitialModelState is not null)
        {
            return expandoObjectConverter.ToExpandoObject(projectionDefinition.InitialModelState, projection.Model.Schema);
        }

        return new ExpandoObject();
    }

    async Task<(int ProjectedEventsCount, ExpandoObject State)> HandleEvents(EngineProjection projection, HashSet<PropertyPath> affectedProperties, ExpandoObject initialState, AppendedEvent[] events)
    {
        var projectedEventsCount = 0;
        var state = initialState;

        foreach (var @event in events)
        {
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, state);
            var keyResolver = projection.GetKeyResolverFor(@event.Metadata.Type);
            var key = await keyResolver(_eventSequenceStorage!, @event);
            var context = new ProjectionEventContext(key, @event, changeset);

            await HandleEventFor(projection!, context);

            projectedEventsCount++;

            state = ApplyActualChanges(key, changeset.Changes, changeset.InitialState, affectedProperties);
        }

        return (projectedEventsCount, state);
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
