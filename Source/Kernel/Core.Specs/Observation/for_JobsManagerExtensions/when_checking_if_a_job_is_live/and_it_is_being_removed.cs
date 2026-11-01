// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.when_checking_if_a_job_is_live;

/// <summary>
/// A job being removed is still draining and reports back when its workers stop, so it still owns its work (#5378).
/// </summary>
public class and_it_is_being_removed : Specification
{
    bool _result;

    void Because() => _result = new JobState { Id = JobId.New(), Status = JobStatus.Removing }.IsLive(DateTimeOffset.UtcNow);

    [Fact] void should_be_live() => _result.ShouldBeTrue();
}
