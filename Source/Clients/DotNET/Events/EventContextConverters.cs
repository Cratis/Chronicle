// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Converter methods for <see cref="EventContext"/>.
/// </summary>
internal static class EventContextConverters
{
    /// <summary>
    /// Convert to contract version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Events.EventContext"/>.</returns>
    internal static Contracts.Events.EventContext ToContract(this EventContext context) => new()
    {
        EventType = context.EventType.ToContract(),
        EventSourceType = context.EventSourceType,
        EventSourceId = context.EventSourceId,
        EventStreamType = context.EventStreamType,
        EventStreamId = context.EventStreamId,
        SequenceNumber = context.SequenceNumber,
        Occurred = context.Occurred,
        EventStore = context.EventStore,
        Namespace = context.Namespace,
        CorrelationId = context.CorrelationId,
        Causation = context.Causation.Select(_ => _.ToContract()).ToList(),
        CausedBy = context.CausedBy.ToContract(),
        Tags = context.Tags.Select(_ => _.Value).ToArray(),
        Hash = context.Hash,
        ObservationState = context.ObservationState.ToContract(),
        Subject = context.Subject?.Value ?? string.Empty
    };

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="Contracts.Events.EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="EventContext"/>.</returns>
    internal static EventContext ToClient(this Contracts.Events.EventContext context) => new(
        context.EventType.ToClient(),
        context.EventSourceType,
        context.EventSourceId,
        context.EventStreamType,
        context.EventStreamId,
        context.SequenceNumber,
        context.Occurred,
        context.EventStore,
        context.Namespace,
        context.CorrelationId,
        context.Causation.Select(_ => _.ToClient()).ToArray(),
        context.CausedBy.ToClient(),
        context.Tags.Select(_ => (Tag)_).ToArray(),
        context.Hash,
        context.ObservationState.ToClient(),
        Subject: ResolveSubject(context.Subject, context.EventSourceId));

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="Contracts.Sequences.EventContext"/> to convert.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the context is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the context is for.</param>
    /// <returns>Converted <see cref="EventContext"/>.</returns>
    /// <remarks>
    /// The event store and namespace are not carried on <see cref="Contracts.Sequences.EventContext"/> - the
    /// caller already knows which event sequence it queried, so they are supplied rather than round-tripped.
    /// </remarks>
    internal static EventContext ToClient(this Contracts.Sequences.EventContext context, EventStoreName eventStore, EventStoreNamespaceName @namespace) => new(
        context.EventType.ToClient(),
        context.EventSourceType,
        context.EventSourceId,
        context.EventStreamType,
        context.EventStreamId,
        context.SequenceNumber,
        context.Occurred,
        eventStore,
        @namespace,
        context.CorrelationId,
        context.Causation.ToClient(),
        context.CausedBy.ToClient(),
        context.Tags.Select(_ => (Tag)_).ToArray(),
        context.Hash ?? EventHash.NotSet,
        context.ObservationState.ToClient(),
        Subject: ResolveSubject(context.Subject, context.EventSourceId));

    /// <summary>
    /// Resolves the <see cref="Subject"/> a server sent, falling back to the event source id when the server did not carry one.
    /// </summary>
    /// <param name="subject">The subject as carried on the contract.</param>
    /// <param name="eventSourceId">The event source id to fall back to.</param>
    /// <returns>The resolved <see cref="Subject"/>.</returns>
    /// <remarks>
    /// The fallback exists only for a server that predates the subject member on the contract, where an absent value
    /// meant "the subject is the event source id". An explicitly carried subject is never replaced by the event source id.
    /// </remarks>
    static Subject ResolveSubject(string? subject, string eventSourceId) =>
        string.IsNullOrEmpty(subject)
            ? new Subject(eventSourceId)
            : new Subject(subject);
}
