// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_accepting_catch_up;

public class and_the_write_fails : given.an_observer_accepting_a_catch_up_handover
{
    readonly IOException _failure = new("storage failed");
    Exception? _error;

    void Establish() => _writes.BeforeWrite = () => Task.FromException(_failure);

    async Task Because() => _error = await Catch.Exception(() => _observer.CaughtUp(JobId.NotSet, 12UL, 12UL));

    [Fact] void should_not_report_success() => _error.ShouldEqual(_failure);
    [Fact] void should_not_acquire_a_successor_after_failed_acceptance() => _jobsManager.ReceivedCalls().ShouldBeEmpty();
}
