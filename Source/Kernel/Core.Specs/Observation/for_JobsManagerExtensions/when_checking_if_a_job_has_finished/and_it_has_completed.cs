// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_checking_if_a_job_has_finished;

public class and_it_has_completed : Specification
{
    bool _result;

    void Because() => _result = JobStatus.CompletedSuccessfully.HasFinished();

    [Fact] void should_have_finished() => _result.ShouldBeTrue();
}
