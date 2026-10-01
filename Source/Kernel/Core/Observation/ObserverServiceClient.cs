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
        if (observerDetails.Type is not (ObserverType.Projection or ObserverType.Reducer))
        {
            await ForEachGrainService(service => service.EndReplayFor(observerDetails));
            return false;
        }

        var hosts = await _managementGrain.GetHosts(true);
        return await FinalizeProjectionReplay([.. hosts.Keys.Select(GetGrainService)], observerDetails);
    }

    /// <inheritdoc/>
    public async Task<bool> FlushReplayFor(ObserverDetails observerDetails)
    {
        var hosts = await _managementGrain.GetHosts(true);
        var flushed = await Task.WhenAll(hosts.Keys.Select(host => GetGrainService(host).FlushReplayFor(observerDetails)));
        return flushed.All(cleanly => cleanly);
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
    /// Finalizes a projection replay across the silos of the cluster.
    /// </summary>
    /// <remarks>
    /// Every silo holds its own sink, and only the silo the replay ran on holds back its last writes. Ending the replay
    /// promotes the rebuilt read model, and the first silo to end it does the promoting - so if a silo with nothing held
    /// back ended it first, the replaying silo's final writes would land in a replay container nothing reads, and the
    /// promoted read model would be missing them. Every silo therefore flushes before any silo ends the replay.
    /// </remarks>
    /// <param name="silos">The <see cref="IObserverService"/> of every silo in the cluster.</param>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>True once the replay has been finalized.</returns>
    /// <exception cref="ReplayFinalizationFailed">No silo finalized the replay, or a silo flushed with failed partitions.</exception>
    internal static async Task<bool> FinalizeProjectionReplay(IReadOnlyCollection<IObserverService> silos, ObserverDetails observerDetails)
    {
        bool[] flushed;
        try
        {
            flushed = await Task.WhenAll(silos.Select(silo => silo.FlushReplayFor(observerDetails)));
        }
        catch
        {
            if (observerDetails.Type == ObserverType.Reducer)
            {
                // A failed RPC must not prevent surviving silos from leaving their replay sinks.
                // Keep the original barrier failure even if an unreachable silo also fails cleanup.
                await Task.WhenAll(silos.Select(async silo =>
                {
                    try
                    {
                        await silo.TryFinalizeReplayFor(observerDetails with { ReplayAborted = true });
                    }
                    catch
                    {
                        // Best effort: the silo may have disappeared since hosts were discovered.
                    }
                }));
            }

            throw;
        }

        if (observerDetails.Type == ObserverType.Reducer && !observerDetails.ReplayAborted)
        {
            // One silo promotes a reducer rebuild, then every other silo leaves replay without another swap.
            // In particular, an intentionally empty SQL rebuild must not recreate and promote an empty shadow
            // table a second time. Every silo has already flushed before the first promotion begins.
            var remaining = silos.ToList();
            var finalized = false;
            try
            {
                while (remaining.Count > 0 && !finalized)
                {
                    var silo = remaining[0];
                    remaining.RemoveAt(0);
                    finalized = await silo.TryFinalizeReplayFor(observerDetails);
                }

                EnsureProjectionReplayFinalized([finalized]);
            }
            finally
            {
                await Task.WhenAll(remaining.Select(silo => silo.TryFinalizeReplayFor(observerDetails with { ReplayAlreadyFinalized = true })));
            }
        }
        else
        {
            var results = await Task.WhenAll(silos.Select(silo => silo.TryFinalizeReplayFor(observerDetails)));
            EnsureProjectionReplayFinalized(results);
        }

        // A final flush that left failed partitions has recorded them for retry, and the replay is still promoted, as
        // it was when each silo flushed while ending. The replay is still not reported as cleanly finalized.
        if (flushed.Any(cleanly => !cleanly))
        {
            throw new ReplayFinalizationFailed(ICanHandleReplayForObserver.Error.Unknown);
        }

        return true;
    }

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