// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
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

    /// <inheritdoc/>
    public EventSequenceId Id => eventSequenceId;

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
        ConcurrencyScope? concurrencyScope = default)
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

        var eventType = eventTypes.GetEventTypeFor(eventClrType);
        var content = await eventSerializer.Serialize(@event);
        var causationChain = causationManager.GetCurrentChain().ToContract();
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
            ConcurrencyScope = concurrencyScope?.ToContract() ?? ConcurrencyScope.None.ToContract()
        });

        return ResolveViolationMessages(response.ToClient());
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
        ConcurrencyScope? concurrencyScope = default)
    {
        var eventsToAppend = events.Select(@event =>
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
                Tags = allTags
            };
        }).ToList();

        concurrencyScope ??= await concurrencyScopeStrategies
            .GetFor(this)
            .GetScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType);

        var concurrencyScopes = concurrencyScope != ConcurrencyScope.NotSet
            ? new Dictionary<EventSourceId, ConcurrencyScope> { { eventSourceId, concurrencyScope } }
            : new Dictionary<EventSourceId, ConcurrencyScope>();

        return await AppendManyImplementation(
            eventsToAppend,
            correlationId ?? correlationIdAccessor.Current,
            concurrencyScopes);
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default)
    {
        var eventsToAppend = events.Select(@event =>
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
                Tags = allTags
            };
        }).ToList();

        return await AppendManyImplementation(
            eventsToAppend,
            correlationId ?? correlationIdAccessor.Current,
            concurrencyScopes ?? new Dictionary<EventSourceId, ConcurrencyScope>());
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
        IEnumerable<EventType>? eventTypes = default)
    {
        var result = await _servicesAccessor.Services.EventSequences.GetEventsFromEventSequenceNumber(new()
        {
            EventStore = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            FromEventSequenceNumber = sequenceNumber,
            EventSourceId = eventSourceId?.Value ?? default,
            EventTypes = eventTypes?.ToContract() ?? []
        });

        return result.Events.ToClient(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(
        EventSourceId eventSourceId,
        IEnumerable<EventType> eventTypes,
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
            EventTypes = eventTypes.ToContract()
        });

        return result.Events.ToClient(jsonSerializerOptions);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default)
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
            EventTypes = eventTypes?.ToContract() ?? []
        };
        var sequenceNumber = await _servicesAccessor.Services.EventSequences.GetTailSequenceNumber(request);
        return sequenceNumber.SequenceNumber;
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason? reason = null) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason? reason = null, params Type[] eventTypes) => throw new NotImplementedException();

    static void ThrowIfUnknownEventType(IEventTypes eventTypes, Type eventClrType)
    {
        if (!eventTypes.HasFor(eventClrType))
        {
            throw new UnknownEventType(eventClrType);
        }
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
}
