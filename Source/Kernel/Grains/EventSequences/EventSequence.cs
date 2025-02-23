// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
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
using Orleans.BroadcastChannel;
using Orleans.Providers;
using IObserver = Cratis.Chronicle.Grains.Observation.IObserver;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="constraintValidatorSetFactory"><see cref="IConstraintValidationFactory"/> for creating a set of constraint validators.</param>
/// <param name="meter">The meter to use for metrics.</param>
/// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSequences)]
public class EventSequence(
    IStorage storage,
    IConstraintValidationFactory constraintValidatorSetFactory,
    IMeter<EventSequence> meter,
    IJsonComplianceManager jsonComplianceManagerProvider,
    IExpandoObjectConverter expandoObjectConverter,
    ILogger<EventSequence> logger) : Grain<EventSequenceState>, IEventSequence, IOnBroadcastChannelSubscribed
{
    IEventSequenceStorage? _eventSequenceStorage;
    IEventTypesStorage? _eventTypesStorage;
    IIdentityStorage? _identityStorage;
    IObserverStorage? _observerStorage;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    IMeterScope<EventSequence>? _metrics;
    IAppendedEventsQueues? _appendedEventsQueues;
    IConstraintValidation? _constraints;
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

        _constraints = await constraintValidatorSetFactory.Create(_eventSequenceKey);

        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<ConstraintsChanged>(OnConstraintsChanged, OnConstraintsChangedError);
        return Task.CompletedTask;
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
    public async Task<Result<EventSequenceNumber, GetSequenceNumberError>> GetNextSequenceNumberGreaterOrEqualTo(
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        try
        {
            logger.GettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, sequenceNumber, eventTypes ?? []);
            var result = await EventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(sequenceNumber, eventTypes, eventSourceId);
            logger.NextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, sequenceNumber, eventTypes ?? [], result);
            return result == EventSequenceNumber.Unavailable ? GetSequenceNumberError.NotFound : result;
        }
        catch (Exception ex)
        {
            logger.FailedGettingNextSequenceNumberGreaterOrEqualThan(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, sequenceNumber, eventTypes ?? [], ex);
            return GetSequenceNumberError.StorageError;
        }
    }

    /// <inheritdoc/>
    public async Task<AppendResult> Append(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        try
        {
            var getValidAndCompliantEvent = await GetValidAndCompliantEvent(eventSourceType, eventSourceId, eventStreamId, eventType, content, correlationId);
            if (getValidAndCompliantEvent.TryGetError(out var error))
            {
                return error;
            }

            var (compliantEvent, constraintContext) = getValidAndCompliantEvent.AsT0;

            return await AppendValidAndCompliantEvent(eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, correlationId, causation, causedBy, compliantEvent, constraintContext);
        }
        catch (Exception ex)
        {
            return HandleAppendEventException(ex, eventSourceType, eventSourceId, eventType, eventStreamId, correlationId);
        }
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        var tasks = events.Select(async e =>
        {
            var result = await GetValidAndCompliantEvent(e.EventSourceType, e.EventSourceId, e.eventStreamId, e.EventType, e.Content, correlationId);
            return (Event: e, Result: result);
        });

        var getValidAndCompliantEvents = await Task.WhenAll(tasks);
        var failedEvents = getValidAndCompliantEvents.Where(eventAndResult => !eventAndResult.Result.IsSuccess).ToList();

        if (failedEvents.Count != 0)
        {
            return new()
            {
                CorrelationId = correlationId,
                ConstraintViolations = failedEvents.SelectMany(r => r.Result.AsT1.ConstraintViolations).ToImmutableList(),
                Errors = failedEvents.SelectMany(r => r.Result.AsT1.Errors).ToImmutableList(),
            };
        }

        var results = new List<AppendResult>();
        foreach (var (eventToAppend, validAndCompliantEvent) in getValidAndCompliantEvents)
        {
            var (compliantEvent, constraintContext) = validAndCompliantEvent.AsT0;
            var appendResult = await AppendValidAndCompliantEvent(
                eventToAppend.EventSourceType,
                eventToAppend.EventSourceId,
                eventToAppend.eventStreamType,
                eventToAppend.eventStreamId,
                eventToAppend.EventType,
                correlationId,
                causation,
                causedBy,
                compliantEvent,
                constraintContext);
            results.Add(appendResult);
            if (appendResult.IsSuccess)
            {
                continue;
            }

            logger.FailedDuringAppendingManyEvents();
            break;
        }

        return new()
        {
            CorrelationId = correlationId,
            SequenceNumbers = results.Select(r => r.SequenceNumber).ToImmutableList(),
            ConstraintViolations = results.SelectMany(r => r.ConstraintViolations).ToImmutableList(),
            Errors = results.SelectMany(r => r.Errors).ToImmutableList()
        };
    }

    /// <inheritdoc/>
    public Task Compensate(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
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
        CorrelationId correlationId,
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
            correlationId,
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
        CorrelationId correlationId,
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
            correlationId,
            causation,
            await IdentityStorage.GetFor(causedBy),
            DateTimeOffset.UtcNow);
        await RewindPartitionForAffectedObservers(eventSourceId, affectedEventTypes);
    }

    async Task<AppendResult> AppendValidAndCompliantEvent(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ExpandoObject compliantEvent,
        ConstraintValidationContext constraintContext)
    {
        try
        {
            Result<AppendedEvent, AppendEventError>? appendResult = null;

            var identity = await IdentityStorage.GetFor(causedBy);
            do
            {
                await HandleFailedAppendResult(appendResult, eventType, eventSourceId, eventType.Id);
                var occurred = DateTimeOffset.UtcNow;
                logger.Appending(
                    _eventSequenceKey.EventStore,
                    _eventSequenceKey.Namespace,
                    _eventSequenceId,
                    eventType,
                    eventSourceId,
                    State.SequenceNumber);

                appendResult = await EventSequenceStorage.Append(
                    State.SequenceNumber,
                    eventSourceType,
                    eventSourceId,
                    eventStreamType,
                    eventStreamId,
                    eventType,
                    correlationId,
                    causation,
                    identity,
                    occurred,
                    compliantEvent);
            }
            while(!appendResult.IsSuccess);

            var appendedSequenceNumber = State.SequenceNumber;
            State.SequenceNumber = appendedSequenceNumber.Next();
            State.TailSequenceNumberPerEventType[eventType.Id] = appendedSequenceNumber;
            await WriteStateAsync();

            _metrics?.AppendedEvent(eventSourceId, eventType.Id);
            var appendedEvents = new[] { (AppendedEvent)appendResult }.ToList();
            await (_appendedEventsQueues?.Enqueue(appendedEvents) ?? Task.CompletedTask);
            await constraintContext.Update(State.SequenceNumber);

            return AppendResult.Success(correlationId, appendedSequenceNumber);
        }
        catch (Exception ex)
        {
            return HandleAppendEventException(ex, eventSourceType, eventSourceId, eventType, eventStreamId, correlationId);
        }
    }

    async Task<Result<(ExpandoObject CompliantEvent, ConstraintValidationContext ConstraintValidationContext), AppendResult>> GetValidAndCompliantEvent(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId)
    {
        try
        {
            var compliantEventAsExpandoObject = await MakeEventCompliant(eventSourceId, eventType, content);
            var checkConstraintViolation = await CheckConstraintViolation(eventSourceId, eventType, correlationId, compliantEventAsExpandoObject);
            if (checkConstraintViolation.TryGetError(out var error))
            {
                return error;
            }

            return (compliantEventAsExpandoObject, (ConstraintValidationContext)checkConstraintViolation);
        }
        catch (Exception ex)
        {
            return HandleAppendEventException(ex, eventSourceType, eventSourceId, eventType, eventStreamId, correlationId);
        }
    }

    AppendResult HandleAppendEventException(Exception ex, EventSourceType eventSourceType, EventSourceId eventSourceId, EventType eventType, EventStreamId eventStreamId, CorrelationId correlationId)
    {
        _metrics?.FailedAppending(eventSourceId, eventType.Id);
        logger.ErrorAppending(
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
            eventType,
            eventStreamId.Value,
            eventSourceType,
            eventSourceId,
            State.SequenceNumber,
            ex);

        return AppendResult.Failed(correlationId, [ex.Message]);
    }

    async Task<ExpandoObject> MakeEventCompliant(EventSourceId eventSourceId, EventType eventType, JsonObject content)
    {
        var eventSchema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);

        var compliantEvent = await jsonComplianceManagerProvider.Apply(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, eventSchema.Schema, eventSourceId, content);
        return expandoObjectConverter.ToExpandoObject(compliantEvent, eventSchema.Schema);
    }

    async Task<Result<ConstraintValidationContext, AppendResult>> CheckConstraintViolation(EventSourceId eventSourceId, EventType eventType, CorrelationId correlationId, ExpandoObject compliantEventAsExpandoObject)
    {
        var constraintContext = _constraints!.Establish(eventSourceId, eventType.Id, compliantEventAsExpandoObject);
        var constraintValidationResult = await constraintContext.Validate();
        if (constraintValidationResult.IsValid)
        {
            return constraintContext;
        }
        _metrics?.ConstraintViolation(eventSourceId, eventType.Id);
        return AppendResult.Failed(correlationId, constraintValidationResult.Violations);
    }

    async Task HandleFailedAppendResult(Result<AppendedEvent, AppendEventError>? appendResult, EventType eventType, EventSourceId eventSourceId, string eventName)
    {
        if (appendResult is null)
        {
            return;
        }

        await appendResult.Match(
            evt => Task.CompletedTask,
            errorType => errorType switch
            {
                AppendEventError.DuplicateEventSequenceNumber => HandleAppendedDuplicateEvent(eventType, eventSourceId, eventName),
                _ => Task.FromException(new UnknownAppendEventErrorType(errorType))
            });
    }

    async Task HandleAppendedDuplicateEvent(EventType eventType, EventSourceId eventSourceId, string eventName)
    {
        logger.DuplicateEvent(
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
            _eventSequenceId,
            eventType,
            eventSourceId,
            State.SequenceNumber);
        _metrics?.DuplicateEventSequenceNumber(eventSourceId, eventName);
        State.SequenceNumber = (await EventSequenceStorage.GetTailSequenceNumber() ).Next();
        await WriteStateAsync();
    }

    async Task OnConstraintsChanged(ConstraintsChanged payload)
    {
        _constraints = await constraintValidatorSetFactory.Create(_eventSequenceKey);
    }

    Task OnConstraintsChangedError(Exception exception)
    {
        return Task.CompletedTask;
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
