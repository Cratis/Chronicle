// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;

namespace Cratis.Api.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
[Route("/api/event-store/{eventStore}/observers")]
public class ObserverCommands(IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Rewind a specific observer in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="namespace">Namespace within the event store the observer is for.</param>
    /// <param name="observerId">Identifier of the observer to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/replay/{observerId}")]
    public async Task Replay(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string observerId)
    {
        await grainFactory.GetGrain<IObserver>(new ObserverKey(observerId, eventStore, @namespace, EventSequenceId.Log)).Replay();
    }

    /// <summary>
    /// Retry a specific partition in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="namespace">Namespace within the event store the observer is for.</param>
    /// <param name="observerId">Identifier of the observer to rewind.</param>
    /// <param name="partition">Partition to retry.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/failed-partitions/{observerId}/retry/{partition}")]
    public async Task RetryPartition(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string observerId,
        [FromRoute] string partition)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Rewind a specific observer in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="namespace">Namespace within the event store the observer is for.</param>
    /// <param name="observerId">Identifier of the observer to rewind.</param>
    /// <param name="partition">Specific partition to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/replay/{observerId}/{partition}")]
    public async Task ReplayPartition(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] string observerId,
        [FromRoute] string partition)
    {
        throw new NotImplementedException();
    }
}
