// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Fast forwards observers through the kernel on behalf of the artifact specific APIs.
/// </summary>
static class ObserverFastForwarder
{
    /// <summary>
    /// Fast forward an observer to the tail of the event log.
    /// </summary>
    /// <param name="observers">The <see cref="Contracts.Observation.IObservers"/> service.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <param name="observerId">The observer identifier.</param>
    /// <returns>The <see cref="FastForwardResult"/>.</returns>
    internal static async Task<FastForwardResult> FastForward(Contracts.Observation.IObservers observers, string eventStore, string @namespace, string observerId)
    {
        var response = await observers.FastForward(new Contracts.Observation.FastForward
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ObserverId = observerId,
            EventSequenceId = string.Empty
        });

        return new FastForwardResult(
            (FastForwardOutcome)(int)response.Outcome,
            response.PreviousNextEventSequenceNumber,
            response.NextEventSequenceNumber);
    }
}
