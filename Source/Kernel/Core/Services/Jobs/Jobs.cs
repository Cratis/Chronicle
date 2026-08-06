// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobs"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/> for logging.</param>
internal sealed class Jobs(IGrainFactory grainFactory, IStorage storage, ILogger<Jobs> logger) : IJobs
{
    /// <inheritdoc/>
    public Task<CommandResult> DeleteJob(DeleteJobRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Jobs.DeleteJob(request.EventStore, request.Namespace, request.JobId),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public Task<CommandResult> ResumeJob(ResumeJobRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Jobs.ResumeJob(request.EventStore, request.Namespace, request.JobId),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public Task<CommandResult> StopJob(StopJobRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Jobs.StopJob(request.EventStore, request.Namespace, request.JobId),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<JobSummaryResponse>>> AllJobs(AllJobsRequest request, CallContext callContext = default) =>
        QueryExecutor.Execute(
            async () =>
            {
                var jobs = await Chronicle.Jobs.JobSummary.AllJobs(request.EventStore, request.Namespace, grainFactory);
                return jobs.Select(ToResponse);
            },
            exception => logger.FailedToGetJobs(exception, request.EventStore, request.Namespace));

    /// <inheritdoc/>
    public IObservable<QueryResult<IEnumerable<JobSummaryResponse>>> ObserveJobs(ObserveJobsRequest request, CallContext callContext = default) =>
        QueryExecutor.Execute(
            () => Chronicle.Jobs.JobSummary.ObserveJobs(request.EventStore, request.Namespace, storage)
                .CompletedBy(callContext.CancellationToken)
                .Select(jobs => (IEnumerable<JobSummaryResponse>)jobs.Select(ToResponse).ToList()),
            exception => logger.FailedToObserveJobs(exception, request.EventStore, request.Namespace));

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<JobStepSummaryResponse>>> GetJobSteps(GetJobStepsRequest request, CallContext callContext = default) =>
        QueryExecutor.Execute(
            async () =>
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
            },
            exception => logger.FailedToGetJobSteps(exception, request.JobId, request.EventStore, request.Namespace));

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
