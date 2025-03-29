// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverServiceClient"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverServiceClient"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
public class ObserverServiceClient(IGrainFactory grainFactory, IServiceProvider serviceProvider) : GrainServiceClient<IObserverService>(serviceProvider), IObserverServiceClient
{
    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);

    /// <inheritdoc/>
    public async Task BeginReplayFor(ObserverDetails observerDetails) => await ForEachGrainService(service => service.BeginReplayFor(observerDetails));

    /// <inheritdoc/>
    public async Task ResumeReplayFor(ObserverDetails observerDetails) => await ForEachGrainService(service => service.ResumeReplayFor(observerDetails));

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails) => await ForEachGrainService(service => service.EndReplayFor(observerDetails));

    async Task ForEachGrainService(Func<IObserverService, Task> callback)
    {
        var hosts = await _managementGrain.GetHosts(true);
        var tasks = hosts.Keys.Select(host => callback(GetGrainService(host)));
        await Task.WhenAll(tasks);
    }
}