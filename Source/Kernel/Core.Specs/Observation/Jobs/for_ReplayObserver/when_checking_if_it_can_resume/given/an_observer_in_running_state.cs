// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_checking_if_it_can_resume.given;

public abstract class an_observer_in_running_state : for_ReplayObserver.given.a_replay_observer_job
{
    protected bool _canResume;

    protected abstract ObserverRunningState RunningState { get; }

    void Establish()
    {
        _stateStorage.State.Request = _request;
        _observer.IsSubscribed().Returns(true);
        _observer.GetState().Returns(_ => ObserverState.Empty with { RunningState = RunningState });
    }
}

