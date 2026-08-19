// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for redacting a single event.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the event.</param>
/// <param name="SequenceNumber">The sequence number of the event to redact.</param>
/// <param name="Reason">Why the event is being redacted.</param>
/// <remarks>
/// Redaction removes the payload, not the event - the sequence slot and its context stay, so nothing downstream
/// has to cope with a hole where an event used to be.
/// </remarks>
[Command]
public record Redact(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    ulong SequenceNumber,
    string Reason)
{
    /// <summary>
    /// Handles the command by redacting the event.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequences"/> to redact through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is redacting.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(
        IEventSequences eventSequences,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor) =>
        eventSequences.Redact(new RedactRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = EventSequenceId,
            SequenceNumber = SequenceNumber,
            Reason = Reason,
            CorrelationId = Guid.NewGuid(),
            Causation = causation.GetCurrentChain(),
            CausedBy = principalAccessor.Current.ToContract()
        });
}
