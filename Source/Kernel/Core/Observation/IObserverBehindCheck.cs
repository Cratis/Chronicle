// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the check that tells which observers have events waiting for them, without activating any of them.
/// </summary>
/// <remarks>
/// Shared by the queue's safety net and the registration path. The cursors of an event sequence are read in one read and
/// the tails in one lookup per observed event type, so the cost of a check does not grow with the number of observers
/// that turn out to be up to date. A failure to read is not contained: the caller decides how to err toward delivery.
/// </remarks>
public interface IObserverBehindCheck
{
    /// <summary>
    /// Compares the cursor of each observer with the tail of the event types it observes on one event sequence.
    /// </summary>
    /// <param name="eventSequence">The <see cref="EventSequenceKey"/> of the event sequence of one namespace.</param>
    /// <param name="candidates">The <see cref="ObserverBehindCheckCandidate"/>s to compare.</param>
    /// <returns>The <see cref="ObserverBehindCheckResult"/>.</returns>
    Task<ObserverBehindCheckResult> Check(EventSequenceKey eventSequence, IReadOnlyCollection<ObserverBehindCheckCandidate> candidates);
}
