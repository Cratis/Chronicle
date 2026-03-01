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
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;
using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="constraintValidatorSetFactory"><see cref="IConstraintValidationFactory"/> for creating a set of constraint validators.</param>
/// <param name="meter">The meter to use for metrics.</param>
/// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and deserializing events.</param>
/// <param name="eventHashCalculator"><see cref="IEventHashCalculator"/> for calculating event content hashes.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSequences)]
public class EventSequence(
    IStorage storage,
    IConstraintValidationFactory constraintValidatorSetFactory,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<EventSequence> meter,
    IJsonComplianceManager jsonComplianceManagerProvider,
    IExpandoObjectConverter expandoObjectConverter,
    IEventSerializer eventSerializer,
    IEventHashCalculator eventHashCalculator,
    ILogger<EventSequence> logger) : Grain<EventSequenceState>, IEventSequence, IOnBroadcastChannelSubscribed
{
    IEventSequenceStorage? _eventSequenceStorage;
    IEventTypesStorage? _eventTypesStorage;
    IIdentityStorage? _identityStorage;
    IObserverDefinitionsStorage? _observerDefinitionsStorage;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    IMeterScope<EventSequence>? _metrics;
    IAppendedEventsQueues? _appendedEventsQueues;
    IConstraintValidation? _constraints;
    IEventSequenceStorage EventSequenceStorage => _eventSequenceStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).GetEventSequence(_eventSequenceId);
    IEventTypesStorage EventTypesStorage => _eventTypesStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).EventTypes;
    IIdentityStorage IdentityStorage => _identityStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).Identities;
    IObserverDefinitionsStorage ObserverStorage => _observerDefinitionsStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).Observers;
    ConcurrencyValidator ConcurrencyValidator => new(EventSequenceStorage);

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
        EventSourceId eventSourceId,
        object @event,
        CorrelationId? correlationId = null,
        IEnumerable<Causation>? causation = null,
        Identity? causedBy = null,
        IEnumerable<Tag>? tags = null,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null)
    {
        var content = eventSerializer.Serialize(@event);
        var eventType = @event.GetType().GetEventType();

        correlationId ??= CorrelationId.New();
        causation ??= [];
        tags ??= [];

        if (causedBy is null &&
            RequestContext.Get(WellKnownKeys.UserIdentity) is string userSubject && !string.IsNullOrEmpty(userSubject) &&
            RequestContext.Get(WellKnownKeys.UserName) is string userName && !string.IsNullOrEmpty(userName) &&
            RequestContext.Get(WellKnownKeys.UserPreferredUserName) is string userPreferredUserName && !string.IsNullOrEmpty(userPreferredUserName))
        {
            causedBy = new Identity(userSubject, userName, userPreferredUserName);
        }

        causedBy ??= Identity.System;

        return await Append(
            eventSourceType ?? EventSourceType.Default,
            eventSourceId,
            eventStreamType ?? EventStreamType.All,
            eventStreamId ?? EventStreamId.Default,
            eventType,
            content,
            correlationId,
            causation,
            causedBy,
            tags,
            ConcurrencyScope.None);
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
        Identity causedBy,
        IEnumerable<Tag> tags,
        ConcurrencyScope concurrencyScope)
    {
        try
        {
            var getValidAndCompliantEvent = await GetValidAndCompliantEvent(eventSourceType, eventSourceId, eventStreamId, eventType, content, correlationId);
            if (getValidAndCompliantEvent.TryGetError(out var error))
            {
                return error;
            }

            var (compliantEvent, constraintContext) = getValidAndCompliantEvent.AsT0;
            var maybeConcurrencyViolation = await ConcurrencyValidator.Validate(eventSourceId, concurrencyScope);
            if (maybeConcurrencyViolation.TryGetValue(out var concurrencyViolation))
            {
                return AppendResult.Failed(correlationId, concurrencyViolation);
            }

            return await AppendValidAndCompliantEvent(
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                eventType,
                correlationId,
                causation,
                causedBy,
                tags,
                compliantEvent,
                constraintContext);
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
        Identity causedBy,
        ConcurrencyScopes concurrencyScopes)
    {
        try
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

            var concurrencyViolations = await ConcurrencyValidator.Validate(concurrencyScopes);
            if (concurrencyViolations.Any())
            {
                return AppendManyResult.Failed(correlationId, concurrencyViolations);
            }

            var identity = await IdentityStorage.GetFor(causedBy);
            var eventsToAppend = new List<EventToAppendToStorage>();
            var constraintContexts = new List<ConstraintValidationContext>();

            foreach (var (eventToAppend, validAndCompliantEvent) in getValidAndCompliantEvents)
            {
                var (compliantEvent, constraintContext) = validAndCompliantEvent.AsT0;
                constraintContexts.Add(constraintContext);
                var eventHash = eventHashCalculator.Calculate(eventToAppend.EventType.Id, eventToAppend.EventSourceId, compliantEvent);

                eventsToAppend.Add(new EventToAppendToStorage(
                    State.SequenceNumber,
                    eventToAppend.EventSourceType,
                    eventToAppend.EventSourceId,
                    eventToAppend.eventStreamType,
                    eventToAppend.eventStreamId,
                    eventToAppend.EventType,
                    correlationId,
                    causation,
                    identity,
                    eventToAppend.Tags,
                    DateTimeOffset.UtcNow,
                    compliantEvent,
                    eventHash));

                State.SequenceNumber = State.SequenceNumber.Next();
            }

            Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>? appendResult = null;
            do
            {
                await HandleFailedAppendManyResult(appendResult, eventsToAppend);
                logger.AppendManyCallingStorage(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, eventsToAppend.Count);
                appendResult = await EventSequenceStorage.AppendMany(eventsToAppend);
            }
            while (!appendResult.IsSuccess);

            List<AppendedEvent> appendedEventsList = new();
            await appendResult.Match(
                success =>
                {
                    appendedEventsList = success.ToList();
                    return Task.CompletedTask;
                },
                _ => Task.CompletedTask);

            var appendedCount = appendedEventsList?.Count ?? 0;
            logger.AppendManyReceived(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, appendedCount);

            appendedEventsList ??= [];
            var sequenceNumbers = appendedEventsList.Select(e => e.Context.SequenceNumber).ToImmutableList();

            foreach (var appendedEvent in appendedEventsList)
            {
                State.TailSequenceNumberPerEventType[appendedEvent.Context.EventType.Id] = appendedEvent.Context.SequenceNumber;
                _metrics?.AppendedEvent(appendedEvent.Context.EventSourceId, appendedEvent.Context.EventType.Id);
            }

            await WriteStateAsync();
            await (_appendedEventsQueues?.Enqueue(appendedEventsList.ToList()) ?? Task.CompletedTask);

            foreach (var constraintContext in constraintContexts)
            {
                await constraintContext.Update(State.SequenceNumber);
            }

            return AppendManyResult.Success(correlationId, sequenceNumbers);
        }
        catch (Exception ex)
        {
            logger.ErrorAppendingMany(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, ex);
            return new AppendManyResult
            {
                CorrelationId = correlationId,
                Errors = [new AppendError(ex.Message)]
            };
        }
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
        await RewindPartitionForAffectedObservers(affectedEvent.Context.EventSourceId, [affectedEvent.Context.EventType]);
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
        IEnumerable<Tag> tags,
        ExpandoObject compliantEvent,
        ConstraintValidationContext constraintContext)
    {
        try
        {
            Result<AppendedEvent, DuplicateEventSequenceNumber>? appendResult = null;

            var identity = await IdentityStorage.GetFor(causedBy);
            var eventHash = eventHashCalculator.Calculate(eventType.Id, eventSourceId, compliantEvent);
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
                    tags,
                    occurred,
                    compliantEvent,
                    eventHash);
            }
            while (!appendResult.IsSuccess);

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

    AppendResult HandleAppendEventException(
        Exception ex,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventType eventType,
        EventStreamId eventStreamId,
        CorrelationId correlationId)
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

    async Task<Result<ConstraintValidationContext, AppendResult>> CheckConstraintViolation(
        EventSourceId eventSourceId,
        EventType eventType,
        CorrelationId correlationId,
        ExpandoObject compliantEventAsExpandoObject)
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

    async Task HandleFailedAppendResult(
        Result<AppendedEvent, DuplicateEventSequenceNumber>? appendResult,
        EventType eventType,
        EventSourceId eventSourceId,
        string eventName)
    {
        if (appendResult is null)
        {
            return;
        }

        await appendResult.Match(
            evt => Task.CompletedTask,
            errorType => errorType switch
            {
                DuplicateEventSequenceNumber duplicateError => HandleAppendedDuplicateEvent(eventType, eventSourceId, eventName, duplicateError.NextAvailableSequenceNumber),
                _ => Task.FromException(new FailedAppendingEvent())
            });
    }

    async Task HandleAppendedDuplicateEvent(EventType eventType, EventSourceId eventSourceId, string eventName, EventSequenceNumber nextAvailableSequenceNumber)
    {
        logger.DuplicateEvent(
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
            _eventSequenceId,
            eventType,
            eventSourceId,
            State.SequenceNumber);
        _metrics?.DuplicateEventSequenceNumber(eventSourceId, eventName);
        State.SequenceNumber = nextAvailableSequenceNumber;
        await WriteStateAsync();
    }

    async Task HandleFailedAppendManyResult(
        Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>? appendResult,
        IEnumerable<EventToAppendToStorage> eventsToAppend)
    {
        if (appendResult is null)
        {
            return;
        }

        await appendResult.Match(
            _ => Task.CompletedTask,
            errorType => HandleAppendedDuplicateEventForMany(eventsToAppend, errorType.NextAvailableSequenceNumber));
    }

    async Task HandleAppendedDuplicateEventForMany(IEnumerable<EventToAppendToStorage> eventsToAppend, EventSequenceNumber nextAvailableSequenceNumber)
    {
        logger.DuplicateEventInMany(
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
            _eventSequenceId,
            State.SequenceNumber);

        foreach (var eventToAppend in eventsToAppend)
        {
            _metrics?.DuplicateEventSequenceNumber(eventToAppend.EventSourceId, eventToAppend.EventType.Id.Value);
        }

        State.SequenceNumber = nextAvailableSequenceNumber;
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
            var key = new ObserverKey(observer.Identifier, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId);
            await GrainFactory.GetGrain<IObserver>(key).ReplayPartition(eventSourceId);
        }
    }
}
