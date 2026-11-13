// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_a_reindex_is_already_running : given.a_reindex_command
{
    void Establish() => _jobs.GetJobs(Arg.Any<JobQuery>()).Returns(ImmutableList.Create(new JobState { Status = JobStatus.Running, Request = new ReindexConstraintsRequest("log", []) }));

    async Task Because() => _result = await _scenario.Execute(new("store", "ns", "log", []));

    [Fact] void should_reject_the_command() => _result.IsValid.ShouldBeFalse();
    [Fact] async Task should_not_start_a_second_job() => await _jobs.DidNotReceiveWithAnyArgs().Start<IReindexConstraints, ReindexConstraintsRequest>(default!);
}
