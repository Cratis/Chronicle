// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for appending several events for several event sources in a single transaction.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence to append to.</param>
/// <param name="Events">The events to append, each carrying its own event source.</param>
/// <param name="CorrelationId">Optional correlation identifier. Defaults to a new one when not provided.</param>
/// <param name="Tags">The tags to associate with every event in the batch, in addition to each event's own tags.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
/// <remarks>
/// Deliberately has no concurrency scope parameter yet - see the <c>Append</c> command's remarks for why
/// mirroring <see cref="Concepts.EventSequences.Concurrency.ConcurrencyScope"/> is a separate, more carefully
/// verified piece of work than this parity pass.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record AppendManyForEventSources(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    IEnumerable<EventForEventSourceId> Events,
    Guid? CorrelationId = default,
    IEnumerable<string>? Tags = default,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
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
        var globalTags = (Tags ?? []).Select(tag => (Tag)tag).ToArray();
        var eventsList = Events.ToList();
        var events = eventsList.Select(@event => new EventSequences.EventToAppend(
            (EventSourceType)@event.EventSourceType,
            @event.EventSourceId,
            (EventStreamType)@event.EventStreamType,
            (EventStreamId)@event.EventStreamId,
            @event.EventType.ToChronicle(),
            (@event.Tags ?? []).Select(tag => (Tag)tag).Concat(globalTags).Distinct(),
            @event.Content,
            @event.Occurred,
            Subject: string.IsNullOrWhiteSpace(@event.Subject) ? null : new Subject(@event.Subject)));

        var concurrencyScopes = new Concepts.EventSequences.Concurrency.ConcurrencyScopes(
            eventsList
                .Select(@event => (EventSourceId)@event.EventSourceId)
                .Distinct()
                .ToDictionary(eventSourceId => eventSourceId, _ => Concepts.EventSequences.Concurrency.ConcurrencyScope.None));

        return eventSequence.AppendMany(
            events,
            CorrelationId ?? Guid.NewGuid(),
            Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity(),
            concurrencyScopes);
    }
}
