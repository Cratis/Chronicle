// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the facts the watchdog read about an eligible observer in one tick.
/// </summary>
/// <param name="Tail">The end of the event sequence.</param>
/// <param name="Next">The next event sequence number the observer expects.</param>
/// <param name="LastHandled">The event sequence number the observer last handled.</param>
/// <param name="HandledEventCount">How many events the observer has handled.</param>
/// <param name="Relevance">What is known about an event the observer observes waiting at or after <paramref name="Next"/>.</param>
/// <param name="IsCatchingUp">Whether the observer is catching up, which moves its cursor only after a scan.</param>
public readonly record struct ObserverLagReading(
    EventSequenceNumber Tail,
    EventSequenceNumber Next,
    EventSequenceNumber LastHandled,
    ulong HandledEventCount,
    ObserverLagRelevance Relevance,
    bool IsCatchingUp);
