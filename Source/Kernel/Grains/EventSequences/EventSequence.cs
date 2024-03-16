// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Auditing;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.EventTypes;
using Cratis.Identities;
using Cratis.Json;
using Cratis.Kernel.Compliance;
using Cratis.Kernel.EventSequences;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Kernel.Storage.EventTypes;
using Cratis.Kernel.Storage.Identities;
using Cratis.Kernel.Storage.Observation;
using Cratis.Metrics;
using Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;
using IObserver = Cratis.Kernel.Grains.Observation.IObserver;

namespace Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventSequence"/>.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="meter">The meter to use for metrics.</param>
/// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
/// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSequences)]
public class EventSequence(
    IStorage storage,
    IMeter<EventSequence> meter,
    IExecutionContextManager executionContextManager,
    IJsonComplianceManager jsonComplianceManagerProvider,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<EventSequence> logger) : Grain<EventSequenceState>, IEventSequence
{
    IEventSequenceStorage? _eventSequenceStorage;
    IEventTypesStorage? _eventTypesStorage;
    IIdentityStorage? _identityStorage;
    IObserverStorage? _observerStorage;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    IAsyncStream<AppendedEvent>? _stream;
    IMeterScope<EventSequence>? _metrics;

    IEventSequenceStorage EventSequenceStorage => _eventSequenceStorage ??= storage.GetEventStore((string)_eventSequenceKey.MicroserviceId).GetNamespace(_eventSequenceKey.TenantId).GetEventSequence(_eventSequenceId);
    IEventTypesStorage EventTypesStorage => _eventTypesStorage ??= storage.GetEventStore((string)_eventSequenceKey.MicroserviceId).EventTypes;
    IIdentityStorage IdentityStorage => _identityStorage ??= storage.GetEventStore((string)_eventSequenceKey.MicroserviceId).Identities;
    IObserverStorage ObserverStorage => _observerStorage ??= storage.GetEventStore((string)_eventSequenceKey.MicroserviceId).GetNamespace(_eventSequenceKey.TenantId).Observers;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventSequenceId = this.GetPrimaryKey(out var key);
        _eventSequenceKey = (EventSequenceKey)key;
        var streamId = StreamId.Create(_eventSequenceKey, (Guid)_eventSequenceId);
        var streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        _stream = streamProvider.GetStream<AppendedEvent>(streamId);
        _metrics = meter.BeginEventSequenceScope(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId);

        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task Rehydrate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => Task.FromResult(State.SequenceNumber);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber() => Task.FromResult(State.SequenceNumber - 1);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForEventTypes(IEnumerable<EventType> eventTypes)
    {
        logger.GettingTailSequenceNumberForEventTypes(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, eventTypes);

        var sequenceNumber = EventSequenceNumber.Unavailable;
        try
        {
            sequenceNumber = State.TailSequenceNumberPerEventType
                        .Where(_ => eventTypes.Any(e => e.Id == _.Key) && _.Value != EventSequenceNumber.Unavailable)
                        .Select(_ => _.Value)
                        .OrderByDescending(_ => _)
                        .FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.FailedGettingTailSequenceNumberForEventTypes(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, eventTypes, ex);
        }

        sequenceNumber ??= EventSequenceNumber.Unavailable;
        logger.ResultForGettingTailSequenceNumberForEventTypes(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, eventTypes, sequenceNumber);
        return Task.FromResult(sequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var result = EventSequenceNumber.Unavailable;
        try
        {
            logger.GettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, sequenceNumber, eventTypes ?? Enumerable.Empty<EventType>());
            result = await EventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(sequenceNumber, eventTypes, eventSourceId);
        }
        catch (Exception ex)
        {
            logger.FailedGettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, sequenceNumber, eventTypes ?? Enumerable.Empty<EventType>(), ex);
        }

        logger.NextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, sequenceNumber, eventTypes ?? Enumerable.Empty<EventType>(), result);

        return result;
    }

    /// <inheritdoc/>
    public async Task Append(
        EventSourceId eventSourceId,
        EventType eventType,
        JsonObject content,
        IEnumerable<Causation> causation,
        Identity causedBy,
        DateTimeOffset? validFrom = default)
    {
        bool updateSequenceNumber;
        var eventName = "[N/A]";
        try
        {
            var eventSchema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);
            eventName = eventSchema.Schema.GetDisplayName();
            logger.Appending(
                _eventSequenceKey.MicroserviceId,
                _eventSequenceKey.TenantId,
                _eventSequenceId,
                eventType,
                eventName,
                eventSourceId,
                State.SequenceNumber);

            var compliantEvent = await jsonComplianceManagerProvider.Apply((string)_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, eventSchema.Schema, eventSourceId, content);

            var compliantEventAsExpandoObject = expandoObjectConverter.ToExpandoObject(compliantEvent, eventSchema.Schema);

            var appending = true;
            while (appending)
            {
                try
                {
                    var appendedEvent = new AppendedEvent(
                        new(State.SequenceNumber, eventType),
                        new(
                            eventSourceId,
                            State.SequenceNumber,
                            DateTimeOffset.UtcNow,
                            validFrom ?? DateTimeOffset.MinValue,
                            _eventSequenceKey.TenantId,
                            executionContextManager.Current.CorrelationId,
                            causation,
                            causedBy),
                        compliantEventAsExpandoObject);

                    await _stream!.OnNextAsync(appendedEvent, new EventSequenceNumberToken(State.SequenceNumber));

                    _metrics?.AppendedEvent(eventSourceId, eventName);

                    State.TailSequenceNumberPerEventType[eventType.Id] = State.SequenceNumber;

                    appending = false;
                }
                catch (DuplicateEventSequenceNumber)
                {
                    _metrics?.DuplicateEventSequenceNumber(eventSourceId, eventName);
                    State.SequenceNumber++;
                    await WriteStateAsync();
                }
            }
            updateSequenceNumber = true;
        }
        catch (UnableToAppendToEventSequence ex)
        {
            _metrics?.FailedAppending(eventSourceId, eventName);
            logger.FailedAppending(
                _eventSequenceKey.MicroserviceId,
                _eventSequenceKey.TenantId,
                eventType,
                ex.StreamId,
                ex.EventSourceId,
                ex.SequenceNumber,
                ex);

            throw;
        }
        catch (Exception ex)
        {
            logger.ErrorAppending(
                _eventSequenceKey.MicroserviceId,
                _eventSequenceKey.TenantId,
                _eventSequenceId,
                eventSourceId,
                State.SequenceNumber,
                ex);

            throw;
        }

        if (updateSequenceNumber)
        {
            State.SequenceNumber++;
            await WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public async Task AppendMany(
        IEnumerable<EventToAppend> events,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        foreach (var @event in events)
        {
            await Append(
                @event.EventSourceId,
                @event.EventType,
                @event.Content,
                causation,
                causedBy,
                @event.ValidFrom);
        }
    }

    /// <inheritdoc/>
    public Task Compensate(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        JsonObject content,
        IEnumerable<Causation> causation,
        Identity causedBy,
        DateTimeOffset? validFrom = default)
    {
        logger.Compensating(
            _eventSequenceKey.MicroserviceId,
            _eventSequenceKey.TenantId,
            eventType,
            _eventSequenceId,
            sequenceNumber);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        logger.Redacting(
            _eventSequenceKey.MicroserviceId,
            _eventSequenceKey.TenantId,
            _eventSequenceId,
            sequenceNumber);

        var affectedEvent = await EventSequenceStorage.Redact(
            sequenceNumber,
            reason,
            causation,
            await IdentityStorage.GetFor(causedBy),
            DateTimeOffset.UtcNow);
        await RewindPartitionForAffectedObservers(affectedEvent.Context.EventSourceId, new[] { affectedEvent.Metadata.Type });
    }

    /// <inheritdoc/>
    public async Task Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType> eventTypes,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        logger.RedactingMultiple(
            _eventSequenceKey.MicroserviceId,
            _eventSequenceKey.TenantId,
            _eventSequenceId,
            eventSourceId,
            eventTypes);

        var affectedEventTypes = await EventSequenceStorage.Redact(
            eventSourceId,
            reason,
            eventTypes,
            causation,
            await IdentityStorage.GetFor(causedBy),
            DateTimeOffset.UtcNow);
        await RewindPartitionForAffectedObservers(eventSourceId, affectedEventTypes);
    }

    async Task RewindPartitionForAffectedObservers(
        EventSourceId eventSourceId,
        IEnumerable<EventType> affectedEventTypes)
    {
        var observers = await ObserverStorage.GetObserversForEventTypes(affectedEventTypes);
        foreach (var observer in observers)
        {
            var key = new ObserverKey(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId);
            await GrainFactory.GetGrain<IObserver>(observer.ObserverId, key).ReplayPartition(eventSourceId);
        }
    }
}
