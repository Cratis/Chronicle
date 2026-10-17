// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catching_up;

public class and_no_catch_up_job_exists : given.an_observer_with_subscription
{
    void Establish() => _jobsManager
        .GetJobs(Arg.Any<JobQuery>())
        .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

    async Task Because() => await _observer.CatchUp();

    [Fact] void should_start_the_catch_up_job_with_the_scope_of_the_definition() => _jobsManager.Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(
            Arg.Is<CatchUpObserverRequest>(_ => _.ReaderScope != null && !_.ReaderScope.IsWildcard && _.ReaderScope.EventTypes.SequenceEqual(_definitionStorage.State.EventTypes)));
}
