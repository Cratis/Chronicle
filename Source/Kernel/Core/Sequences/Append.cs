// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending a single event to an event sequence.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="EventSourceId">The event source the event belongs to.</param>
/// <param name="EventSourceType">The type of event source.</param>
/// <param name="EventStreamType">The stream type within the event source.</param>
/// <param name="EventStreamId">The stream within the stream type.</param>
/// <param name="EventType">The type of event being appended.</param>
/// <param name="Content">The content of the event.</param>
[Command]
public record Append(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    string EventSourceType,
    string EventStreamType,
    string EventStreamId,
    EventType EventType,
    JsonObject Content)
{
    /// <summary>
    /// Handles the command by appending the event.
    /// </summary>
    /// <param name="eventSequences">The <see cref="IEventSequences"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="AppendRejected">Thrown when the append is rejected by the kernel.</exception>
    internal async Task Handle(
        IEventSequences eventSequences,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var response = await eventSequences.Append(new AppendRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = EventSequenceId,
            CorrelationId = Guid.NewGuid(),
            EventSourceId = EventSourceId,
            EventSourceType = EventSourceType,
            EventStreamType = EventStreamType,
            EventStreamId = EventStreamId,
            EventType = EventType.ToContract(),
            Content = JsonSerializer.Serialize(Content),
            Causation = causation.GetCurrentChain(),
            CausedBy = principalAccessor.Current.ToContract(),
            Tags = [],
            ConcurrencyScope = new Contracts.EventSequences.Concurrency.ConcurrencyScope
            {
                SequenceNumber = ulong.MaxValue,
                EventSourceId = false,
                EventStreamType = null,
                EventStreamId = null,
                EventSourceType = null,
                EventTypes = null
            }
        });

        AppendRejected.ThrowIfRejected(response.Errors, response.ConstraintViolations);
    }
}
