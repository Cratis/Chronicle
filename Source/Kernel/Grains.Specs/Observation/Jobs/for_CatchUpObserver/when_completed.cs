// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Observation.States;
using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver;

public class when_completed : given.a_catchup_observer_and_a_request
{
    IObserver _observer;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _stateStorage.State.HandledCount = 42;
    }

    async Task Because() => await _job.OnCompleted();

    [Fact] void should_report_handled_events() => _observer.Received(1).ReportHandledEvents(_stateStorage.State.HandledCount);
    [Fact] void should_transition_to_routing() => _observer.Received(1).TransitionTo<Routing>();
}
