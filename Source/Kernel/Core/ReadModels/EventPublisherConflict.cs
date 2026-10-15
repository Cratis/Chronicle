// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when two targets would publish the same event type to the same event sequence.
/// </summary>
/// <remarks>
/// The published event is the state of its key. Two publishers of one event type to one destination would fold each
/// other's output, so exactly one target may publish a given event type to a given sequence.
/// </remarks>
/// <param name="destination">The <see cref="EventSequenceId"/> both would publish to.</param>
/// <param name="eventType">The <see cref="EventTypeId"/> both would publish.</param>
/// <param name="first">The <see cref="ReadModelIdentifier"/> that already publishes it.</param>
/// <param name="second">The <see cref="ReadModelIdentifier"/> that was refused.</param>
public class EventPublisherConflict(EventSequenceId destination, EventTypeId eventType, ReadModelIdentifier first, ReadModelIdentifier second)
    : Exception($"Event type '{eventType}' is already published to event sequence '{destination}' by '{first}'. '{second}' cannot publish it as well: one publisher per destination and event type.");
