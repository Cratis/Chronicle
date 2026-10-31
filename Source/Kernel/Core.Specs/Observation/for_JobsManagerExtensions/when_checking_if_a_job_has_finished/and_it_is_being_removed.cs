// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_checking_if_a_job_has_finished;

/// <summary>
/// A job being removed is still draining and will still report back (#5378).
/// </summary>
public class and_it_is_being_removed : Specification
{
    bool _result;

    void Because() => _result = JobStatus.Removing.HasFinished();

    [Fact] void should_not_have_finished() => _result.ShouldBeFalse();
}
