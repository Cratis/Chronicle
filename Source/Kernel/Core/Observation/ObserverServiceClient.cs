// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Observation;

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
    public async Task EndReplayFor(ObserverDetails observerDetails) => _ = await TryFinalizeReplayFor(observerDetails);

    /// <inheritdoc/>
    public async Task<bool> TryFinalizeReplayFor(ObserverDetails observerDetails)
    {
        if (observerDetails.Type != ObserverType.Projection)
        {
            await ForEachGrainService(service => service.EndReplayFor(observerDetails));
            return false;
        }

        var hosts = await _managementGrain.GetHosts(true);
        var results = await Task.WhenAll(hosts.Keys.Select(host => GetGrainService(host).TryFinalizeReplayFor(observerDetails)));
        EnsureProjectionReplayFinalized(results);
        return true;
    }

    /// <inheritdoc/>
    public async Task BeginReplayPartitionFor(ObserverDetails observerDetails, Key partition) => await ForEachGrainService(service => service.BeginReplayPartitionFor(observerDetails, partition));

    /// <inheritdoc/>
    public async Task EndReplayPartitionFor(ObserverDetails observerDetails, Key partition) => await ForEachGrainService(service => service.EndReplayPartitionFor(observerDetails, partition));

    /// <inheritdoc/>
    public async Task BeginCatchupFor(ObserverDetails observerDetails) => await ForEachGrainService(service => service.BeginCatchupFor(observerDetails));

    /// <inheritdoc/>
    public async Task ResumeCatchupFor(ObserverDetails observerDetails) => await ForEachGrainService(service => service.ResumeCatchupFor(observerDetails));

    /// <inheritdoc/>
    public async Task EndCatchupFor(ObserverDetails observerDetails) => await ForEachGrainService(service => service.EndCatchupFor(observerDetails));

    /// <summary>
    /// Ensure at least one silo finalized a projection replay.
    /// </summary>
    /// <param name="results">Whether each silo finalized the replay.</param>
    /// <exception cref="ReplayFinalizationFailed">No silo finalized the replay.</exception>
    internal static void EnsureProjectionReplayFinalized(IEnumerable<bool> results)
    {
        if (!results.Any(_ => _))
        {
            throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.CouldNotGetReplayContext);
        }
    }

    async Task ForEachGrainService(Func<IObserverService, Task> callback)
    {
        var hosts = await _managementGrain.GetHosts(true);
        var tasks = hosts.Keys.Select(host => callback(GetGrainService(host)));
        await Task.WhenAll(tasks);
    }
}