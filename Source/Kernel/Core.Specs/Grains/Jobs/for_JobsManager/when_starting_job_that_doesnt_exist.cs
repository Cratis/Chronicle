// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;
using Orleans.TestKit;
using Catch = Cratis.Specifications.Catch;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager;

public class when_starting_job_that_doesnt_exist : given.the_manager
{
    Exception _error;
    SomeJobRequest _request;
    INullJobWithSomeRequest _job;
    void Establish()
    {
        _request = new(42);
        _job = Substitute.For<INullJobWithSomeRequest>();
        _job.Start(Arg.Any<SomeJobRequest>()).Returns(Result.Failed(StartJobError.Unknown));
        _silo.AddProbe(_ => _job);
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.Start<INullJobWithSomeRequest, SomeJobRequest>(_request));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_get_job_from_storage() => _jobStorage.DidNotReceive().GetJob(Arg.Any<JobId>());
    [Fact] void should_start_the_job_grain() => _job.Received(1).Start(_request);
}
