// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.Runtime.Services;

namespace Cratis.Kernel.Grains.Observation;

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
    public async Task BeginReplayFor(ObserverDetails observerDetails)
    {
        var hosts = await _managementGrain.GetHosts(true);
        foreach (var host in hosts.Keys)
        {
            await GetGrainService(host).BeginReplayFor(observerDetails);
        }
    }

    /// <inheritdoc/>
    public async Task EndReplayFor(ObserverDetails observerDetails)
    {
        var hosts = await _managementGrain.GetHosts(true);
        foreach (var host in hosts.Keys)
        {
            await GetGrainService(host).EndReplayFor(observerDetails);
        }
    }
}
