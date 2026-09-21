// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobsConverters;

public class when_converting_to_client_with_default_strings : Specification
{
    Job _result;
    IEventStore _eventStore;

    void Establish() => _eventStore = Substitute.For<IEventStore>();

    void Because() => _result = new Contracts.Jobs.JobSummaryResponse().ToClient(_eventStore);

    [Fact] void should_use_empty_details() => _result.Details.ShouldEqual(new JobDetails(string.Empty));
    [Fact] void should_use_empty_type() => _result.Type.ShouldEqual(new JobType(string.Empty));
}
