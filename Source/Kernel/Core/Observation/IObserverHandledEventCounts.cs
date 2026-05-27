// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a grain that keeps an in-memory cache of the number of events each observer in an
/// event store namespace has handled and refreshes it on a regular cadence.
/// </summary>
/// <remarks>
/// The count represents the total number of events present in the observer's event sequence from
/// the first sequence number through <c>LastHandledEventSequenceNumber</c> inclusive. A redacted
/// event remains in the sequence at its original sequence number — the redaction replaces its
/// content but does not remove it — and therefore continues to count. This matches the intent
/// of the metric: an observer that processed an event before it was redacted has still handled it.
/// </remarks>
public interface IObserverHandledEventCounts : IGrainWithStringKey
{
    /// <summary>
    /// Get the cached handled event count for a specific observer and event sequence.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get the count for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer is observing.</param>
    /// <returns>The cached <see cref="EventCount"/>. Returns <see cref="EventCount.Zero"/> when the count is not yet cached.</returns>
    /// <remarks>
    /// Redacted events count: see the interface remarks for the full rationale.
    /// </remarks>
    Task<EventCount> GetCountFor(ObserverId observerId, EventSequenceId eventSequenceId);

    /// <summary>
    /// Get all cached handled event counts keyed by observer and event sequence.
    /// </summary>
    /// <returns>A read-only dictionary of <see cref="ObserverHandledEventCountKey"/> to <see cref="EventCount"/>.</returns>
    /// <remarks>
    /// Redacted events count: see the interface remarks for the full rationale.
    /// </remarks>
    Task<IReadOnlyDictionary<ObserverHandledEventCountKey, EventCount>> GetAll();

    /// <summary>
    /// Force a refresh of the cached handled event counts from storage.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Used by callers that need an immediately fresh view of the counts (for example, the
    /// observable observers query in the workbench), as well as by tests.
    /// </remarks>
    Task Refresh();
}
