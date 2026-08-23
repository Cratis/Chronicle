// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Transactions;
using Cratis.Monads;
using Cratis.Traces;
using ContractCompleteStreamError = Cratis.Chronicle.Contracts.EventSequences.CompleteStreamError;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/> for gRPC.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequence"/> class.
/// </remarks>
/// <param name="eventStoreName">Name of the event store.</param>
/// <param name="namespace">Namespace for the event store.</param>
/// <param name="eventSequenceId">The identifier of the event sequence.</param>
/// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Chronicle.</param>
/// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
/// <param name="constraints">Known <see cref="IConstraints"/>.</param>
/// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> for getting correlation.</param>
/// <param name="concurrencyScopeStrategies"><see cref="IConcurrencyScopeStrategies"/> for managing concurrency scopes.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
/// <param name="unitOfWorkManager"><see cref="IUnitOfWorkManager"/> for working with the unit of work.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
/// <param name="jsonSerializerOptions">JSON serializer options to use.</param>
/// <param name="activitySource">Optional <see cref="IActivitySource{T}"/> for tracing. Defaults to a source named <see cref="ClientActivity.SourceName"/> when not provided.</param>
public class EventSequence(
    EventStoreName eventStoreName,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IChronicleConnection connection,
    IEventTypes eventTypes,
    IConstraints constraints,
    IEventSerializer eventSerializer,
    ICorrelationIdAccessor correlationIdAccessor,
    IConcurrencyScopeStrategies concurrencyScopeStrategies,
    ICausationManager causationManager,
    IUnitOfWorkManager unitOfWorkManager,
    IIdentityProvider identityProvider,
    JsonSerializerOptions jsonSerializerOptions,
    IActivitySource<EventSequence>? activitySource = null) : IEventSequence
{
    /// <summary>
    /// Gets the default <see cref="IActivitySource{T}"/> for Chronicle client event sequence traces.
    /// </summary>
    internal static readonly IActivitySource<EventSequence> DefaultActivitySource =
        new ActivitySource<EventSequence>(new System.Diagnostics.ActivitySource(ClientActivity.SourceName));

    readonly IChronicleServicesAccessor _servicesAccessor = (connection as IChronicleServicesAccessor)!;
    readonly IActivitySource<EventSequence> _activitySource = activitySource ?? DefaultActivitySource;

    IObservable<IEnumerable<AppendedEventWithResult>>? _appendOperations;
    event Action<IEnumerable<AppendedEventWithResult>>? _appendedEventsRaised;

    /// <inheritdoc/>
    public EventSequenceId Id => eventSequenceId;

    /// <inheritdoc/>
    public IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations =>
        _appendOperations ??= Observable.FromEvent<IEnumerable<AppendedEventWithResult>>(
            h => _appendedEventsRaised += h,
            h => _appendedEventsRaised -= h);

    /// <inheritdoc/>
    public ITransactionalEventSequence Transactional => new TransactionalEventSequence(this, unitOfWorkManager);

    /// <inheritdoc/>
    public async Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default)
    {
        using var span = _activitySource.Append(
            eventStoreName.Value,
            @namespace.Value,
            eventSequenceId.Value,
            (eventSourceType ?? EventSourceType.Default).Value,
            eventSourceId.Value);

        var eventClrType = @event.GetType();
        var resolvedEventStreamType = eventStreamType ?? EventStreamType.All;
        var resolvedEventStreamId = eventStreamId ?? EventStreamId.Default;
        var resolvedEventSourceType = eventSourceType ?? EventSourceType.Default;
        correlationId ??= correlationIdAccessor.Current;
        if (concurrencyScope is null || concurrencyScope == ConcurrencyScope.NotSet)
        {
            concurrencyScope = await concurrencyScopeStrategies
                .GetFor(this)
                .GetScope(eventSourceId, resolvedEventStreamType, resolvedEventStreamId, resolvedEventSourceType);
        }

        ThrowIfUnknownEventType(eventTypes, eventClrType);

        subject ??= SubjectResolver.ResolveFrom(@event);

        var eventType = eventTypes.GetEventTypeFor(eventClrType);
        var content = await eventSerializer.Serialize(@event);
        var causation = causationManager.GetCurrentChain();
        var identity = identityProvider.GetCurrent();

        // Merge static tags from the event type with dynamic tags
        var staticTags = eventClrType.GetTags();
        var allTags = staticTags.Concat(tags ?? []).Distinct().ToList();

        var response = await _servicesAccessor.Services.Sequences.Append(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceType = eventSourceType?.Value ?? string.Empty,
            EventSourceId = eventSourceId,
            EventStreamType = eventStreamType?.Value ?? string.Empty,
            EventStreamId = eventStreamId?.Value ?? string.Empty,
            CorrelationId = correlationId,
            EventType = eventType.ToSequencesContract(),
            Content = content,
            Causation = causation.ToSequencesContract(),
            CausedBy = identity.ToSequencesContract(),
            Tags = allTags,
            ConcurrencyScope = concurrencyScope.ToSequencesContract(),
            Occurred = ToWireOccurred(occurred),
            Subject = subject?.Value
        }).EnsureSuccess();

        var result = ResolveViolationMessages(response.ToClient()) with
        {
            EventStore = eventStoreName,
            EventStoreNamespace = @namespace,
            EventSequenceId = eventSequenceId,
            Observers = GetObservers()
        };
        if (_appendedEventsRaised is not null)
        {
            var context = EventContext.From(
                eventStoreName,
                @namespace,
                eventType,
                resolvedEventSourceType,
                eventSourceId,
                resolvedEventStreamType,
                resolvedEventStreamId,
                result.SequenceNumber,
                correlationId,
                occurred) with
            {
                Causation = causation,
                CausedBy = identity
            };
            _appendedEventsRaised([new AppendedEventWithResult(new AppendedEvent(context, @event), result)]);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default)
    {
        using var span = _activitySource.AppendMany(eventStoreName.Value, @namespace.Value, eventSequenceId.Value);

        var resolvedEventStreamType = eventStreamType ?? EventStreamType.All;
        var resolvedEventStreamId = eventStreamId ?? EventStreamId.Default;
        var resolvedEventSourceType = eventSourceType ?? EventSourceType.Default;
        var eventsList = events.ToList();

        if (concurrencyScope is null || concurrencyScope == ConcurrencyScope.NotSet)
        {
            concurrencyScope = await concurrencyScopeStrategies
                .GetFor(this)
                .GetScope(eventSourceId, resolvedEventStreamType, resolvedEventStreamId, resolvedEventSourceType);
        }

        // Merge static tags from every event type in the batch with dynamic tags. AppendManyRequest carries one
        // shared Tags field for the whole batch rather than a per-event one, so per-event-type static tags are
        // unioned across the batch rather than scoped to just the event that declared them.
        var staticTags = eventsList.SelectMany(_ => _.GetType().GetTags());
        var allTags = staticTags.Concat(tags ?? []).Distinct().ToList();

        var eventsToAppend = new List<Contracts.Sequences.EventToAppend>(eventsList.Count);
        foreach (var @event in eventsList)
        {
            var eventClrType = @event.GetType();
            ThrowIfUnknownEventType(eventTypes, eventClrType);

            var eventType = eventTypes.GetEventTypeFor(eventClrType);
            var content = await eventSerializer.Serialize(@event);
            eventsToAppend.Add(new Contracts.Sequences.EventToAppend
            {
                EventType = eventType.ToSequencesContract(),
                Content = content,
                Subject = (subject ?? SubjectResolver.ResolveFrom(@event))?.Value
            });
        }

        var resolvedCorrelationId = correlationId ?? correlationIdAccessor.Current;
        var causation = causationManager.GetCurrentChain();
        var identity = identityProvider.GetCurrent();

        var response = await _servicesAccessor.Services.Sequences.AppendMany(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            Events = eventsToAppend,
            CorrelationId = resolvedCorrelationId,
            Tags = allTags,
            Causation = causation.ToSequencesContract(),
            CausedBy = identity.ToSequencesContract(),
            ConcurrencyScope = concurrencyScope.ToSequencesContract()
        }).EnsureSuccess();

        var result = ResolveViolationMessages(response.ToClient()) with
        {
            EventStore = eventStoreName,
            EventStoreNamespace = @namespace,
            EventSequenceId = eventSequenceId,
            Observers = GetObservers()
        };
        NotifyAppendMany(
            eventsList,
            resolvedCorrelationId,
            eventSourceId,
            resolvedEventSourceType,
            resolvedEventStreamType,
            resolvedEventStreamId,
            causation,
            identity,
            result,
            occurred);
        return result;
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default)
    {
        using var span = _activitySource.AppendMany(eventStoreName.Value, @namespace.Value, eventSequenceId.Value);

        var eventsList = events.ToList();
        var eventsToAppend = new List<Contracts.Sequences.EventForEventSourceId>(eventsList.Count);
        IImmutableList<Causation>? causation = null;

        foreach (var @event in eventsList)
        {
            if (causation is null && @event.Causation is not null)
            {
                causation = [@event.Causation];
            }

            var eventClrType = @event.Event.GetType();
            ThrowIfUnknownEventType(eventTypes, eventClrType);
            var eventType = eventTypes.GetEventTypeFor(eventClrType);

            // Merge static tags from the event type with the event's own tags and the call-level dynamic tags
            var staticTags = eventClrType.GetTags();
            var allTags = staticTags.Concat(@event.Tags).Concat(tags ?? []).Distinct().ToList();

            eventsToAppend.Add(new Contracts.Sequences.EventForEventSourceId
            {
                EventSourceId = @event.EventSourceId,
                EventSourceType = @event.EventSourceType,
                EventStreamType = @event.EventStreamType,
                EventStreamId = @event.EventStreamId,
                EventType = eventType.ToSequencesContract(),
                Content = await eventSerializer.Serialize(@event.Event),
                Tags = allTags,
                Occurred = ToWireOccurred(@event.Occurred),
                Subject = (@event.Subject ?? SubjectResolver.ResolveFrom(@event.Event))?.Value
            });
        }

        causation ??= causationManager.GetCurrentChain();

        var resolvedCorrelationId = correlationId ?? correlationIdAccessor.Current;
        var resolvedConcurrencyScopes = await ResolveConcurrencyScopes(eventsList, concurrencyScopes);
        var identity = identityProvider.GetCurrent();

        var response = await _servicesAccessor.Services.Sequences.AppendManyForEventSources(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            Events = eventsToAppend,
            CorrelationId = resolvedCorrelationId,
            Causation = causation.ToSequencesContract(),
            CausedBy = identity.ToSequencesContract(),
            ConcurrencyScopes = resolvedConcurrencyScopes
                .Select(_ => new Contracts.Sequences.EventSourceConcurrencyScope
                {
                    EventSourceId = _.Key,
                    Scope = _.Value.ToSequencesContract()
                })
                .ToList()
        }).EnsureSuccess();

        var result = ResolveViolationMessages(response.ToClient()) with
        {
            EventStore = eventStoreName,
            EventStoreNamespace = @namespace,
            EventSequenceId = eventSequenceId,
            Observers = GetObservers()
        };

        if (_appendedEventsRaised is not null)
        {
            var sequenceNumbers = result.SequenceNumbers.ToList();
            var allResults = new List<AppendedEventWithResult>(eventsList.Count);

            for (var i = 0; i < eventsList.Count; i++)
            {
                var evt = eventsList[i];
                var eventClrType = evt.Event.GetType();
                var evtType = eventTypes.GetEventTypeFor(eventClrType);
                var sequenceNumber = result.IsSuccess && i < sequenceNumbers.Count
                    ? sequenceNumbers[i]
                    : EventSequenceNumber.Unavailable;

                var context = EventContext.From(
                    eventStoreName,
                    @namespace,
                    evtType,
                    evt.EventSourceType,
                    evt.EventSourceId,
                    evt.EventStreamType,
                    evt.EventStreamId,
                    sequenceNumber,
                    resolvedCorrelationId,
                    evt.Occurred) with
                {
                    Causation = causation,
                    CausedBy = identity
                };

                allResults.Add(new AppendedEventWithResult(new AppendedEvent(context, evt.Event), ToAppendResult(resolvedCorrelationId, sequenceNumber, result)));
            }

            _appendedEventsRaised(allResults);
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) =>
        _servicesAccessor.Services.Sequences.HasEventsForEventSourceId(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId
        }).EnsureSuccess();

    /// <inheritdoc/>
    public async Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? filterEventTypes = default)
    {
        var result = await _servicesAccessor.Services.Sequences.FromSequenceNumber(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            FromEventSequenceNumber = sequenceNumber,
            EventSourceId = eventSourceId?.Value,
            EventTypeIds = JoinEventTypeIds(filterEventTypes)
        }).EnsureSuccess();

        return result.ToClient(eventStoreName, @namespace, eventTypes, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(
        EventSourceId eventSourceId,
        IEnumerable<EventType> filterEventTypes,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default)
    {
        var result = await _servicesAccessor.Services.Sequences.ForEventSourceIdAndEventTypes(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            EventTypeIds = JoinEventTypeIds(filterEventTypes),
            EventStreamType = eventStreamType?.Value,
            EventStreamId = eventStreamId?.Value
        }).EnsureSuccess();

        return result.ToClient(eventStoreName, @namespace, eventTypes, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetNextSequenceNumber()
    {
        var tail = await GetTailSequenceNumber();
        return tail.IsUnavailable ? EventSequenceNumber.First : tail.Value + 1;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? filterEventTypes = default) =>
        await _servicesAccessor.Services.Sequences.TailSequenceNumber(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId?.Value,
            EventSourceType = eventSourceType?.Value,
            EventStreamType = eventStreamType?.Value,
            EventStreamId = eventStreamId?.Value,
            EventTypeIds = JoinEventTypeIds(filterEventTypes)
        }).EnsureSuccess();

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type)
    {
        var observerEventTypes = ReactorInvoker.GetEventTypesFor(eventTypes, type);
        return await GetTailSequenceNumber(filterEventTypes: observerEventTypes);
    }

    /// <inheritdoc/>
    public async Task Revise(EventSequenceNumber sequenceNumber, object @event)
    {
        var eventClrType = @event.GetType();
        ThrowIfUnknownEventType(eventTypes, eventClrType);

        var eventType = eventTypes.GetEventTypeFor(eventClrType);
        var content = await eventSerializer.Serialize(@event);
        var causation = causationManager.GetCurrentChain();
        var identity = identityProvider.GetCurrent();

        await _servicesAccessor.Services.Sequences.Revise(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            SequenceNumber = sequenceNumber,
            EventType = eventType.ToSequencesContract(),
            Content = content,
            Causation = causation.ToSequencesContract(),
            CausedBy = identity.ToSequencesContract()
        }).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason)
    {
        var causation = causationManager.GetCurrentChain();
        var identity = identityProvider.GetCurrent();
        await _servicesAccessor.Services.Sequences.Redact(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            SequenceNumber = sequenceNumber,
            Reason = reason,
            Causation = causation.ToSequencesContract(),
            CausedBy = identity.ToSequencesContract()
        }).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes)
    {
        var eventTypeIds = clrEventTypes.Select(t => eventTypes.GetEventTypeFor(t).Id.Value).ToList();
        var causation = causationManager.GetCurrentChain();
        var identity = identityProvider.GetCurrent();
        await _servicesAccessor.Services.Sequences.RedactForEventSource(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            Reason = reason,
            EventTypes = eventTypeIds,
            Causation = causation.ToSequencesContract(),
            CausedBy = identity.ToSequencesContract()
        }).EnsureSuccess();
    }

    /// <inheritdoc/>
    public async Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId)
    {
        // Deliberately still on the old EventSequences service - see CompleteStream.cs on the kernel side for why
        // mirroring its error enum is a separate, more carefully verified piece of work than this migration.
        var response = await _servicesAccessor.Services.EventSequences.CompleteStream(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventStreamType = eventStreamType,
            EventStreamId = eventStreamId
        });

        if (response.IsSuccess)
        {
            return (EventSequenceNumber)response.SequenceNumber;
        }

        return response.Error switch
        {
            ContractCompleteStreamError.DefaultStreamCannotBeCompleted => CompleteStreamError.DefaultStreamCannotBeCompleted,
            _ => CompleteStreamError.AlreadyCompleted
        };
    }

    static void ThrowIfUnknownEventType(IEventTypes eventTypes, Type eventClrType)
    {
        if (!eventTypes.HasFor(eventClrType))
        {
            throw new UnknownEventType(eventClrType);
        }
    }

    static string? JoinEventTypeIds(IEnumerable<EventType>? filterEventTypes) =>
        filterEventTypes is null ? null : string.Join(',', filterEventTypes.Select(_ => _.Id.Value));

    /// <summary>
    /// Converts a nullable occurred time to its wire representation.
    /// </summary>
    /// <param name="occurred">The occurred time, or <see langword="null"/> when not specified.</param>
    /// <returns>The <see cref="Contracts.Primitives.SerializableDateTimeOffset"/> to send.</returns>
    /// <remarks>
    /// <see cref="Contracts.Primitives.SerializableDateTimeOffset"/>'s own nullable-accepting conversion produces a
    /// null reference for a null input, but the wire field is declared non-nullable - protobuf-net represents
    /// "absent" as a default-constructed instance (an empty <c>Value</c> string), not a missing message. Coalescing
    /// to <see cref="DateTimeOffset.MinValue"/> before converting would pick the non-nullable operator, but that
    /// operator serializes the value as a real (wrong) timestamp rather than the empty string the server's own
    /// SerializableDateTimeOffset-to-DateTimeOffset? conversion recognizes as "not specified".
    /// </remarks>
    static Contracts.Primitives.SerializableDateTimeOffset ToWireOccurred(DateTimeOffset? occurred) =>
        (Contracts.Primitives.SerializableDateTimeOffset?)occurred ?? new Contracts.Primitives.SerializableDateTimeOffset();

    AppendResult ToAppendResult(CorrelationId correlationId, EventSequenceNumber sequenceNumber, AppendManyResult batchResult)
    {
        if (batchResult.IsSuccess)
        {
            return AppendResult.Success(correlationId, sequenceNumber) with
            {
                EventStore = eventStoreName,
                EventStoreNamespace = @namespace,
                EventSequenceId = eventSequenceId,
                ConcurrencyCheckPerformed = batchResult.ConcurrencyCheckPerformed,
                Observers = GetObservers()
            };
        }

        return new AppendResult
        {
            CorrelationId = correlationId,
            EventStore = eventStoreName,
            EventStoreNamespace = @namespace,
            EventSequenceId = eventSequenceId,
            ConstraintViolations = batchResult.ConstraintViolations,
            ConcurrencyViolation = batchResult.ConcurrencyViolations.FirstOrDefault(),
            Errors = batchResult.Errors,
            ConcurrencyCheckPerformed = batchResult.ConcurrencyCheckPerformed,
            Observers = GetObservers()
        };
    }

    /// <summary>
    /// Gets the observer service to carry on the append result, or <see langword="null"/> when the connection has none.
    /// </summary>
    /// <returns>The <see cref="Contracts.Observation.IObservers"/>, or <see langword="null"/> when the connection has no observer surface.</returns>
    /// <remarks>
    /// The in-process testing surfaces connect through a services implementation that has no observer surface and
    /// throws <see cref="NotSupportedException"/> for it, while appending itself must keep working there. The absence
    /// is therefore carried on the result rather than thrown here - and
    /// <see cref="Observation.AppendResultWaitForCompletionExtensions.WaitForCompletion"/> turns it into a
    /// <see cref="Observation.CannotWaitForObserverCompletion"/> at the point where it actually matters. A
    /// <see langword="null"/> here means "no observer surface exists", never "no observers were affected".
    /// </remarks>
    Contracts.Observation.IObservers? GetObservers()
    {
        try
        {
            return _servicesAccessor.Services.Observers;
        }
        catch (NotSupportedException)
        {
            // Deferred, not swallowed: waiting for completion fails by name instead of reporting success.
            return null;
        }
    }

    async Task<Dictionary<EventSourceId, ConcurrencyScope>> ResolveConcurrencyScopes(
        IEnumerable<EventForEventSourceId> events,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes)
    {
        var resolvedConcurrencyScopes = concurrencyScopes?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? [];
        var strategy = concurrencyScopeStrategies.GetFor(this);

        foreach (var eventsForEventSource in events.GroupBy(_ => _.EventSourceId))
        {
            if (resolvedConcurrencyScopes.TryGetValue(eventsForEventSource.Key, out var concurrencyScope) &&
                concurrencyScope != ConcurrencyScope.NotSet)
            {
                continue;
            }

            var firstEvent = eventsForEventSource.First();
            resolvedConcurrencyScopes[eventsForEventSource.Key] = await strategy.GetScope(
                firstEvent.EventSourceId,
                firstEvent.EventStreamType,
                firstEvent.EventStreamId,
                firstEvent.EventSourceType);
        }

        return resolvedConcurrencyScopes;
    }

    AppendResult ResolveViolationMessages(AppendResult result) => result with { ConstraintViolations = ResolveViolationMessages(result.ConstraintViolations) };
    AppendManyResult ResolveViolationMessages(AppendManyResult result) => result with { ConstraintViolations = ResolveViolationMessages(result.ConstraintViolations) };
    ImmutableList<ConstraintViolation> ResolveViolationMessages(IEnumerable<ConstraintViolation> violations) => violations.Select(constraints.ResolveMessageFor).ToImmutableList();

    void NotifyAppendMany(
        List<object> events,
        CorrelationId correlationId,
        EventSourceId eventSourceId,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        IImmutableList<Causation> causation,
        Identity identity,
        AppendManyResult result,
        DateTimeOffset? occurred)
    {
        var sequenceNumbers = result.SequenceNumbers.ToList();
        var results = new List<AppendedEventWithResult>(events.Count);

        if (_appendedEventsRaised is null) return;

        for (var i = 0; i < events.Count; i++)
        {
            var eventClrType = events[i].GetType();
            var evtType = eventTypes.GetEventTypeFor(eventClrType);
            var sequenceNumber = result.IsSuccess && i < sequenceNumbers.Count
                ? sequenceNumbers[i]
                : EventSequenceNumber.Unavailable;

            var context = EventContext.From(
                eventStoreName,
                @namespace,
                evtType,
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                sequenceNumber,
                correlationId,
                occurred) with
            {
                Causation = causation,
                CausedBy = identity
            };

            results.Add(new AppendedEventWithResult(new AppendedEvent(context, events[i]), ToAppendResult(correlationId, sequenceNumber, result)));
        }

        _appendedEventsRaised(results);
    }
}
