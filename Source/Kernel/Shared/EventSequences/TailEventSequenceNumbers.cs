// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.EventSequences;

/// <summary>
/// Represents the tail sequence numbers.
/// </summary>
/// <param name="EventSequenceId"><see cref="EventSequenceId"/> the numbers are for.</param>
/// <param name="EventTypes">Collection of <see cref="EventType"/> the numbers are for.</param>
/// <param name="Tail">The tail <see cref="EventSequenceNumber"/> for the event sequence.</param>
/// <param name="TailForEventTypes">The tail <see cref="EventSequenceNumber"/> for any of the event types.</param>
public record TailEventSequenceNumbers(EventSequenceId EventSequenceId, IImmutableList<EventType> EventTypes, EventSequenceNumber Tail, EventSequenceNumber TailForEventTypes);
