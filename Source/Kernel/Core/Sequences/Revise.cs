// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for revising the content of an already appended event.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the event.</param>
/// <param name="SequenceNumber">The sequence number of the event to revise.</param>
/// <param name="EventType">The type of the event being revised.</param>
/// <param name="Content">The revised content.</param>
/// <remarks>
/// A revision does not rewrite history - the original content is kept alongside the revision, so what the event
/// said before stays answerable.
/// </remarks>
[Command]
public record Revise(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    ulong SequenceNumber,
    EventType EventType,
    JsonObject Content)
{
    /// <summary>
    /// Handles the command by revising the event.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequences"/> to revise through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is revising.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(
        IEventSequences eventSequences,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor) =>
        eventSequences.Revise(new ReviseRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = EventSequenceId,
            SequenceNumber = SequenceNumber,
            EventType = EventType.ToContract(),
            Content = JsonSerializer.Serialize(Content),
            CorrelationId = Guid.NewGuid(),
            Causation = causation.GetCurrentChain(),
            CausedBy = principalAccessor.Current.ToContract()
        });
}
