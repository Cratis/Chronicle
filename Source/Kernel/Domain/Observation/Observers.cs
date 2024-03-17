// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;
using Cratis.Kernel.Grains.Observation;
using Cratis.Kernel.Keys;
using Cratis.Observation;
using Cratis.Properties;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
[Route("/api/events/store/{eventStore}/observers")]
public class Observers(IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Rewind a specific observer in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/replay/{observerId}")]
    public async Task Replay(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] ObserverId observerId)
    {
        var observer = grainFactory.GetGrain<IObserver>(observerId, new ObserverKey(eventStore, @namespace, EventSequenceId.Log));
        await observer.Replay();
    }

    /// <summary>
    /// Retry a specific partition in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <param name="partition"><see cref="Key">Partition</see> to retry.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/failed-partitions/{observerId}/retry/{partition}")]
    public async Task RetryPartition(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] ObserverId observerId,
        [FromRoute] string partition)
    {
        var observer = grainFactory.GetGrain<IObserver>(observerId, new ObserverKey(eventStore, @namespace, EventSequenceId.Log));
        await observer.TryRecoverFailedPartition(new Key(partition, ArrayIndexers.NoIndexers));
    }

    /// <summary>
    /// Rewind a specific observer in an event store and specific namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer is for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <param name="partition">Specific <see cref="Key"/> to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{namespace}/replay/{observerId}/{partition}")]
    public async Task ReplayPartition(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] ObserverId observerId,
        [FromRoute] string partition)
    {
        var observer = grainFactory.GetGrain<IObserver>(observerId, new ObserverKey(eventStore, @namespace, EventSequenceId.Log));
        await observer.ReplayPartition(new Key(partition, ArrayIndexers.NoIndexers));
    }
}
