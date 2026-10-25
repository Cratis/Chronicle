// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Dispatches received batches through a store's existing reactor dispatcher.
/// </summary>
internal interface IReceiveReactorDeliveries
{
    /// <summary>
    /// Delivers a batch to a discovered reactor.
    /// </summary>
    /// <param name="id">The reactor identifier.</param>
    /// <param name="events">The batch.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The published result, or null if none was published.</returns>
    Task<ReactorResult?> Deliver(ReactorId id, EventsToObserve events, CancellationToken cancellationToken);
}
