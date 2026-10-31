// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Jobs.for_JobCommandOutcomes.given;

public class a_job_command : Specification
{
    protected IJobsManager _jobsManager;
    protected IGrainFactory _grainFactory;
    protected Cratis.Orleans.Jobs.JobId _jobId;

    void Establish()
    {
        _jobId = Cratis.Orleans.Jobs.JobId.New();
        _jobsManager = Substitute.For<IJobsManager>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(_jobsManager);
    }
}
