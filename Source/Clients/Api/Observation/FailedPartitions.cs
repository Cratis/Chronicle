// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Reactive;
using IFailedPartitions = Cratis.Chronicle.Contracts.Observation.IFailedPartitions;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/failed-partitions")]
public class FailedPartitions : ControllerBase
{
    readonly IFailedPartitions _failedPartitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedPartitions"/> class.
    /// </summary>
    /// <param name="failedPartitions"><see cref="IFailedPartitions"/> for working with failed partitions.</param>
    internal FailedPartitions(IFailedPartitions failedPartitions)
    {
        _failedPartitions = failedPartitions;
    }

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
        [FromRoute] string? observerId = default) =>
        _failedPartitions.InvokeAndWrapWithTransformSubject(
            token => _failedPartitions.ObserveFailedPartitions(new() { EventStore = eventStore, Namespace = @namespace, ObserverId = observerId }, token),
            failedPartitions => failedPartitions.ToApi());
}
