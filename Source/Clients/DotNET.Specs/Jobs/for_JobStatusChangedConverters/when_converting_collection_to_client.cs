// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStatusChangedConverters;

public class when_converting_collection_to_client : Specification
{
    IEnumerable<Contracts.Jobs.JobStatusChanged> _contracts;
    IList<JobStatusChanged> _result;

    void Establish() =>
        _contracts =
        [
            new Contracts.Jobs.JobStatusChanged { Status = Contracts.Jobs.JobStatus.Running },
            new Contracts.Jobs.JobStatusChanged { Status = Contracts.Jobs.JobStatus.CompletedSuccessfully }
        ];

    void Because() => _result = _contracts.ToClient();

    [Fact] void should_return_two_items() => _result.Count.ShouldEqual(2);
    [Fact] void should_convert_first_status() => _result[0].Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_convert_second_status() => _result[1].Status.ShouldEqual(JobStatus.CompletedSuccessfully);
}
