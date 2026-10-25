// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.given;

public class a_stalled_job_tracker : Specification
{
    internal StalledObserverJobs _tracker;
    protected JobState _job;
    protected DateTimeOffset _now;
    protected IReadOnlyList<JobId> _result;

    void Establish()
    {
        _tracker = new();
        _now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _job = new JobState { Id = JobId.New(), Status = JobStatus.Running, Progress = new() { TotalSteps = 10 } };
    }
}
