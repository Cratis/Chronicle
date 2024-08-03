// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Events.Constraints;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using IObserver = Cratis.Chronicle.Grains.Observation.IObserver;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventSequence"/>.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="meter">The meter to use for metrics.</param>
/// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSequences)]
public class EventSequence(
    IStorage storage,
    IMeter<EventSequence> meter,
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
    IMeterScope<EventSequence>? _metrics;
    IAppendedEventsQueues? _appendedEventsQueues;
    IConstraints? _constraints;

    IEventSequenceStorage EventSequenceStorage => _eventSequenceStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).GetEventSequence(_eventSequenceId);
    IEventTypesStorage EventTypesStorage => _eventTypesStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).EventTypes;
    IIdentityStorage IdentityStorage => _identityStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).Identities;
    IObserverStorage ObserverStorage => _observerStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).Observers;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventSequenceKey = EventSequenceKey.Parse(this.GetPrimaryKeyString());
        _eventSequenceId = _eventSequenceKey.EventSequenceId;
        _metrics = meter.BeginEventSequenceScope(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace);

        var namespaces = GrainFactory.GetGrain<INamespaces>(_eventSequenceKey.EventStore);
        await @namespaces.Ensure(_eventSequenceKey.Namespace);

        _appendedEventsQueues = GrainFactory.GetGrain<IAppendedEventsQueues>(_eventSequenceKey);

        var constraintsKey = new ConstraintsKey(_eventSequenceKey.EventStore);
        _constraints = GrainFactory.GetGrain<IConstraints>(constraintsKey);

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
        logger.GettingTailSequenceNumberForEventTypes(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventTypes);

        var sequenceNumber = EventSequenceNumber.Unavailable;
        try
        {
            sequenceNumber = State.TailSequenceNumberPerEventType
                        .Where(_ => eventTypes.Any(e => e.Id == _.Key) && _.Value != EventSequenceNumber.Unavailable)
                        .Select(_ => _.Value)
                        .OrderDescending()
                        .FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.FailedGettingTailSequenceNumberForEventTypes(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventTypes, ex);
        }

        sequenceNumber ??= EventSequenceNumber.Unavailable;
        logger.ResultForGettingTailSequenceNumberForEventTypes(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventTypes, sequenceNumber);
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
            logger.GettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, sequenceNumber, eventTypes ?? []);
            result = await EventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(sequenceNumber, eventTypes, eventSourceId);
        }
        catch (Exception ex)
        {
            logger.FailedGettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, sequenceNumber, eventTypes ?? [], ex);
        }

        logger.NextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, sequenceNumber, eventTypes ?? [], result);

        return result;
    }

    /// <inheritdoc/>
    public async Task<AppendResult> Append(
        EventSourceId eventSourceId,
        EventType eventType,
        JsonObject content,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        bool updateSequenceNumber;
        var eventName = "[N/A]";
        try
        {
            _constraints?.Check(eventSourceId, eventType, content);

            var eventSchema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);
            eventName = eventSchema.Schema.GetDisplayName();
            logger.Appending(
                _eventSequenceKey.EventStore,
                _eventSequenceKey.Namespace,
                _eventSequenceId,
                eventType,
                eventName,
                eventSourceId,
                State.SequenceNumber);

            var compliantEvent = await jsonComplianceManagerProvider.Apply(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, eventSchema.Schema, eventSourceId, content);
            var compliantEventAsExpandoObject = expandoObjectConverter.ToExpandoObject(compliantEvent, eventSchema.Schema);

            var appending = true;
            while (appending)
            {
                try
                {
                    var occurred = DateTimeOffset.UtcNow;

                    _metrics?.AppendedEvent(eventSourceId, eventName);
                    var appendedEvent = await EventSequenceStorage.Append(
                        State.SequenceNumber,
                        eventSourceId,
                        eventType,
                        causation,
                        await IdentityStorage.GetFor(causedBy),
                        occurred,
                        compliantEventAsExpandoObject);

                    var appendedEvents = new[] { appendedEvent }.ToList();
                    await (_appendedEventsQueues?.Enqueue(appendedEvents) ?? Task.CompletedTask);

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
                _eventSequenceKey.EventStore,
                _eventSequenceKey.Namespace,
                eventType,
                ex.EventSequenceId,
                ex.EventSourceId,
                ex.SequenceNumber,
                ex);

            throw;
        }
        catch (Exception ex)
        {
            logger.ErrorAppending(
                _eventSequenceKey.EventStore,
                _eventSequenceKey.Namespace,
                _eventSequenceId,
                eventSourceId,
                State.SequenceNumber,
                ex);

            throw;
        }

        if (updateSequenceNumber)
        {
            var appendedSequenceNumber = State.SequenceNumber;
            State.SequenceNumber++;
            await WriteStateAsync();
            return AppendResult.Success(appendedSequenceNumber);
        }

        return AppendResult.Success(State.SequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<EventToAppend> events,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        foreach (var @event in events)
        {
            await Append(
                eventSourceId,
                @event.EventType,
                @event.Content,
                causation,
                causedBy);
        }

        return AppendManyResult.Success([]);
    }

    /// <inheritdoc/>
    public Task Compensate(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        JsonObject content,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        logger.Compensating(
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
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
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
            _eventSequenceId,
            sequenceNumber);

        var affectedEvent = await EventSequenceStorage.Redact(
            sequenceNumber,
            reason,
            causation,
            await IdentityStorage.GetFor(causedBy),
            DateTimeOffset.UtcNow);
        await RewindPartitionForAffectedObservers(affectedEvent.Context.EventSourceId, [affectedEvent.Metadata.Type]);
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
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
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
        var observers = await ObserverStorage.GetForEventTypes(affectedEventTypes);
        foreach (var observer in observers)
        {
            var key = new ObserverKey(observer.ObserverId, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId);
            await GrainFactory.GetGrain<IObserver>(key).ReplayPartition(eventSourceId);
        }
    }
}
