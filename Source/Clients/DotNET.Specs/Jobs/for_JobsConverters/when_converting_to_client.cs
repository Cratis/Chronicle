// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobsConverters;

public class when_converting_to_client : Specification
{
    Contracts.Jobs.Job _contract;
    Job _result;
    Guid _jobId;
    IEventStore _eventStore;
    DateTimeOffset _created;

    void Establish()
    {
        _jobId = Guid.NewGuid();
        _created = DateTimeOffset.UtcNow;
        _eventStore = Substitute.For<IEventStore>();
        _contract = new Contracts.Jobs.Job
        {
            Id = _jobId,
            Details = "Some details",
            Type = "MyJobType",
            Status = Contracts.Jobs.JobStatus.Running,
            Created = _created,
            StatusChanges = [new Contracts.Jobs.JobStatusChanged { Status = Contracts.Jobs.JobStatus.Running }],
            Progress = new Contracts.Jobs.JobProgress { TotalSteps = 5, SuccessfulSteps = 3 }
        };
    }

    void Because() => _result = _contract.ToClient(_eventStore);

    [Fact] void should_set_id() => _result.Id.ShouldEqual(new JobId(_jobId));
    [Fact] void should_set_details() => _result.Details.ShouldEqual(new JobDetails("Some details"));
    [Fact] void should_set_type() => _result.Type.ShouldEqual(new JobType("MyJobType"));
    [Fact] void should_set_status() => _result.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_set_created() => _result.Created.ShouldEqual(_created);
    [Fact] void should_have_one_status_change() => _result.StatusChanges.Count().ShouldEqual(1);
    [Fact] void should_set_progress_total_steps() => _result.Progress.TotalSteps.ShouldEqual(5);
    [Fact] void should_set_progress_successful_steps() => _result.Progress.SuccessfulSteps.ShouldEqual(3);
}
