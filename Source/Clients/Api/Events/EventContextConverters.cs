// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Auditing;
using Cratis.Chronicle.Api.Identities;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Converts between contracts and API models for event context.
/// </summary>
internal static class EventContextConverters
{
    /// <summary>
    /// Converts a contract <see cref="Contracts.Events.EventContext"/> to an <see cref="EventContext"/>.
    /// </summary>
    /// <param name="context">The contract <see cref="Contracts.Events.EventContext"/> to convert.</param>
    /// <returns>The converted <see cref="EventContext"/>.</returns>
    public static EventContext ToApi(this Contracts.Events.EventContext context) => new(
        context.EventType.ToApi(),
        context.EventSourceType,
        context.EventSourceId,
        context.SequenceNumber,
        context.EventStreamType,
        context.EventStreamId,
        context.Occurred,
        context.CorrelationId,
        context.Causation.Select(c => new Causation(c.Occurred, c.Type, c.Properties)),
        new Identity(context.CausedBy.Subject, context.CausedBy.Name, context.CausedBy.UserName, null));

    /// <summary>
    /// Converts an <see cref="EventContext"/> to a contract <see cref="Contracts.Events.EventContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> to convert.</param>
    /// <returns>The converted <see cref="Contracts.Events.EventContext"/>.</returns>
    public static Contracts.Events.EventContext ToContract(this EventContext context) => new()
    {
        EventType = context.EventType.ToContract(),
        EventSourceType = context.EventSourceType,
        EventSourceId = context.EventSourceId,
        SequenceNumber = context.SequenceNumber,
        EventStreamType = context.EventStreamType,
        EventStreamId = context.EventStreamId,
        Occurred = context.Occurred!,
        CorrelationId = context.CorrelationId,
        Causation = context.Causation.ToContract(),
        CausedBy = context.CausedBy.ToContract()
    };
}
