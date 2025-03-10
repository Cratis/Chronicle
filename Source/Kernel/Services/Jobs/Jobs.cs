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
    public Task Stop(StopJob command, CallContext context = default) =>
        grainFactory.GetJobsManager(command.EventStore, command.Namespace).Stop(command.JobId);

    /// <inheritdoc/>
    public Task Resume(ResumeJob command, CallContext context = default) =>
        grainFactory.GetJobsManager(command.EventStore, command.Namespace).Resume(command.JobId);

    /// <inheritdoc/>
    public Task Delete(DeleteJob command, CallContext context = default) =>
        grainFactory.GetJobsManager(command.EventStore, command.Namespace).Delete(command.JobId);

    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetJobs(GetJobsRequest request, CallContext context = default) =>
        (await grainFactory.GetJobsManager(request.EventStore, request.Namespace).GetAllJobs()).ToContract();

    /// <inheritdoc/>
    public IObservable<IEnumerable<Job>> ObserveJobs(GetJobsRequest request, CallContext context = default)
    {
        var catchOrObserve = storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Jobs.ObserveJobs();
        if (catchOrObserve.IsSuccess)
        {
            return catchOrObserve.AsT0.Select(_ => _.ToContract());
        }

        return Observable.Empty<IEnumerable<Job>>();
    }
}
