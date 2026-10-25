// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Projections;

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
    ILogger<ImmediateProjection> logger) : Grain<ProjectionDefinition>, IImmediateProjection, INotifyProjectionDefinitionsChanged
{
    /// <summary>
    /// The maximum lifetime of a session activation without explicit dehydration.
    /// </summary>
    internal static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The one-shot, non-keep-alive session expiry timer options.
    /// </summary>
    internal static readonly GrainTimerCreationOptions SessionTimerOptions = new()
    {
        DueTime = SessionLifetime,
        Period = Timeout.InfiniteTimeSpan,
        Interleave = false,
        KeepAlive = false
    };

    IGrainTimer? _sessionExpiry;
    IEventSequenceStorage? _eventSequenceStorage;
    ImmediateProjectionKey? _projectionKey;
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
        await _projection.SubscribeDefinitionsChanged(GetSelfGrainReference());
        if (_projectionKey.SessionId is not null)
        {
            _sessionExpiry = this.RegisterGrainTimer(
                _ =>
                {
                    DeactivateSession();
                    return Task.CompletedTask;
                },
                SessionTimerOptions);
        }
    }

    /// <inheritdoc/>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _sessionExpiry?.Dispose();
        _sessionExpiry = null;
        if (reason.ReasonCode == DeactivationReasonCode.ShuttingDown)
        {
            // The projection grain is deactivating with the silo too, so the call could not complete.
            logger.SkippingDefinitionUnsubscribeDuringShutdown();
            return;
        }

        if (_projection is not null)
        {
            await _projection.UnsubscribeDefinitionsChanged(GetSelfGrainReference());
        }
    }

    /// <inheritdoc/>
    public Task OnProjectionDefinitionsChanged(ProjectionDefinition definition)
    {
        State = definition;
        _readModelDefinition = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ProjectionResult> GetModelInstance()
    {
        using var scope = logger.BeginImmediateProjectionScope(_projectionKey!.ProjectionId, _projectionKey!);

        logger.GettingModelInstance();

        try
        {
            if (State.ReadModel is null)
            {
                // A passive projection's definition is set lazily, from the first event of its kind observed
                // anywhere in the store, rather than eagerly at startup. Before that has happened, this is
                // indistinguishable from "no matching event for this key yet" - the documented null contract
                // for a read model that was never created - so it resolves the same way instead of throwing.
                logger.NoDefinitionSetYet();
                return ProjectionResult.Empty;
            }

            var readModelKey = new ReadModelGrainKey(State.ReadModel, _projectionKey.EventStore);
            var readModel = GrainFactory.GetGrain<IReadModel>(readModelKey);
            _readModelDefinition = await readModel.GetDefinition();
            var eventTypes = await _projection!.GetEventTypes();
            var modelKey = _projectionKey.ReadModelKey.IsSpecified ? (EventSourceId)_projectionKey.ReadModelKey.Value : null!;
            var streamScope = _projectionKey.StreamScope;
            using var cursor = await _eventSequenceStorage!.GetFromSequenceNumber(
                EventSequenceNumber.First,
                streamScope?.EventSourceId ?? modelKey,
                eventSourceType: streamScope?.EventSourceType,
                eventStreamType: streamScope?.EventStreamType,
                eventStreamId: streamScope?.EventStreamId,
                eventTypes: eventTypes);
            var projectedEventsCount = 0;
            var lastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
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

                lastHandledEventSequenceNumber = events[^1].Context.SequenceNumber;
            }

            if (!HasInitializedModel(state))
            {
                return ProjectionResult.Empty with
                {
                    ProjectedEventsCount = projectedEventsCount,
                    LastHandledEventSequenceNumber = lastHandledEventSequenceNumber
                };
            }

            var jsonObject = ReadModelSubjects.CopyToJson(state.Model, expandoObjectConverter.ToJsonObject(state.Model, _readModelDefinition!.GetSchemaForLatestGeneration()));
            return new(jsonObject, projectedEventsCount, lastHandledEventSequenceNumber);
        }
        catch (Exception ex)
        {
            logger.FailedGettingModelInstance(ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task Dehydrate()
    {
        DeactivateSession();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets this grain as a reference that can receive projection definition changes.
    /// </summary>
    /// <returns>The observer reference for this grain.</returns>
    protected virtual INotifyProjectionDefinitionsChanged GetSelfGrainReference() => this.AsReference<INotifyProjectionDefinitionsChanged>();

    static bool HasInitializedModel(SingleReadModelProjectionState state) => state.Exists && state.IsInitialized;

    /// <summary>
    /// Requests deactivation of this session after its current work completes.
    /// </summary>
    protected virtual void DeactivateSession() => DeactivateOnIdle();

    SingleReadModelProjectionState GetInitialState()
    {
        // Preserve the definition's eager seed and presence decision for a never-created instance.
        var model = State.InitialModelState is not null
            ? expandoObjectConverter.ToExpandoObject(State.InitialModelState, _readModelDefinition!.GetSchemaForLatestGeneration())
            : new ExpandoObject();
        var exists = ((IDictionary<string, object?>)model).Count > 0;
        return new(model, exists, exists);
    }

    async Task<(int ProjectedEventsCount, SingleReadModelProjectionState State, EventSequenceNumber Tail)> HandleEvents(SingleReadModelProjectionState initialState, AppendedEvent[] events)
    {
        var projectionKey = new ProjectionKey(_projectionKey!.ProjectionId, _projectionKey!.EventStore);
        var projectionGrain = GrainFactory.GetGrain<IProjection>(projectionKey);

        var eventsWithSequenceNumber = events.Where(_ => _.Context.SequenceNumber != EventSequenceNumber.Unavailable).ToArray();
        var eventSequenceNumber = eventsWithSequenceNumber.Length > 0
            ? eventsWithSequenceNumber[^1].Context.SequenceNumber
            : EventSequenceNumber.Unavailable;

        var state = await projectionGrain.FoldForSingleReadModel(_projectionKey!.Namespace, initialState, events);

        var projectedEventsCount = events.Count(_ => _ is not null);

        return (projectedEventsCount, state, eventSequenceNumber);
    }
}
