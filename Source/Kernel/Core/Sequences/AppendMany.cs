// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

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
[BelongsTo(WellKnownServices.EventSequences)]
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
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="AppendRejected">Thrown when the append is rejected by the kernel.</exception>
    internal async Task Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var events = Events.Select(@event => new EventSequences.EventToAppend(
            EventSourceType.Default,
            EventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            @event.EventType.ToChronicle(),
            [],
            @event.Content,
            Subject: string.IsNullOrWhiteSpace(@event.Subject) ? null : new Subject(@event.Subject)));

        var result = await eventSequence.AppendMany(
            events,
            Guid.NewGuid(),
            causation.GetCurrentChain(),
            principalAccessor.Current.ToIdentity(),
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

        AppendRejected.ThrowIfRejected(result.Errors, result.ConstraintViolations);
    }
}
