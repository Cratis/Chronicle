// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for redacting every event of given types for one event source.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the events.</param>
/// <param name="EventSourceId">The event source whose events are redacted.</param>
/// <param name="Reason">Why the events are being redacted.</param>
/// <param name="EventTypes">The event types to redact. Empty redacts every type for the event source.</param>
[Command]
public record RedactMany(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    string Reason,
    IEnumerable<string> EventTypes)
{
    /// <summary>
    /// Handles the command by redacting every matching event for the event source.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequences"/> to redact through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is redacting.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(
        IEventSequences eventSequences,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor) =>
        eventSequences.RedactForEventSource(new RedactForEventSourceRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = EventSequenceId,
            EventSourceId = EventSourceId,
            Reason = Reason,
            EventTypes = [.. EventTypes.Select(eventType => new Contracts.Events.EventType { Id = eventType, Generation = 1 })],
            CorrelationId = Guid.NewGuid(),
            Causation = causation.GetCurrentChain(),
            CausedBy = principalAccessor.Current.ToContract()
        });
}
