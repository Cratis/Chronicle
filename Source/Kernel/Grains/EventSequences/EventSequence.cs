// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.EventTypes;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Kernel.Storage.Identities;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Metrics;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;
using IObserver = Aksio.Cratis.Kernel.Grains.Observation.IObserver;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSequences)]
public class EventSequence : Grain<EventSequenceState>, IEventSequence
{
    readonly IClusterStorage _clusterStorage;
    readonly IMeter<EventSequence> _meter;
    readonly IExecutionContextManager _executionContextManager;
    readonly IJsonComplianceManager _jsonComplianceManagerProvider;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<EventSequence> _logger;
    IEventSequenceStorage? _eventSequenceStorage;
    IEventTypesStorage? _eventTypesStorage;
    IIdentityStorage? _identityStorage;
    IObserverStorage? _observerStorage;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    IAsyncStream<AppendedEvent>? _stream;
    IMeterScope<EventSequence>? _metrics;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequence"/>.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing storage for the cluster.</param>
    /// <param name="meter">The meter to use for metrics.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public EventSequence(
        IClusterStorage clusterStorage,
        IMeter<EventSequence> meter,
        IExecutionContextManager executionContextManager,
        IJsonComplianceManager jsonComplianceManagerProvider,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<EventSequence> logger)
    {
        _clusterStorage = clusterStorage;
        _meter = meter;
        _executionContextManager = executionContextManager;
        _jsonComplianceManagerProvider = jsonComplianceManagerProvider;
        _expandoObjectConverter = expandoObjectConverter;
        _logger = logger;
    }

    IEventSequenceStorage EventSequenceStorage => _eventSequenceStorage ??= _clusterStorage.GetEventStore((string)_eventSequenceKey.MicroserviceId).GetInstance(_eventSequenceKey.TenantId).GetEventSequence(_eventSequenceId);
    IEventTypesStorage EventTypesStorage => _eventTypesStorage ??= _clusterStorage.GetEventStore((string)_eventSequenceKey.MicroserviceId).EventTypes;
    IIdentityStorage IdentityStorage => _identityStorage ??= _clusterStorage.GetEventStore((string)_eventSequenceKey.MicroserviceId).Identities;
    IObserverStorage ObserverStorage => _observerStorage ??= _clusterStorage.GetEventStore((string)_eventSequenceKey.MicroserviceId).GetInstance(_eventSequenceKey.TenantId).Observers;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventSequenceId = this.GetPrimaryKey(out var key);
        _eventSequenceKey = (EventSequenceKey)key;
        var streamId = StreamId.Create(_eventSequenceKey, (Guid)_eventSequenceId);
        var streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        _stream = streamProvider.GetStream<AppendedEvent>(streamId);
        _metrics = _meter.BeginEventSequenceScope(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId);

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
        _logger.GettingTailSequenceNumberForEventTypes(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, eventTypes);

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
            _logger.FailedGettingTailSequenceNumberForEventTypes(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, eventTypes, ex);
        }

        sequenceNumber ??= EventSequenceNumber.Unavailable;
        _logger.ResultForGettingTailSequenceNumberForEventTypes(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, eventTypes, sequenceNumber);
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
            _logger.GettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, sequenceNumber, eventTypes ?? Enumerable.Empty<EventType>());
            result = await EventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(sequenceNumber, eventTypes, eventSourceId);
        }
        catch (Exception ex)
        {
            _logger.FailedGettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, sequenceNumber, eventTypes ?? Enumerable.Empty<EventType>(), ex);
        }

        _logger.NextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId, sequenceNumber, eventTypes ?? Enumerable.Empty<EventType>(), result);

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
            _logger.Appending(
                _eventSequenceKey.MicroserviceId,
                _eventSequenceKey.TenantId,
                _eventSequenceId,
                eventType,
                eventName,
                eventSourceId,
                State.SequenceNumber);

            var compliantEvent = await _jsonComplianceManagerProvider.Apply(eventSchema.Schema, eventSourceId, content);

            var compliantEventAsExpandoObject = _expandoObjectConverter.ToExpandoObject(compliantEvent, eventSchema.Schema);

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
                            _executionContextManager.Current.CorrelationId,
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
            _logger.FailedAppending(
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
            _logger.ErrorAppending(
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
        _logger.Compensating(
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
        _logger.Redacting(
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
        _logger.RedactingMultiple(
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
