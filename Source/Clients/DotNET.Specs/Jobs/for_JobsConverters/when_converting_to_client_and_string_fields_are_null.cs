// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobsConverters;

public class when_converting_to_client_and_string_fields_are_null : Specification
{
    Contracts.Jobs.Job _contract;
    Job _result;
    IEventStore _eventStore;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();
        _contract = new Contracts.Jobs.Job
        {
            Id = Guid.NewGuid(),
            Details = null!,
            Type = null!,
            StatusChanges = [],
            Progress = new Contracts.Jobs.JobProgress { Message = null! }
        };
    }

    void Because() => _result = _contract.ToClient(_eventStore);

    [Fact] void should_not_throw() => _result.ShouldNotBeNull();
    [Fact] void should_use_empty_details() => _result.Details.ShouldEqual(new JobDetails(string.Empty));
    [Fact] void should_use_empty_type() => _result.Type.ShouldEqual(new JobType(string.Empty));
}
