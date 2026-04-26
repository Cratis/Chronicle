// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobsConverters;

public class when_converting_collection_to_client : Specification
{
    IEnumerable<Contracts.Jobs.Job> _contracts;
    IEnumerable<Job> _result;
    IEventStore _eventStore;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _contracts =
        [
            new Contracts.Jobs.Job
            {
                Id = Guid.NewGuid(),
                Type = "TypeA",
                Status = Contracts.Jobs.JobStatus.Running,
                StatusChanges = [],
                Progress = new Contracts.Jobs.JobProgress()
            },
            new Contracts.Jobs.Job
            {
                Id = Guid.NewGuid(),
                Type = "TypeB",
                Status = Contracts.Jobs.JobStatus.CompletedSuccessfully,
                StatusChanges = [],
                Progress = new Contracts.Jobs.JobProgress()
            }
        ];
    }

    void Because() => _result = _contracts.ToClient(_eventStore);

    [Fact] void should_return_two_jobs() => _result.Count().ShouldEqual(2);
    [Fact] void should_convert_first_type() => _result.First().Type.ShouldEqual(new JobType("TypeA"));
    [Fact] void should_convert_second_status() => _result.Skip(1).First().Status.ShouldEqual(JobStatus.CompletedSuccessfully);
}
