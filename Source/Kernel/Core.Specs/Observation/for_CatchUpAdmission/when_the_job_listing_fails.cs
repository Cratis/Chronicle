// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission;

public class when_the_job_listing_fails : given.a_catch_up_admission
{
    IDisposable? _lease;

    async Task Because()
    {
        _jobStorage.GetJobs(Arg.Any<JobQuery>()).Returns(_ => Task.FromResult(Catch<IImmutableList<JobState>>.Failed(new InvalidOperationException("storage is down"))));
        _lease = await _admission.TryAdmit(KeyOf("one"), 1, 0);
    }

    [Fact] void should_admit_the_observer() => _lease.ShouldNotBeNull();
}
