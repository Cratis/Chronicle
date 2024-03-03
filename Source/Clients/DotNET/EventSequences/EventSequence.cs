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
public class EventSequence : IEventSequence
{
    readonly EventStoreName _eventStoreName;
    readonly EventStoreNamespaceName _namespace;
    readonly EventSequenceId _eventSequenceId;
    readonly ICratisConnection _connection;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequence"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="namespace">Namespace for the event store.</param>
    /// <param name="eventSequenceId">The identifier of the event sequence.</param>
    /// <param name="connection"><see cref="ICratisConnection"/> for working with the connection to Cratis Kernel.</param>
    /// <param name="eventTypes">Known <see cref="IEventTypes"/>.</param>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    public EventSequence(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        ICratisConnection connection,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        ICausationManager causationManager,
        IIdentityProvider identityProvider)
    {
        _eventStoreName = eventStoreName;
        _namespace = @namespace;
        _eventSequenceId = eventSequenceId;
        _connection = connection;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
    }

    /// <inheritdoc/>
    public async Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null)
    {
        var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
        var content = await _eventSerializer.Serialize(@event);
        var causationChain = _causationManager.GetCurrentChain().Select(_ => new Kernel.Contracts.Auditing.Causation
        {
            Occurred = _.Occurred!,
            Type = _.Type,
            Properties = _.Properties
        });
        var identity = _identityProvider.GetCurrent();
        await _connection.Services.EventSequences.Append(new()
        {
            EventStoreName = _eventStoreName,
            Namespace = _namespace,
            EventSequenceId = _eventSequenceId.ToString(),
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
