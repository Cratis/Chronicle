// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IImmediateProjection"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ImmediateProjection"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> to convert between JSON and ExpandoObject.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class ImmediateProjection(
    IStorage storage,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<ImmediateProjection> logger) : Grain<ProjectionDefinition>, IImmediateProjection
{
    IEventSequenceStorage? _eventSequenceStorage;
    ImmediateProjectionKey? _projectionKey;
    EventSequenceNumber _lastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
    ExpandoObject? _initialState;
    ReadModelDefinition? _readModelDefinition;
    IProjection? _projection;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _projectionKey = ImmediateProjectionKey.Parse(this.GetPrimaryKeyString());
        _eventSequenceStorage = storage
                                    .GetEventStore(_projectionKey.EventStore)
                                    .GetNamespace(_projectionKey.Namespace)
                                    .GetEventSequence(_projectionKey.EventSequenceId);

        _projection = GrainFactory.GetGrain<IProjection>(new ProjectionKey(_projectionKey.ProjectionId, _projectionKey.EventStore));
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetModelInstance()
    {
        using var scope = logger.BeginImmediateProjectionScope(_projectionKey!.ProjectionId, _projectionKey!);

        logger.GettingModelInstance();

        try
        {
            var readModelKey = new ReadModelGrainKey(State.ReadModel, _projectionKey.EventStore);
            var readModel = GrainFactory.GetGrain<IReadModel>(readModelKey);
            _readModelDefinition = await readModel.GetDefinition();
            var eventTypes = await _projection!.GetEventTypes();
            var fromSequenceNumber = _lastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : _lastHandledEventSequenceNumber.Next();

            var eventSequenceKey = new EventSequenceKey(_projectionKey!.EventSequenceId, _projectionKey!.EventStore, _projectionKey!.Namespace);
            var eventSequence = GrainFactory.GetGrain<IEventSequence>(eventSequenceKey);
            var tail = await eventSequence.GetTailSequenceNumberForEventTypes(eventTypes);
            if (tail != EventSequenceNumber.Unavailable && tail < fromSequenceNumber && _initialState != null)
            {
                logger.UsingCachedModelInstance();
                var initialStateAsJson = expandoObjectConverter.ToJsonObject(_initialState, _readModelDefinition!.GetSchemaForLatestGeneration());
                return new(initialStateAsJson, 0, tail);
            }

            var modelKey = _projectionKey.ReadModelKey.IsSpecified ? (EventSourceId)_projectionKey.ReadModelKey.Value : null!;
            using var cursor = await _eventSequenceStorage!.GetFromSequenceNumber(fromSequenceNumber, modelKey, eventTypes: eventTypes);
            var projectedEventsCount = 0;
            var state = GetInitialState();
            while (await cursor.MoveNext())
            {
                if (!cursor.Current.Any())
                {
                    break;
                }

                var events = cursor.Current.ToArray();
                var result = await HandleEvents(state, events);
                projectedEventsCount += result.ProjectedEventsCount;
                state = result.State;

                _lastHandledEventSequenceNumber = events[^1].Context.SequenceNumber;
            }

            _initialState = state;
            var jsonObject = expandoObjectConverter.ToJsonObject(state, _readModelDefinition!.GetSchemaForLatestGeneration());
            return new(jsonObject, projectedEventsCount, _lastHandledEventSequenceNumber);
        }
        catch (Exception ex)
        {
            logger.FailedGettingModelInstance(ex);
            return ProjectionResult.Empty;
        }
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetCurrentModelInstanceWithAdditionalEventsApplied(IEnumerable<EventToApply> events)
    {
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
        var result = await HandleEvents(initialState, eventsToApply);
        _initialState = result.State;
        var jsonObject = expandoObjectConverter.ToJsonObject(result.State, _readModelDefinition!.GetSchemaForLatestGeneration());
        return new(jsonObject, result.ProjectedEventsCount, result.Tail);
    }

    /// <inheritdoc/>
    public Task Dehydrate()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }

    ExpandoObject GetInitialState()
    {
        return _initialState ?? (State.InitialModelState is not null
            ? expandoObjectConverter.ToExpandoObject(State.InitialModelState, _readModelDefinition!.GetSchemaForLatestGeneration())
            : new ExpandoObject());
    }

    async Task<(int ProjectedEventsCount, ExpandoObject State, EventSequenceNumber Tail)> HandleEvents(ExpandoObject initialState, AppendedEvent[] events)
    {
        var projectionKey = new ProjectionKey(_projectionKey!.ProjectionId, _projectionKey!.EventStore);
        var projectionGrain = GrainFactory.GetGrain<IProjection>(projectionKey);

        var eventsWithSequenceNumber = events.Where(_ => _.Context.SequenceNumber != EventSequenceNumber.Unavailable).ToArray();
        var eventSequenceNumber = eventsWithSequenceNumber.Length > 0
            ? eventsWithSequenceNumber[^1].Context.SequenceNumber
            : EventSequenceNumber.Unavailable;

        var state = await projectionGrain.Process(_projectionKey!.Namespace, initialState, events);

        var projectedEventsCount = events.Count(_ => _ is not null);

        return (projectedEventsCount, state, eventSequenceNumber);
    }
}
