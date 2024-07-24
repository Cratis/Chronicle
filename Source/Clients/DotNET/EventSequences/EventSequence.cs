// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
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
/// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Cratis Kernel.</param>
/// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
/// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
public class EventSequence(
    EventStoreName eventStoreName,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IChronicleConnection connection,
    IEventTypes eventTypes,
    IEventSerializer eventSerializer,
    ICausationManager causationManager,
    IIdentityProvider identityProvider) : IEventSequence
{
    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event)
    {
        var eventClrType = @event.GetType();

        ThrowIfUnknownEventType(eventTypes, eventClrType);

        var eventType = eventTypes.GetEventTypeFor(eventClrType);
        var content = await eventSerializer.Serialize(@event);
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        await connection.Services.EventSequences.Append(new()
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
    }

    /// <inheritdoc/>
    public async Task AppendMany(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        var eventsToAppend = events.Select(@event =>
        {
            var eventType = eventTypes.GetEventTypeFor(@event.GetType());
            return new Contracts.Events.EventToAppend
            {
                EventType = eventType.ToContract(),
                Content = eventSerializer.Serialize(@event).GetAwaiter().GetResult().ToString()
            };
        }).ToList();
        var causationChain = causationManager.GetCurrentChain().ToContract();
        var identity = identityProvider.GetCurrent();
        await connection.Services.EventSequences.AppendMany(new()
        {
            EventStoreName = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId,
            EventSourceId = eventSourceId,
            Events = eventsToAppend,
            Causation = causationChain,
            CausedBy = identity.ToContract()
        });
    }

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> eventTypes) => throw new NotImplementedException();

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
}
