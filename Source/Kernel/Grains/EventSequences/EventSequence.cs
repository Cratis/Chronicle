// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Engines.Compliance;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Schemas;
using Aksio.DependencyInversion;
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
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ProviderFor<IIdentityStore> _identityStoreProvider;
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;
    readonly IEventSequenceMetricsFactory _metricsFactory;
    readonly IExecutionContextManager _executionContextManager;
    readonly IJsonComplianceManager _jsonComplianceManagerProvider;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<EventSequence> _logger;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    IAsyncStream<AppendedEvent>? _stream;
    IEventSequenceMetrics? _metrics;
    long _appendedEventsCount;

    /// <summary>
    /// Initializes a new instance of <see cref="EventSequence"/>.
    /// </summary>
    /// <param name="schemaStoreProvider">Provider for <see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="identityStoreProvider">Provider for <see cref="IIdentityStore"/>.</param>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="metricsFactory">Factory for creating metrics.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public EventSequence(
        ProviderFor<ISchemaStore> schemaStoreProvider,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ProviderFor<IIdentityStore> identityStoreProvider,
        ProviderFor<IObserverStorage> observerStorageProvider,
        IEventSequenceMetricsFactory metricsFactory,
        IExecutionContextManager executionContextManager,
        IJsonComplianceManager jsonComplianceManagerProvider,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<EventSequence> logger)
    {
        _schemaStoreProvider = schemaStoreProvider;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _identityStoreProvider = identityStoreProvider;
        _observerStorageProvider = observerStorageProvider;
        _metricsFactory = metricsFactory;
        _executionContextManager = executionContextManager;
        _jsonComplianceManagerProvider = jsonComplianceManagerProvider;
        _expandoObjectConverter = expandoObjectConverter;
        _logger = logger;
    }

    IEventSequenceStorage EventSequenceStorage
    {
        get
        {
            // TODO: This is a temporary work-around till we fix #264 & #265
            _executionContextManager.Establish(
                _eventSequenceKey.TenantId,
                _executionContextManager.Current.CorrelationId,
                _eventSequenceKey.MicroserviceId);
            return _eventSequenceStorageProvider();
        }
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventSequenceId = this.GetPrimaryKey(out var key);
        _eventSequenceKey = (EventSequenceKey)key;
        var streamId = StreamId.Create(_eventSequenceKey, (Guid)_eventSequenceId);
        var streamProvider = this.GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        _stream = streamProvider.GetStream<AppendedEvent>(streamId);

        _appendedEventsCount = await EventSequenceStorage.GetCount(_eventSequenceId);

        _metrics = _metricsFactory.CreateFor(
            _eventSequenceId,
            _eventSequenceKey.MicroserviceId,
            _eventSequenceKey.TenantId,
            () => _appendedEventsCount);

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
        var sequenceNumber = State.TailSequenceNumberPerEventType
                    .Where(_ => eventTypes.Any(e => e.Id == _.Key) && _.Value != EventSequenceNumber.Unavailable)
                    .Select(_ => _.Value)
                    .OrderByDescending(_ => _)
                    .SingleOrDefault();

        if (sequenceNumber is null)
        {
            return Task.FromResult(EventSequenceNumber.Unavailable);
        }

        return Task.FromResult(sequenceNumber);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null) =>
        EventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(_eventSequenceId, sequenceNumber, eventTypes, eventSourceId);

    /// <inheritdoc/>
    public async Task Append(
        EventSourceId eventSourceId,
        EventType eventType,
        JsonObject content,
        IEnumerable<Causation> causation,
        Identity causedBy,
        DateTimeOffset? validFrom = default)
    {
        var updateSequenceNumber = false;
        var eventName = "[N/A]";
        try
        {
            var eventSchema = await _schemaStoreProvider().GetFor(eventType.Id, eventType.Generation);
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

                    State.TailSequenceNumberPerEventType[eventType.Id] = State.SequenceNumber;

                    _metrics?.AppendedEvent(eventSourceId, eventName);
                    _appendedEventsCount++;

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
            _eventSequenceId,
            sequenceNumber,
            reason,
            causation,
            await _identityStoreProvider().GetFor(causedBy),
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
            _eventSequenceId,
            eventSourceId,
            reason,
            eventTypes,
            causation,
            await _identityStoreProvider().GetFor(causedBy),
            DateTimeOffset.UtcNow);
        await RewindPartitionForAffectedObservers(eventSourceId, affectedEventTypes);
    }

    async Task RewindPartitionForAffectedObservers(
        EventSourceId eventSourceId,
        IEnumerable<EventType> affectedEventTypes)
    {
        _executionContextManager.Establish(_eventSequenceKey.TenantId, _executionContextManager.Current.CorrelationId, _eventSequenceKey.MicroserviceId);
        var observers = await _observerStorageProvider().GetObserversForEventTypes(affectedEventTypes);
        foreach (var observer in observers)
        {
            var key = new ObserverKey(_eventSequenceKey.MicroserviceId, _eventSequenceKey.TenantId, _eventSequenceId);
            await GrainFactory.GetGrain<IObserver>(observer.ObserverId, key).ReplayPartition(eventSourceId);
        }
    }
}
