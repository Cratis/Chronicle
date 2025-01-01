// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Api.Observation;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
/// <param name="failedPartitions"><see cref="IFailedPartitions"/> for working with failed partitions.</param>
[Route("/api/event-store/{eventStore}/{namespace}/failed-partitions")]
public class FailedPartitions(IFailedPartitions failedPartitions) : ControllerBase
{
    /// <summary>
    /// Gets all failed partitions for an event store and namespace.
    /// </summary>
    /// <param name="eventStore">Event store the failed partitions are for.</param>
    /// <param name="namespace">Namespace the failed partitions are for.</param>
    /// <param name="observerId">Optional observer id to filter down which observer it is for.</param>
    /// <returns>Client observable of a collection of <see cref="FailedPartitions"/>.</returns>
    [HttpGet("{observerId?}")]
    public ISubject<IEnumerable<FailedPartition>> AllFailedPartitions(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string? observerId = default)
    {
        var subject = new Subject<IEnumerable<FailedPartition>>();
        failedPartitions.ObserveFailedPartitions(new() { EventStore = eventStore, Namespace = @namespace, ObserverId = observerId }).Subscribe(subject);
        return subject;
    }
}
