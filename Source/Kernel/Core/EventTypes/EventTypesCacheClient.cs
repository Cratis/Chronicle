// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesCacheClient"/> that fans out to the
/// <see cref="IEventTypesCacheGrainService"/> on every silo, so each silo evicts its own event type caches.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventTypesCacheClient"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
public class EventTypesCacheClient(IGrainFactory grainFactory, IServiceProvider serviceProvider)
    : GrainServiceClient<IEventTypesCacheGrainService>(serviceProvider), IEventTypesCacheClient
{
    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);

    /// <inheritdoc/>
    public async Task Invalidate(EventStoreName eventStore, EventTypeId eventTypeId) =>
        await ForEachGrainService(service => service.Invalidate(eventStore, eventTypeId));

    async Task ForEachGrainService(Func<IEventTypesCacheGrainService, Task> callback)
    {
        var hosts = await _managementGrain.GetHosts(true);
        var tasks = hosts.Keys.Select(host => callback(GetGrainService(host)));
        await Task.WhenAll(tasks);
    }
}
