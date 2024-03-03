// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Kernel.Storage.EventSequences;

/// <summary>
/// Represents the tail sequence numbers.
/// </summary>
/// <param name="EventSequenceId"><see cref="EventSequenceId"/> the numbers are for.</param>
/// <param name="EventTypes">Collection of <see cref="EventType"/> the numbers are for.</param>
/// <param name="Tail">The tail <see cref="EventSequenceNumber"/> for the event sequence.</param>
/// <param name="TailForEventTypes">The tail <see cref="EventSequenceNumber"/> for any of the event types.</param>
public record TailEventSequenceNumbers(EventSequenceId EventSequenceId, IImmutableList<EventType> EventTypes, EventSequenceNumber Tail, EventSequenceNumber TailForEventTypes)
{
    /// <summary>
    /// Represents an empty instance of <see cref="TailEventSequenceNumbers"/>.
    /// </summary>
    public static readonly TailEventSequenceNumbers Empty = new(EventSequenceId.Unspecified, ImmutableList<EventType>.Empty, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable);
}
