// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.EventSequences.Placement;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Monads;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.BroadcastChannel;
using Orleans.Providers;
using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for accessing the underlying storage.</param>
/// <param name="constraintValidatorSetFactory"><see cref="IConstraintValidationFactory"/> for creating a set of constraint validators.</param>
/// <param name="eventTypeMigrations"><see cref="IEventTypeMigrations"/> for migrating events between generations.</param>
/// <param name="meter">The meter to use for metrics.</param>
/// <param name="activitySource">The <see cref="IActivitySource{T}"/> for tracing.</param>
/// <param name="jsonComplianceManagerProvider"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and deserializing events.</param>
/// <param name="eventHashCalculator"><see cref="IEventHashCalculator"/> for calculating event content hashes.</param>
/// <param name="options"><see cref="IOptions{T}"/> for <see cref="ChronicleOptions"/>.</param>
/// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSequences)]
[EventSequencePlacement]
public class EventSequence(
    IStorage storage,
    IConstraintValidationFactory constraintValidatorSetFactory,
    IEventTypeMigrations eventTypeMigrations,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<EventSequence> meter,
    [FromKeyedServices(WellKnown.MeterName)] IActivitySource<EventSequence> activitySource,
    IJsonComplianceManager jsonComplianceManagerProvider,
    IExpandoObjectConverter expandoObjectConverter,
    IEventSerializer eventSerializer,
    IEventHashCalculator eventHashCalculator,
    IOptions<ChronicleOptions> options,
    ILogger<EventSequence> logger) : Grain<EventSequenceState>, IEventSequence, IOnBroadcastChannelSubscribed
{
    IEventSequenceStorage? _eventSequenceStorage;
    IEventTypesStorage? _eventTypesStorage;
    IIdentityStorage? _identityStorage;
    IObserverDefinitionsStorage? _observerDefinitionsStorage;
    IClosedStreamsConstraintStorage? _closedStreamsStorage;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    EventSequenceKey _eventSequenceKey = EventSequenceKey.NotSet;
    IMeterScope<EventSequence>? _metrics;
    IAppendedEventsQueues? _appendedEventsQueues;
    IConstraintValidation? _constraints;
    IReadOnlyCollection<IConstraintDefinition> _knownConstraints = [];
    ConstraintsVersion _constraintsVersion = ConstraintsVersion.NotSet;
    int _statePersistenceInterval = 1;
    int _appendsSinceStateWrite;
    IEventSequenceStorage EventSequenceStorage => _eventSequenceStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).GetEventSequence(_eventSequenceId);
    IEventTypesStorage EventTypesStorage => _eventTypesStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).EventTypes;
    IIdentityStorage IdentityStorage => _identityStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).Identities;
    IObserverDefinitionsStorage ObserverStorage => _observerDefinitionsStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).Observers;
    IClosedStreamsConstraintStorage ClosedStreamsStorage => _closedStreamsStorage ??= storage.GetEventStore(_eventSequenceKey.EventStore).GetNamespace(_eventSequenceKey.Namespace).GetClosedStreamsConstraints(_eventSequenceId);
    ConcurrencyValidator ConcurrencyValidator => new(EventSequenceStorage);
    IConstraints ConstraintsGrain => GrainFactory.GetGrain<IConstraints>(new ConstraintsKey(_eventSequenceKey.EventStore));

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
        _knownConstraints = await ConstraintsGrain.GetDefinitions();
        _constraintsVersion = await ConstraintsGrain.GetVersion();
        _statePersistenceInterval = Math.Max(1, options.Value.Events.StatePersistenceInterval);

        await EventSequenceStorage.EnsureIndexes();
        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Flushes any state accumulated since the last periodic write so a subsequent activation can warm-start from it.
    /// Correctness does not depend on this write — <see cref="EventSequencesStorageProvider"/> rebuilds the state from
    /// the event tail on activation — it only lets the next activation skip re-deriving the per-event-type tails.
    /// </remarks>
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (_appendsSinceStateWrite > 0)
        {
            _appendsSinceStateWrite = 0;
            await WriteStateAsync();
        }

        await base.OnDeactivateAsync(reason, cancellationToken);
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
        ConcurrencyScope concurrencyScope,
        DateTimeOffset? occurred = null,
        Subject? subject = null)
    {
        try
        {
            await RefreshConstraintsIfChanged();
            var getValidAndCompliantEvent = await GetValidAndCompliantEvent(eventSourceType, eventSourceId, eventStreamType, eventStreamId, eventType, content, correlationId, subject);
            if (getValidAndCompliantEvent.TryGetError(out var error))
            {
                return error;
            }

            var (compliantEvent, compliantContent, constraintContext) = getValidAndCompliantEvent.AsT0;
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
                compliantContent,
                constraintContext,
                occurred,
                subject);
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
        using var span = activitySource.AppendMany();
        span?.Activity?.Tag(_eventSequenceKey.EventStore);
        span?.Activity?.Tag(_eventSequenceKey.Namespace);
        span?.Activity?.Tag(_eventSequenceKey.EventSequenceId);
        try
        {
            await RefreshConstraintsIfChanged();
            events = events as IList<EventToAppend> ?? events.ToList();

            // Validate sequentially with a shared set of batch claims so that two events in the same batch
            // cannot both claim the same unique constraint value — the persisted index is only updated after
            // the whole batch has been appended, so without this earlier events in the batch are invisible.
            var batchClaims = new ConstraintBatchClaims();
            var getValidAndCompliantEvents = new List<(EventToAppend Event, Result<(ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintValidationContext), AppendResult> Result)>();
            foreach (var e in events)
            {
                var result = await GetValidAndCompliantEvent(e.EventSourceType, e.EventSourceId, e.eventStreamType, e.eventStreamId, e.EventType, e.Content, correlationId, e.Subject, batchClaims);
                getValidAndCompliantEvents.Add((e, result));
            }

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

            var identity = await IdentityStorage.GetFor(causedBy.WithoutDuplicates());
            var eventsToAppend = new List<EventToAppendToStorage>();
            var constraintContexts = new List<ConstraintValidationContext>();

            foreach (var (eventToAppend, validAndCompliantEvent) in getValidAndCompliantEvents)
            {
                var (compliantEvent, _, constraintContext) = validAndCompliantEvent.AsT0;
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
                    eventToAppend.Occurred ?? DateTimeOffset.UtcNow,
                    compliantEvent,
                    eventHash,
                    eventToAppend.Subject));

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

            await PersistStateAfterAppends(appendedCount);
            await (_appendedEventsQueues?.Enqueue(appendedEventsList.ToList()) ?? Task.CompletedTask);

            foreach (var (constraintContext, eventToAppend) in constraintContexts.Zip(eventsToAppend))
            {
                await constraintContext.Update(eventToAppend.SequenceNumber);
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
    public async Task Revise(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy)
    {
        logger.Revising(
            _eventSequenceKey.EventStore,
            _eventSequenceKey.Namespace,
            eventType,
            _eventSequenceId,
            sequenceNumber);

        var @event = await EventSequenceStorage.GetEventAt(sequenceNumber);
        if (@event.Context.EventType.Id != eventType.Id)
        {
            throw new InvalidRevisionEventType(sequenceNumber, @event.Context.EventType.Id, eventType.Id);
        }

        var eventSchema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var contentAsExpandoObject = expandoObjectConverter.ToExpandoObject(content, eventSchema.Schema);
        var hash = eventHashCalculator.Calculate(eventType.Id, @event.Context.EventSourceId, contentAsExpandoObject);

        await EventSequenceStorage.Revise(
            sequenceNumber,
            eventType,
            correlationId,
            causation,
            await IdentityStorage.GetFor(causedBy.WithoutDuplicates()),
            DateTimeOffset.UtcNow,
            contentAsExpandoObject,
            hash);

        await RewindPartitionForAffectedObservers(@event.Context.EventSourceId, [@event.Context.EventType]);
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
            await IdentityStorage.GetFor(causedBy.WithoutDuplicates()),
            DateTimeOffset.UtcNow);

        // Storage returns the event with EventType.Id == Redaction (i.e. "EventRedacted") when
        // it short-circuits the mutation because the event was already redacted. Rewinding for
        // that synthetic type would replay observers subscribed to EventRedacted for a redaction
        // that already triggered its own rewind — causing duplicate EventRedacted notifications
        // when the same redaction is dispatched through more than one path (for example the
        // production Service.Redact + EventSequencesReactor path racing with an integration
        // fixture that calls EventSequence.Redact directly).
        if (affectedEvent.Context.EventType.Id == GlobalEventTypes.Redaction)
        {
            return;
        }

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
            await IdentityStorage.GetFor(causedBy.WithoutDuplicates()),
            DateTimeOffset.UtcNow);
        await RewindPartitionForAffectedObservers(eventSourceId, affectedEventTypes);
    }

    /// <inheritdoc/>
    public async Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId)
    {
        if (eventStreamType == EventStreamType.All && eventStreamId.Value == EventStreamId.Default)
        {
            return CompleteStreamError.DefaultStreamCannotBeCompleted;
        }

        if (await ClosedStreamsStorage.IsStreamClosed(eventStreamType, eventStreamId))
        {
            return CompleteStreamError.AlreadyCompleted;
        }

        await ClosedStreamsStorage.CloseStream(eventStreamType, eventStreamId);
        return State.SequenceNumber - 1;
    }

    /// <inheritdoc/>
    public Task<bool> IsStreamCompleted(EventStreamType eventStreamType, EventStreamId eventStreamId) =>
        ClosedStreamsStorage.IsStreamClosed(eventStreamType, eventStreamId);

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
        JsonObject compliantContent,
        ConstraintValidationContext constraintContext,
        DateTimeOffset? occurred = null,
        Subject? subject = null)
    {
        using var span = activitySource.Append();
        span?.Activity?.Tag(_eventSequenceKey.EventStore);
        span?.Activity?.Tag(_eventSequenceKey.Namespace);
        span?.Activity?.Tag(_eventSequenceKey.EventSequenceId);
        span?.Activity?.Tag(eventType);
        span?.Activity?.Tag(eventSourceType, eventSourceId);
        try
        {
            Result<AppendedEvent, DuplicateEventSequenceNumber>? appendResult = null;

            var identity = await IdentityStorage.GetFor(causedBy.WithoutDuplicates());

            // Migrate the event to all generations using the already-compliant content and expando
            var migratedContent = await eventTypeMigrations.MigrateToAllGenerations(_eventSequenceKey.EventStore, eventType, compliantContent, compliantEvent);

            // Calculate content hashes for each generation
            var contentHashes = migratedContent.ToDictionary(
                kvp => kvp.Key,
                kvp => eventHashCalculator.Calculate(eventType.Id, eventSourceId, kvp.Value));

            do
            {
                await HandleFailedAppendResult(appendResult, eventType, eventSourceId, eventType.Id);
                var eventOccurred = occurred ?? DateTimeOffset.UtcNow;
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
                    eventOccurred,
                    migratedContent,
                    contentHashes,
                    subject);
            }
            while (!appendResult.IsSuccess);

            var appendedSequenceNumber = State.SequenceNumber;
            State.SequenceNumber = appendedSequenceNumber.Next();
            State.TailSequenceNumberPerEventType[eventType.Id] = appendedSequenceNumber;
            await PersistStateAfterAppends(1);

            _metrics?.AppendedEvent(eventSourceId, eventType.Id);
            var appendedEvents = new[] { (AppendedEvent)appendResult }.ToList();
            await (_appendedEventsQueues?.Enqueue(appendedEvents) ?? Task.CompletedTask);
            await constraintContext.Update(appendedSequenceNumber);

            return AppendResult.Success(correlationId, appendedSequenceNumber);
        }
        catch (Exception ex)
        {
            return HandleAppendEventException(ex, eventSourceType, eventSourceId, eventType, eventStreamId, correlationId);
        }
    }

    async Task<Result<(ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintValidationContext), AppendResult>> GetValidAndCompliantEvent(
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        JsonObject content,
        CorrelationId correlationId,
        Subject? subject = null,
        ConstraintBatchClaims? batchClaims = default)
    {
        try
        {
            var (compliantEventAsExpandoObject, compliantContent, eventSchema) = await MakeEventCompliant(eventSourceId, eventType, content, subject);
            var schemaValidationResult = ValidateAgainstSchema(eventType, compliantContent, eventSchema, correlationId);
            if (schemaValidationResult.TryGetError(out var schemaError))
            {
                return schemaError;
            }

            // Constraint validation and index updates must operate on the ORIGINAL, pre-compliance content.
            // PII encryption is non-deterministic (a fresh data key and nonce per value), so hashing the
            // encrypted value would produce a different hash on every append — a [Unique] constraint on a
            // [PII] property would then never detect a collision and silently do nothing. Build a plaintext
            // expando from the original content for constraint purposes, while the encrypted expando is what
            // actually gets appended and persisted.
            var plaintextEventAsExpandoObject = expandoObjectConverter.ToExpandoObject(content, eventSchema.Schema);
            var checkConstraintViolation = await CheckConstraintViolation(eventSourceId, eventType, correlationId, plaintextEventAsExpandoObject, eventSourceType, eventStreamType, eventStreamId, batchClaims);
            if (checkConstraintViolation.TryGetError(out var error))
            {
                return error;
            }

            return (compliantEventAsExpandoObject, compliantContent, (ConstraintValidationContext)checkConstraintViolation);
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

    async Task<(ExpandoObject ExpandoObject, JsonObject CompliantContent, EventTypeSchema EventTypeSchema)> MakeEventCompliant(EventSourceId eventSourceId, EventType eventType, JsonObject content, Subject? subject = null)
    {
        var eventSchema = await EventTypesStorage.GetFor(eventType.Id, eventType.Generation);

        var complianceIdentifier = subject?.IsSet == true ? subject.Value : eventSourceId.Value;
        var compliantEvent = await jsonComplianceManagerProvider.Apply(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, eventSchema.Schema, complianceIdentifier, content);
        var expandoObject = expandoObjectConverter.ToExpandoObject(compliantEvent, eventSchema.Schema);
        return (expandoObject, compliantEvent, eventSchema);
    }

    async Task<Result<ConstraintValidationContext, AppendResult>> CheckConstraintViolation(
        EventSourceId eventSourceId,
        EventType eventType,
        CorrelationId correlationId,
        ExpandoObject compliantEventAsExpandoObject,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        ConstraintBatchClaims? batchClaims = default)
    {
        var constraintContext = _constraints!.Establish(eventSourceId, eventType.Id, compliantEventAsExpandoObject, eventSourceType, eventStreamType, eventStreamId, batchClaims);
        var constraintValidationResult = await constraintContext.Validate();
        if (constraintValidationResult.IsValid)
        {
            return constraintContext;
        }
        _metrics?.ConstraintViolation(eventSourceId, eventType.Id);
        return AppendResult.Failed(correlationId, constraintValidationResult.Violations);
    }

    Result<bool, AppendResult> ValidateAgainstSchema(
        EventType eventType,
        JsonObject content,
        EventTypeSchema eventSchema,
        CorrelationId correlationId)
    {
        var validationErrors = eventSchema.Schema.Validate(content);
        if (validationErrors.Count == 0)
        {
            return true;
        }

        var violations = validationErrors.Select(error =>
        {
            var details = new ConstraintViolationDetails
            {
                ["path"] = error.Path ?? string.Empty,
                ["kind"] = error.Kind.ToString()
            };

            return new ConstraintViolation(
                eventType.Id,
                EventSequenceNumber.Unavailable,
                ConstraintType.Schema,
                new ConstraintName("SchemaValidation"),
                new ConstraintViolationMessage(error.ToString()),
                details);
        }).ToList();

        return AppendResult.Failed(correlationId, violations);
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

    Task HandleAppendedDuplicateEvent(EventType eventType, EventSourceId eventSourceId, string eventName, EventSequenceNumber nextAvailableSequenceNumber)
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
        return Task.CompletedTask;
    }

    async Task HandleFailedAppendManyResult(
        Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>? appendResult,
        List<EventToAppendToStorage> eventsToAppend)
    {
        if (appendResult is null)
        {
            return;
        }

        await appendResult.Match(
            _ => Task.CompletedTask,
            errorType => HandleAppendedDuplicateEventForMany(eventsToAppend, errorType.NextAvailableSequenceNumber));
    }

    Task HandleAppendedDuplicateEventForMany(List<EventToAppendToStorage> eventsToAppend, EventSequenceNumber nextAvailableSequenceNumber)
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

        var sequenceNumber = nextAvailableSequenceNumber;
        for (var index = 0; index < eventsToAppend.Count; index++)
        {
            eventsToAppend[index] = eventsToAppend[index] with { SequenceNumber = sequenceNumber };
            sequenceNumber = sequenceNumber.Next();
        }

        State.SequenceNumber = sequenceNumber;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists the event sequence state as a warm-start snapshot once at least
    /// <see cref="Configuration.Events.StatePersistenceInterval"/> appends have accumulated since the last write,
    /// rather than on every append.
    /// </summary>
    /// <param name="appendedCount">Number of events appended by the current operation.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The persisted state is only an optimization: <see cref="EventSequencesStorageProvider"/> rebuilds
    /// <see cref="EventSequenceState.SequenceNumber"/> from the actual event tail — and the per-event-type tails via
    /// aggregation — on every activation, so a crash between these periodic writes loses no sequence-number
    /// correctness. The next append still gets the right number.
    /// </remarks>
    async Task PersistStateAfterAppends(int appendedCount)
    {
        _appendsSinceStateWrite += appendedCount;
        if (_appendsSinceStateWrite < _statePersistenceInterval)
        {
            return;
        }

        _appendsSinceStateWrite = 0;
        await WriteStateAsync();
    }

    async Task OnConstraintsChanged(ConstraintsChanged payload)
    {
        _constraints = await constraintValidatorSetFactory.Create(_eventSequenceKey);
        await StartReindexJob(payload.Changes.Where(_ => _.RequiresReindex).ToArray());
    }

    Task OnConstraintsChangedError(Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-reads the constraint validators when the constraints registered for the event store have changed since this
    /// grain last observed them, starting a reindex job for any unique constraints whose index must be rebuilt.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The <see cref="WellKnownBroadcastChannelNames.ConstraintsChanged"/> broadcast never reaches sequence grains
    /// (they are keyed differently to the constraints grain and are not implicit channel subscribers), so constraint
    /// changes are picked up here instead by a cheap <see cref="ConstraintsVersion"/> check on each append. The version
    /// is a content-derived stamp, so it is stable across constraints-grain deactivation and consistent across silos —
    /// the validators are only re-read, and a reindex only started, when the constraints genuinely changed.
    /// </remarks>
    async Task RefreshConstraintsIfChanged()
    {
        var version = await ConstraintsGrain.GetVersion();
        if (version == _constraintsVersion)
        {
            return;
        }

        var previous = _knownConstraints;
        var current = await ConstraintsGrain.GetDefinitions();
        _constraints = await constraintValidatorSetFactory.Create(_eventSequenceKey);
        _knownConstraints = current;
        _constraintsVersion = version;
        await StartReindexJob(ConstraintDefinitionComparison.GetReindexChanges(previous, current));
    }

    async Task StartReindexJob(IReadOnlyCollection<ConstraintDefinitionChange> changesRequiringReindex)
    {
        if (changesRequiringReindex.Count == 0)
        {
            return;
        }

        var jobsManager = GrainFactory.GetJobsManager(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace);
        await jobsManager.Start<IReindexConstraints, ReindexConstraintsRequest>(new(_eventSequenceId, changesRequiringReindex));
    }

    async Task RewindPartitionForAffectedObservers(
        EventSourceId eventSourceId,
        IEnumerable<EventType> affectedEventTypes)
    {
        var observers = await ObserverStorage.GetReplayableObserversForEventTypes(affectedEventTypes);
        foreach (var observer in observers)
        {
            var key = new ObserverKey(observer.Identifier, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId);
            await GrainFactory.GetGrain<IObserver>(key).ReplayPartition(eventSourceId);
        }
    }
}
