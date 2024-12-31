// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
public class Jobs(IGrainFactory grainFactory, IStorage storage) : IJobs
{
    /// <inheritdoc/>
    public Task Stop(StopJob command, CallContext context = default)
    {
        var key = new JobsManagerKey(command.EventStoreName, command.Namespace);
        var manager = grainFactory.GetGrain<IJobsManager>(0, key);
        return manager.Stop(command.JobId);
    }

    /// <inheritdoc/>
    public Task Resume(ResumeJob command, CallContext context = default)
    {
        var key = new JobsManagerKey(command.EventStoreName, command.Namespace);
        var manager = grainFactory.GetGrain<IJobsManager>(0, key);
        return manager.Resume(command.JobId);
    }

    /// <inheritdoc/>
    public Task Delete(DeleteJob command, CallContext context = default)
    {
        var key = new JobsManagerKey(command.EventStoreName, command.Namespace);
        var manager = grainFactory.GetGrain<IJobsManager>(0, key);
        return manager.Delete(command.JobId);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetJobs(GetJobsRequest request, CallContext context = default)
    {
        var key = new JobsManagerKey(request.EventStoreName, request.Namespace);
        var manager = grainFactory.GetGrain<IJobsManager>(0, key);
        return (await manager.GetAllJobs()).ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Job>> ObserveJobs(GetJobsRequest request, CallContext context = default)
    {
        var catchOrObserve = storage.GetEventStore(request.EventStoreName).GetNamespace(request.Namespace).Jobs.ObserveJobs();
        if (catchOrObserve.IsSuccess)
        {
            return catchOrObserve.AsT0.Select(_ => _.ToContract());
        }

        return Observable.Empty<IEnumerable<Job>>();
    }
}
