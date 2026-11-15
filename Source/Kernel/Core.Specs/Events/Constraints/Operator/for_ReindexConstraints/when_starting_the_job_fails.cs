// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_starting_the_job_fails : given.a_reindex_command
{
    void Establish() => _jobs.Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>()).Returns(Result<JobId, StartJobError>.Failed(StartJobError.CouldNotPrepareJobSteps));

    async Task Because() => _result = await _scenario.Execute(new("store", "ns", "log", []));

    [Fact] void should_not_claim_success() => _result.IsSuccess.ShouldBeFalse();
}
