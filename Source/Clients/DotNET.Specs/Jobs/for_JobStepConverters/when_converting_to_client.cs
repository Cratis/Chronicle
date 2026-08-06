// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepConverters;

public class when_converting_to_client : Specification
{
    Contracts.Jobs.JobStepSummaryResponse _contract;
    JobStep _result;
    Guid _stepId;

    void Establish()
    {
        _stepId = Guid.NewGuid();
        _contract = new Contracts.Jobs.JobStepSummaryResponse
        {
            Id = _stepId,
            Type = "MyJobStepType",
            Name = "My Job Step",
            Status = Contracts.Jobs.JobStepStatus.CompletedSuccessfully,
            StatusChanges = [],
            Progress = new Contracts.Jobs.JobStepProgress { Percentage = 100, Message = "Done" }
        };
    }

    void Because() => _result = _contract.ToClient();

    [Fact] void should_set_id() => _result.Id.ShouldEqual(new JobStepId(_stepId));
    [Fact] void should_set_type() => _result.Type.ShouldEqual(new JobStepType("MyJobStepType"));
    [Fact] void should_set_name() => _result.Name.ShouldEqual(new JobStepName("My Job Step"));
    [Fact] void should_set_status() => _result.Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
    [Fact] void should_set_progress_percentage() => _result.Progress.Percentage.ShouldEqual(new JobStepPercentage(100));
    [Fact] void should_set_progress_message() => _result.Progress.Message.ShouldEqual(new JobStepProgressMessage("Done"));
}
