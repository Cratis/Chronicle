// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_catching_up_and_observer_is_replaying : given.an_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Replaying };
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.CatchUp();

    [Fact] void should_not_start_a_catch_up_job() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
}
