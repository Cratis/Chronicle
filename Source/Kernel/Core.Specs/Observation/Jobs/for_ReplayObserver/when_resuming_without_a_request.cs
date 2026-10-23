// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_resuming_without_a_request : given.a_replay_observer_job
{
    Exception _error;
    bool _resumed;

    void Establish() => _stateStorage.State.Request = null!;

    async Task Because() => _error = await Catch.Exception(async () => _resumed = (await _job.Resume()).IsSuccess);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_refuse_to_resume() => _resumed.ShouldBeFalse();
    [Fact] void should_not_contact_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
}
