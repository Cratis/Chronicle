// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepConverters;

public class when_converting_collection_to_client : Specification
{
    IEnumerable<Contracts.Jobs.JobStepSummaryResponse> _contracts;
    IEnumerable<JobStep> _result;

    void Establish() =>
        _contracts =
        [
            new Contracts.Jobs.JobStepSummaryResponse
            {
                Id = Guid.NewGuid(),
                Type = "StepTypeA",
                Status = Contracts.Jobs.JobStepStatus.Running,
                StatusChanges = [],
                Progress = new Contracts.Jobs.JobStepProgress()
            },
            new Contracts.Jobs.JobStepSummaryResponse
            {
                Id = Guid.NewGuid(),
                Type = "StepTypeB",
                Status = Contracts.Jobs.JobStepStatus.CompletedSuccessfully,
                StatusChanges = [],
                Progress = new Contracts.Jobs.JobStepProgress()
            }
        ];

    void Because() => _result = _contracts.ToClient();

    [Fact] void should_return_two_items() => _result.Count().ShouldEqual(2);
    [Fact] void should_convert_first_type() => _result.First().Type.ShouldEqual(new JobStepType("StepTypeA"));
    [Fact] void should_convert_second_status() => _result.Skip(1).First().Status.ShouldEqual(JobStepStatus.CompletedSuccessfully);
}
