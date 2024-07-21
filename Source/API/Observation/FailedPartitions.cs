// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Queries;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactions;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Observation;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverQueries"/> class.
/// </remarks>
[Route("/api/events/store/{eventStore}/{namespace}/failed-partitions")]
public class FailedPartitions() : ControllerBase
{
    /// <summary>
    /// Gets all failed partitions for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the failed partitions are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the failed partitions are for.</param>
    /// <param name="observerId">Optional <see cref="ObserverId"/> to filter down which observer it is for.</param>
    /// <returns>Client observable of a collection of <see cref="FailedPartitions"/>.</returns>
    [HttpGet("{observerId}")]
    public Task<ClientObservable<IEnumerable<FailedPartition>>> AllFailedPartitions(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string? observerId = default)
    {
        throw new NotImplementedException();
    }
}
