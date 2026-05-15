// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal sealed class Jobs(IGrainFactory grainFactory, IStorage storage) : IJobs
{
    /// <inheritdoc/>
    public Task DeleteJob(DeleteJobRequest request, CallContext callContext = default) =>
        new Chronicle.Jobs.DeleteJob(request.EventStore, request.Namespace, request.JobId)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task ResumeJob(ResumeJobRequest request, CallContext callContext = default) =>
        new Chronicle.Jobs.ResumeJob(request.EventStore, request.Namespace, request.JobId)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task StopJob(StopJobRequest request, CallContext callContext = default) =>
        new Chronicle.Jobs.StopJob(request.EventStore, request.Namespace, request.JobId)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public IObservable<IEnumerable<JobSummaryResponse>> AllJobs(AllJobsRequest request, CallContext callContext = default) =>
        Chronicle.Jobs.JobSummary.AllJobs(request.EventStore, request.Namespace, storage)
            .CompletedBy(callContext.CancellationToken)
            .Select(jobs => (IEnumerable<JobSummaryResponse>)jobs.Select(j => ToResponse(j)).ToList());

    /// <inheritdoc/>
    public async Task<IEnumerable<JobStepSummaryResponse>> GetJobSteps(GetJobStepsRequest request, CallContext callContext = default)
    {
        var steps = await Chronicle.Jobs.JobStepSummary.GetJobSteps(request.EventStore, request.Namespace, request.JobId, storage);
        return steps.Select(s => new JobStepSummaryResponse
        {
            Id = s.Id,
            Type = s.Type,
            Name = s.Name,
            Status = s.Status,
            StatusChanges = s.StatusChanges,
            Progress = s.Progress
        });
    }

    static JobSummaryResponse ToResponse(Chronicle.Jobs.JobSummary job) => new()
    {
        Id = job.Id,
        Details = job.Details,
        Type = job.Type,
        Status = job.Status,
        Created = job.Created,
        StatusChanges = job.StatusChanges,
        Progress = job.Progress
    };
}
