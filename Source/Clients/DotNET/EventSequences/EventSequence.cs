// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Transactions;

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
    JsonSerializerOptions jsonSerializerOptions) : IEventSequence
{
    readonly IChronicleServicesAccessor _servicesAccessor = (connection as IChronicleServicesAccessor)!;

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
        var eventClrType = @event.GetType();
        eventStreamType ??= EventStreamType.All;
        eventStreamId ??= EventStreamId.Default;
        eventSourceType ??= EventSourceType.Default;
        correlationId ??= correlationIdAccessor.Current;
        concurrencyScope ??= await concurrencyScopeStrategies
            .GetFor(this)
            .GetScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType);

        concurrencyScope = concurrencyScope != ConcurrencyScope.NotSet
            ? concurrencyScope
            : default;

        ThrowIfUnknownEventType(eventTypes, eventClrType);

        subject ??= SubjectResolver.ResolveFrom(@event);

        var eventType = eventTypes.GetEventTypeFor(eventClrType);
        var content = await eventSerializer.Serialize(@event);
        var causation = causationManager.GetCurrentChain();
        var causationChain = causation.ToContract();
        var identity = identityProvider.GetCurrent();

        // Merge static tags from the event type with dynamic tags
        var staticTags = eventClrType.GetTags();
        var allTags = staticTags.Concat(tags ?? []).Distinct().ToList();

        var response = await _servicesAccessor.Services.EventSequences.Append(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceType = eventSourceType,
            EventSourceId = eventSourceId,
            EventStreamType = eventStreamType,
            EventStreamId = eventStreamId,
            CorrelationId = correlationId,
            EventType = new()
            {
                Id = eventType.Id,
                Generation = eventType.Generation
            },
            Content = content.ToJsonString(),
            Causation = causationChain,
            CausedBy = identity.ToContract(),
            Tags = allTags,
            ConcurrencyScope = concurrencyScope?.ToContract() ?? ConcurrencyScope.None.ToContract(),
            Occurred = occurred,
            Subject = subject?.Value
        });

        var result = ResolveViolationMessages(response.ToClient());
        if (_appendedEventsRaised is not null)
        {
            var context = EventContext.From(
                eventStoreName,
                @namespace,
                eventType,
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
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
        DateTimeOffset? occurred = default)
    {
        var eventsList = events.ToList();
        var eventsToAppend = eventsList.ConvertAll(@event =>
        {
            var eventClrType = @event.GetType();
            var eventType = eventTypes.GetEventTypeFor(eventClrType);

            // Merge static tags from the event type with dynamic tags
            var staticTags = eventClrType.GetTags();
            var allTags = staticTags.Concat(tags ?? []).Distinct().ToList();

            return new Contracts.Events.EventToAppend
            {
                EventSourceType = eventSourceType ?? EventSourceType.Default,
                EventSourceId = eventSourceId,
                EventStreamType = eventStreamType ?? EventStreamType.All,
                EventStreamId = eventStreamId ?? EventStreamId.Default,
                EventType = eventType.ToContract(),
                Content = eventSerializer.Serialize(@event).GetAwaiter().GetResult().ToString(),
                Tags = allTags,
                Occurred = occurred
            };
        });

        concurrencyScope ??= await concurrencyScopeStrategies
            .GetFor(this)
            .GetScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType);

        var concurrencyScopes = concurrencyScope != ConcurrencyScope.NotSet
            ? new Dictionary<EventSourceId, ConcurrencyScope> { { eventSourceId, concurrencyScope } }
            : new Dictionary<EventSourceId, ConcurrencyScope>();

        var resolvedCorrelationId = correlationId ?? correlationIdAccessor.Current;
        var causation = causationManager.GetCurrentChain();
        var identity = identityProvider.GetCurrent();
        var result = await AppendManyImplementation(eventsToAppend, resolvedCorrelationId, concurrencyScopes);
        NotifyAppendMany(
            eventsList,
            resolvedCorrelationId,
            eventSourceId,
            eventSourceType ?? EventSourceType.Default,
            eventStreamType ?? EventStreamType.All,
            eventStreamId ?? EventStreamId.Default,
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
        var eventsList = events.ToList();
        var eventsToAppend = eventsList.ConvertAll(@event =>
        {
            var eventClrType = @event.Event.GetType();
            var eventType = eventTypes.GetEventTypeFor(eventClrType);

            // Merge static tags from the event type with dynamic tags
            var staticTags = eventClrType.GetTags();
            var allTags = staticTags.Concat(tags ?? []).Distinct().ToList();

            return new Contracts.Events.EventToAppend
            {
                EventSourceType = @event.EventSourceType,
                EventSourceId = @event.EventSourceId,
                EventStreamType = @event.EventStreamType,
                EventStreamId = @event.EventStreamId,
                EventType = eventType.ToContract(),
                Content = eventSerializer.Serialize(@event.Event).GetAwaiter().GetResult().ToString(),
                Tags = allTags,
                Occurred = @event.Occurred
            };
        });

        var causation = causationManager.GetCurrentChain();
        var resolvedCorrelationId = correlationId ?? correlationIdAccessor.Current;
        var result = await AppendManyImplementation(eventsToAppend, resolvedCorrelationId, concurrencyScopes ?? new Dictionary<EventSourceId, ConcurrencyScope>());

        if (_appendedEventsRaised is not null)
        {
            var identity = identityProvider.GetCurrent();
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
    public async Task<bool> HasEventsFor(EventSourceId eventSourceId)
    {
        var result = await _servicesAccessor.Services.EventSequences.HasEventsForEventSourceId(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId
        });

        return result.HasEvents;
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? filterEventTypes = default)
    {
        var result = await _servicesAccessor.Services.EventSequences.GetEventsFromEventSequenceNumber(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            FromEventSequenceNumber = sequenceNumber,
            EventSourceId = eventSourceId?.Value ?? default,
            EventTypes = filterEventTypes?.ToContract() ?? []
        });

        return result.Events.ToClient(eventTypes, jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(
        EventSourceId eventSourceId,
        IEnumerable<EventType> filterEventTypes,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default)
    {
        var result = await _servicesAccessor.Services.EventSequences.GetForEventSourceIdAndEventTypes(new()
        {
            EventStore = eventStoreName,
            EventStreamType = eventStreamType ?? EventStreamType.All,
            EventStreamId = eventStreamId ?? EventStreamId.Default,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceType = eventSourceType ?? EventSourceType.Default,
            EventSourceId = eventSourceId,
            EventTypes = filterEventTypes.ToContract()
        });

        return result.Events.ToClient(eventTypes, jsonSerializerOptions);
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
        IEnumerable<EventType>? filterEventTypes = default)
    {
        var request = new GetTailSequenceNumberRequest
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId?.Value ?? default,
            EventSourceType = eventSourceType?.Value ?? default,
            EventStreamType = eventStreamType?.Value ?? default,
            EventStreamId = eventStreamId?.Value ?? default,
            EventTypes = filterEventTypes?.ToContract() ?? []
        };
        var sequenceNumber = await _servicesAccessor.Services.EventSequences.GetTailSequenceNumber(request);
        return sequenceNumber.SequenceNumber;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type)
    {
        var observerEventTypes = ReactorInvoker.GetEventTypesFor(eventTypes, type);
        return await GetTailSequenceNumber(filterEventTypes: observerEventTypes);
    }

    /// <inheritdoc/>
    public async Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason)
    {
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        await _servicesAccessor.Services.EventSequences.Redact(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            SequenceNumber = sequenceNumber,
            Reason = reason,
            CorrelationId = correlationIdAccessor.Current,
            Causation = causationChain,
            CausedBy = identity.ToContract()
        });
    }

    /// <inheritdoc/>
    public async Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes)
    {
        var eventTypeContracts = clrEventTypes.Select(t => eventTypes.GetEventTypeFor(t).ToContract()).ToList();
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        await _servicesAccessor.Services.EventSequences.RedactForEventSource(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            Reason = reason,
            EventTypes = eventTypeContracts,
            CorrelationId = correlationIdAccessor.Current,
            Causation = causationChain,
            CausedBy = identity.ToContract()
        });
    }

    static void ThrowIfUnknownEventType(IEventTypes eventTypes, Type eventClrType)
    {
        if (!eventTypes.HasFor(eventClrType))
        {
            throw new UnknownEventType(eventClrType);
        }
    }

    static AppendResult ToAppendResult(CorrelationId correlationId, EventSequenceNumber sequenceNumber, AppendManyResult batchResult)
    {
        if (batchResult.IsSuccess)
        {
            return AppendResult.Success(correlationId, sequenceNumber);
        }

        return new AppendResult
        {
            CorrelationId = correlationId,
            ConstraintViolations = batchResult.ConstraintViolations,
            ConcurrencyViolation = batchResult.ConcurrencyViolations.FirstOrDefault(),
            Errors = batchResult.Errors
        };
    }

    async Task<AppendManyResult> AppendManyImplementation(IList<Contracts.Events.EventToAppend> eventsToAppend, CorrelationId correlationId, IDictionary<EventSourceId, ConcurrencyScope> concurrencyScopes)
    {
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        var request = new AppendManyRequest()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            CorrelationId = correlationId,
            Events = eventsToAppend,
            Causation = causationChain,
            CausedBy = identity.ToContract(),
            ConcurrencyScopes = concurrencyScopes
                .Where(kvp => kvp.Value is not null)
                .ToDictionary(_ => _.Key.Value, _ => _.Value.ToContract())
        };
        var response = await _servicesAccessor.Services.EventSequences.AppendMany(request);

        return ResolveViolationMessages(response.ToClient());
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
