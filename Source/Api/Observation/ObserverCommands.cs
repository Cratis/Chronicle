// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <param name="observers"><see cref="IObservers"/> for working with the observers.</param>
[Route("/api/event-store/{eventStore}/observers")]
public class ObserverCommands(IObservers observers) : ControllerBase
{
    /// <summary>
    /// Rewind a specific observer in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="namespace">Namespace within the event store the observer is for.</param>
    /// <param name="observerId">Identifier of the observer to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/replay/{observerId}")]
    public Task Replay(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string observerId) =>
        observers.Replay(new() { EventStoreName = eventStore, Namespace = @namespace, ObserverId = observerId });

    /// <summary>
    /// Retry a specific partition in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="namespace">Namespace within the event store the observer is for.</param>
    /// <param name="observerId">Identifier of the observer to rewind.</param>
    /// <param name="partition">Partition to retry.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/failed-partitions/{observerId}/try-recover-failed-partition/{partition}")]
    public Task TryRecoverFailedPartition(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string observerId,
        [FromRoute] string partition) =>
        observers.ReplayPartition(new() { EventStoreName = eventStore, Namespace = @namespace, ObserverId = observerId, Partition = partition });

    /// <summary>
    /// Rewind a specific observer in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="namespace">Namespace within the event store the observer is for.</param>
    /// <param name="observerId">Identifier of the observer to rewind.</param>
    /// <param name="partition">Specific partition to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/replay/{observerId}/{partition}")]
    public Task ReplayPartition(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string observerId,
        [FromRoute] string partition) =>
        observers.RetryPartition(new() { EventStoreName = eventStore, Namespace = @namespace, ObserverId = observerId, Partition = partition });
}
