// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Contracts.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending several events for one event source in a single transaction.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="EventSourceId">The event source every event belongs to.</param>
/// <param name="Events">The events to append.</param>
[Command]
public record AppendMany(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    IEnumerable<EventToAppend> Events)
{
    /// <summary>
    /// Handles the command by appending every event in one transaction.
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
        var response = await eventSequences.AppendMany(new AppendManyRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            EventSequenceId = EventSequenceId,
            CorrelationId = Guid.NewGuid(),
            Events = [.. Events.Select(@event => new Contracts.Events.EventToAppend
            {
                EventSourceType = string.Empty,
                EventSourceId = EventSourceId,
                EventStreamType = string.Empty,
                EventStreamId = string.Empty,
                EventType = @event.EventType.ToContract(),
                Content = JsonSerializer.Serialize(@event.Content),
                Tags = [],
                Subject = @event.Subject
            })],
            Causation = causation.GetCurrentChain(),
            CausedBy = principalAccessor.Current.ToContract(),
            ConcurrencyScopes = new Dictionary<string, Contracts.EventSequences.Concurrency.ConcurrencyScope>()
        });

        AppendRejected.ThrowIfRejected(response.Errors, response.ConstraintViolations);
    }
}
