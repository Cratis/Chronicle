// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Services.Auditing;

namespace Cratis.Chronicle.Services.Events;

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
    public static Contracts.Events.EventContext ToContract(this EventContext context) => new()
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
        Tags = context.Tags.Select(_ => _.Value),
        Hash = context.Hash,
        ObservationState = context.ObservationState.ToContract(),
        Subject = context.Subject?.Value ?? string.Empty
    };

    /// <summary>
    /// Convert to Chronicle version of <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context"><see cref="Contracts.Events.EventContext"/> to convert.</param>
    /// <returns>Converted <see cref="EventContext"/>.</returns>
    public static EventContext ToChronicle(this Contracts.Events.EventContext context) => new(
        context.EventType.ToChronicle(),
        context.EventSourceType,
        context.EventSourceId,
        context.EventStreamType,
        context.EventStreamId,
        context.SequenceNumber,
        context.Occurred,
        context.EventStore,
        context.Namespace,
        context.CorrelationId,
        context.Causation.Select(_ => _.ToChronicle()),
        context.CausedBy.ToChronicle(),
        context.Tags.Select(_ => new Tag(_)).ToArray(),
        context.Hash,
        context.ObservationState.ToChronicle(),
        context.ResolveSubject());

    /// <summary>
    /// Resolves the <see cref="Subject"/> from a contract context, falling back to the event source id when the
    /// sender did not carry one.
    /// </summary>
    /// <param name="context"><see cref="Contracts.Events.EventContext"/> to resolve for.</param>
    /// <returns>The resolved <see cref="Subject"/>.</returns>
    /// <remarks>
    /// The fallback exists only for a peer that predates the subject member on the contract. An explicitly set
    /// subject is never replaced by the event source id.
    /// </remarks>
    static Subject ResolveSubject(this Contracts.Events.EventContext context) =>
        string.IsNullOrEmpty(context.Subject)
            ? new Subject(context.EventSourceId)
            : new Subject(context.Subject);
}
