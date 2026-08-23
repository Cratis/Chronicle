// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
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
/// <param name="CorrelationId">Optional correlation identifier. Defaults to a new one when not provided.</param>
/// <param name="Tags">The tags to associate with every event in the batch.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope to validate the append against. Defaults to no check when not provided.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendMany(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    IEnumerable<EventToAppend> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default,
    ConcurrencyScope? ConcurrencyScope = default)
{
    /// <summary>
    /// Handles the command by appending every event in one transaction.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is appending.</param>
    /// <returns>The <see cref="AppendManyResult"/> describing the outcome, successful or not.</returns>
    /// <remarks>
    /// A rejected append (constraint or concurrency violation) is a normal, expected outcome of appending - not an
    /// exceptional condition - so it is reported on the returned <see cref="AppendManyResult"/> rather than thrown.
    /// </remarks>
    internal Task<AppendManyResult> Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var eventSequence = grainFactory.GetEventSequence(EventSequenceId, EventStore, Namespace);
        var tags = (Tags ?? []).Select(tag => (Tag)tag).ToArray();
        var events = Events.Select(@event => new EventSequences.EventToAppend(
            EventSourceType.Default,
            EventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            @event.EventType.ToChronicle(),
            tags,
            @event.Content,
            Subject: string.IsNullOrWhiteSpace(@event.Subject) ? null : new Subject(@event.Subject)));

        var concurrencyScopes = new Concepts.EventSequences.Concurrency.ConcurrencyScopes(
            new Dictionary<EventSourceId, Concepts.EventSequences.Concurrency.ConcurrencyScope>
            {
                [EventSourceId] = ConcurrencyScope?.ToChronicle() ?? Concepts.EventSequences.Concurrency.ConcurrencyScope.None
            });

        return eventSequence.AppendMany(
            events,
            CorrelationId ?? Guid.NewGuid(),
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity(),
            concurrencyScopes);
    }
}
