// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Defines the endpoint called for receiving events from the kernel.
/// </summary>
public interface IClientObservers
{
    /// <summary>
    /// Called for events to be handled.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer it is for.</param>
    /// <param name="events">The collection of <see cref="AppendedEvent"/>.</param>
    /// <returns>Sequence number of last successfully processed event.</returns>
    public Task<ObserverInvocationResult> OnNext(ObserverId observerId, IEnumerable<AppendedEvent> events);
}
