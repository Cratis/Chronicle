// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Identities;

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
/// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
public class EventSequence(
    EventStoreName eventStoreName,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IChronicleConnection connection,
    IEventTypes eventTypes,
    IConstraints constraints,
    IEventSerializer eventSerializer,
    ICausationManager causationManager,
    IIdentityProvider identityProvider) : IEventSequence
{
    /// <inheritdoc/>
    public EventSequenceId Id => eventSequenceId;

    /// <inheritdoc/>
    public async Task<AppendResult> Append(EventSourceId eventSourceId, object @event)
    {
        var eventClrType = @event.GetType();

        ThrowIfUnknownEventType(eventTypes, eventClrType);

        var eventType = eventTypes.GetEventTypeFor(eventClrType);
        var content = await eventSerializer.Serialize(@event);
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        var response = await connection.Services.EventSequences.Append(new()
        {
            EventStoreName = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            EventType = new()
            {
                Id = eventType.Id,
                Generation = eventType.Generation
            },
            Content = content.ToJsonString(),
            Causation = causationChain,
            CausedBy = identity.ToContract()
        });

        return ResolveViolationMessages(response.ToClient());
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        var eventsToAppend = events.Select(@event =>
        {
            var eventType = eventTypes.GetEventTypeFor(@event.GetType());
            return new Contracts.Events.EventToAppend
            {
                EventSourceId = eventSourceId,
                EventType = eventType.ToContract(),
                Content = eventSerializer.Serialize(@event).GetAwaiter().GetResult().ToString()
            };
        }).ToList();

        return await AppendManyImplementation(eventsToAppend);
    }

    /// <inheritdoc/>
    public async Task<AppendManyResult> AppendMany(IEnumerable<EventForEventSourceId> events)
    {
        var eventsToAppend = events.Select(@event =>
        {
            var eventType = eventTypes.GetEventTypeFor(@event.GetType());
            return new Contracts.Events.EventToAppend
            {
                EventSourceId = @event.EventSourceId,
                EventType = eventType.ToContract(),
                Content = eventSerializer.Serialize(@event).GetAwaiter().GetResult().ToString()
            };
        }).ToList();

        return await AppendManyImplementation(eventsToAppend);
    }

    /// <inheritdoc/>
    public async Task<bool> HasEventsFor(EventSourceId eventSourceId)
    {
        var result = await connection.Services.EventSequences.HasEventsForEventSourceId(new()
        {
            EventStoreName = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId
        });

        return result.HasEvents;
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> eventTypes)
    {
        var result = await connection.Services.EventSequences.GetForEventSourceIdAndEventTypes(new()
        {
            EventStoreName = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            EventTypes = eventTypes.ToContract()
        });

        return result.Events.ToClient();
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason? reason = null) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason? reason = null, params Type[] eventTypes) => throw new NotImplementedException();

    void ThrowIfUnknownEventType(IEventTypes eventTypes, Type eventClrType)
    {
        if (!eventTypes.HasFor(eventClrType))
        {
            throw new UnknownEventType(eventClrType);
        }
    }

    async Task<AppendManyResult> AppendManyImplementation(IList<Contracts.Events.EventToAppend> eventsToAppend)
    {
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        var response = await connection.Services.EventSequences.AppendMany(new()
        {
            EventStoreName = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            Events = eventsToAppend,
            Causation = causationChain,
            CausedBy = identity.ToContract()
        });

        return ResolveViolationMessages(response.ToClient());
    }

    AppendResult ResolveViolationMessages(AppendResult result) => result with { ConstraintViolations = ResolveViolationMessages(result.ConstraintViolations) };
    AppendManyResult ResolveViolationMessages(AppendManyResult result) => result with { ConstraintViolations = ResolveViolationMessages(result.ConstraintViolations) };
    IImmutableList<ConstraintViolation> ResolveViolationMessages(IEnumerable<ConstraintViolation> violations) =>
        violations.Select(constraints.ResolveMessageFor).ToImmutableList();

}
