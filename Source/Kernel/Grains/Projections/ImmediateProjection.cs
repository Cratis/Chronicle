// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Kernel.Projections;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;
using Microsoft.Extensions.Logging;
using EngineProjection = Aksio.Cratis.Kernel.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjection"/>.
/// </summary>
public class ImmediateProjection : Grain, IImmediateProjection
{
    readonly IKernel _kernel;
    readonly IStorage _storage;
    readonly IObjectComparer _objectComparer;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<ImmediateProjection> _logger;
    IEventSequenceStorage? _eventSequenceStorage;
    ImmediateProjectionKey? _projectionKey;
    EventSequenceNumber _lastHandledEventSequenceNumber;
    ExpandoObject? _initialState;
    ProjectionId _projectionId = ProjectionId.NotSet;
    DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateProjection"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> to compare objects with.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between JSON and ExpandoObject.</param>
    /// <param name="logger">Logger for logging.</param>
    public ImmediateProjection(
        IKernel kernel,
        IStorage storage,
        IObjectComparer objectComparer,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<ImmediateProjection> logger)
    {
        _kernel = kernel;
        _storage = storage;
        _objectComparer = objectComparer;
        _expandoObjectConverter = expandoObjectConverter;
        _logger = logger;
        _lastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionId = this.GetPrimaryKey(out var keyAsString);
        _projectionKey = ImmediateProjectionKey.Parse(keyAsString);
        _eventSequenceStorage = _storage
                                    .GetEventStore((string)_projectionKey.MicroserviceId)
                                    .GetNamespace(_projectionKey.TenantId)
                                    .GetEventSequence(_projectionKey.EventSequenceId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetModelInstance()
    {
        using var scope = _logger.BeginImmediateProjectionScope(_projectionId, _projectionKey!);

        _logger.GettingModelInstance();

        try
        {
            var eventStore = _kernel.GetEventStore((string)_projectionKey!.MicroserviceId);
            var @namespace = eventStore.GetNamespace(_projectionKey!.TenantId);

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

            var eventSequence = GrainFactory.GetGrain<IEventSequence>(_projectionKey.EventSequenceId, new MicroserviceAndTenant(_projectionKey.MicroserviceId, _projectionKey.TenantId));
            var tail = await eventSequence.GetTailSequenceNumberForEventTypes(projection.EventTypes);
            if (tail != EventSequenceNumber.Unavailable && tail < fromSequenceNumber && _initialState != null && !projectionChanged)
            {
                _logger.UsingCachedModelInstance();
                var initialStateAsJson = _expandoObjectConverter.ToJsonObject(_initialState, projection.Model.Schema);
                return new(initialStateAsJson, Enumerable.Empty<PropertyPath>(), 0);
            }

            if (!projection.EventTypes.Any())
            {
                _logger.NoEventTypes();
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
            var jsonObject = _expandoObjectConverter.ToJsonObject(state, projection.Model.Schema);
            return new(jsonObject, affectedProperties, projectedEventsCount);
        }
        catch (Exception ex)
        {
            _logger.FailedGettingModelInstance(ex);
            return ImmediateProjectionResult.Empty;
        }
    }

    /// <inheritdoc/>
    public async Task<ImmediateProjectionResult> GetCurrentModelInstanceWithAdditionalEventsApplied(IEnumerable<EventToApply> events)
    {
        var @namespace = _kernel.GetEventStore((string)_projectionKey!.MicroserviceId).GetNamespace(_projectionKey!.TenantId);

        var projection = @namespace.ProjectionManager.Get(_projectionId);
        var affectedProperties = new HashSet<PropertyPath>();

        var eventTypesStorage = _storage.GetEventStore((string)_projectionKey!.MicroserviceId).EventTypes;
        var eventsToApplyTasks = events.Select(async _ =>
        {
            var eventSchema = await eventTypesStorage.GetFor(_.EventType.Id, _.EventType.Generation);
            return AppendedEvent.EmptyWithEventType(_.EventType) with
            {
                Content = _expandoObjectConverter.ToExpandoObject(_.Content, eventSchema.Schema)
            };
        }).ToArray();
        var eventsToApply = await Task.WhenAll(eventsToApplyTasks);
        var initialState = _initialState ?? new ExpandoObject();
        var result = await HandleEvents(projection, affectedProperties, initialState, eventsToApply);
        var jsonObject = _expandoObjectConverter.ToJsonObject(result.State, projection.Model.Schema);
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
            return _expandoObjectConverter.ToExpandoObject(projectionDefinition.InitialModelState, projection.Model.Schema);
        }

        return new ExpandoObject();
    }

    async Task<(int ProjectedEventsCount, ExpandoObject State)> HandleEvents(EngineProjection projection, HashSet<PropertyPath> affectedProperties, ExpandoObject initialState, AppendedEvent[] events)
    {
        var projectedEventsCount = 0;
        var state = initialState;

        foreach (var @event in events)
        {
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(_objectComparer, @event, state);
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
