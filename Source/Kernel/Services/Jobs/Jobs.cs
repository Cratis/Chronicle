// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Reactive;
using Cratis.Chronicle.Storage;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal class Jobs(IGrainFactory grainFactory, IStorage storage) : IJobs
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
    public async Task<OneOf<Job, Contracts.Jobs.JobError>> GetJob(GetJobRequest request, CallContext context = default)
    {
        grainFactory.GetJobsManager(request.EventStore, request.Namespace);

        var result = await storage.GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Jobs
            .GetJob(request.JobId);

        if (result.IsSuccess)
        {
            return new OneOf<Job, Contracts.Jobs.JobError>(result.AsT0.ToContract());
        }

        return new OneOf<Job, Contracts.Jobs.JobError>((Contracts.Jobs.JobError)(int)result.AsT1);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Job>> GetJobs(GetJobsRequest request, CallContext context = default) =>
        (await grainFactory.GetJobsManager(request.EventStore, request.Namespace).GetAllJobs()).ToContract();

    /// <inheritdoc/>
    public IObservable<IEnumerable<Job>> ObserveJobs(GetJobsRequest request, CallContext context = default)
    {
        var catchOrObserve = storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Jobs
            .ObserveJobs();

        if (catchOrObserve.IsSuccess)
        {
            return catchOrObserve.AsT0.CompletedBy(context.CancellationToken).Select(_ => _.ToContract());
        }

        return Observable.Empty<IEnumerable<Job>>();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<JobStep>> GetJobSteps(GetJobStepsRequest request, CallContext context = default)
    {
        var catchOrObserve = await storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).JobSteps
            .GetForJob(request.JobId, request.Statuses.Select(_ => (Concepts.Jobs.JobStepStatus)(int)_).ToArray());

        if (catchOrObserve.IsSuccess)
        {
            return catchOrObserve.AsT0.ToContract();
        }

        return [];
    }
}
