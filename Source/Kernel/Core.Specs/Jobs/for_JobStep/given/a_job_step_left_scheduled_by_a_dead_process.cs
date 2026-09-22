// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Jobs.for_JobStep.given;

/// <summary>
/// A job step whose persisted state says it is already Scheduled - exactly what a step looks like
/// when the silo died between its work being scheduled and the work completing. The activation is
/// fresh, so no background task exists no matter what the persisted status claims.
/// </summary>
public class a_job_step_left_scheduled_by_a_dead_process : Specification
{
    protected TestKitSilo _silo = new();
    protected SomeJobStepGrain _jobStep;
    protected JobId _jobId;
    protected JobStepId _jobStepId;
    protected JobStepState _persistedState;

    async Task Establish()
    {
        _jobId = Guid.Parse("fefd1ea0-f739-4d68-8817-6c85f722dec4");
        _jobStepId = Guid.Parse("2b2b2b2b-0000-0000-0000-000000000002");

        _persistedState = new JobStepState
        {
            Id = new(_jobId, _jobStepId),
            IsPrepared = true,
            Status = JobStepStatus.Scheduled
        };

        _silo.AddPersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps, _persistedState);
        _silo.AddService<IJobStepThrottle>(new PassThroughThrottle());
        _silo.AddService<ILogger<SomeJobStepGrain>>(NullLogger<SomeJobStepGrain>.Instance);

        var key = new JobStepKey(_jobId, "event-store", "namespace");
        _jobStep = await _silo.CreateGrainAsync<SomeJobStepGrain>(_jobStepId, keyExtension: key);
    }

    sealed class PassThroughThrottle : IJobStepThrottle
    {
        public Task AcquireAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Release()
        {
        }
    }
}
