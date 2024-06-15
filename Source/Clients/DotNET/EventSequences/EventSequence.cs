// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Auditing;
using Cratis.Events;
using Cratis.Identities;

namespace Cratis.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequence"/> for gRPC.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequence"/> class.
/// </remarks>
/// <param name="eventStoreName">Name of the event store.</param>
/// <param name="namespace">Namespace for the event store.</param>
/// <param name="eventSequenceId">The identifier of the event sequence.</param>
/// <param name="connection"><see cref="ICratisConnection"/> for working with the connection to Cratis Kernel.</param>
/// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
/// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
public class EventSequence(
    EventStoreName eventStoreName,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    ICratisConnection connection,
    IEventTypes eventTypes,
    IEventSerializer eventSerializer,
    ICausationManager causationManager,
    IIdentityProvider identityProvider) : IEventSequence
{
    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var eventType = eventTypes.GetEventTypeFor(@event.GetType());
        var content = await eventSerializer.Serialize(@event);
        var causationChain = causationManager.GetCurrentChain().Select(_ => new Chronicle.Contracts.Auditing.Causation
        {
            Occurred = _.Occurred!,
            Type = _.Type,
            Properties = _.Properties
        });
        var identity = identityProvider.GetCurrent();
        await connection.Services.EventSequences.Append(new()
        {
            EventStoreName = eventStoreName,
            Namespace = @namespace,
            EventSequenceId = eventSequenceId.ToString(),
            EventSourceId = eventSourceId,
            EventType = new()
            {
                Id = eventType.Id.ToString(),
                Generation = eventType.Generation
            },
            Content = content.ToJsonString(),
            Causation = causationChain,
            Identity = identity.ToContract(),
            ValidFrom = validFrom
        });
    }

    /// <inheritdoc/>
    public Task AppendMany(EventSourceId eventSourceId, IEnumerable<object> events) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task AppendMany(EventSourceId eventSourceId, IEnumerable<EventAndValidFrom> events) => throw new NotImplementedException();

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
}
