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
public interface IObserverHandledEventCounts : IGrainWithStringKey
{
    /// <summary>
    /// Get the cached handled event count for a specific observer and event sequence.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get the count for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer is observing.</param>
    /// <returns>The cached <see cref="EventCount"/>. Returns <see cref="EventCount.Zero"/> when the count is not yet cached.</returns>
    Task<EventCount> GetCountFor(ObserverId observerId, EventSequenceId eventSequenceId);

    /// <summary>
    /// Get all cached handled event counts keyed by observer and event sequence.
    /// </summary>
    /// <returns>A read-only dictionary of <see cref="ObserverHandledEventCountKey"/> to <see cref="EventCount"/>.</returns>
    Task<IReadOnlyDictionary<ObserverHandledEventCountKey, EventCount>> GetAll();

    /// <summary>
    /// Force a refresh of the cached handled event counts from storage. Used by tests and any explicit refresh requests.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Refresh();
}
