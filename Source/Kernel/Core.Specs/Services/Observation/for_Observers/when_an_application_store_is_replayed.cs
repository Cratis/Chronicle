// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Security.given;
using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_an_application_store_is_replayed : given.all_dependencies
{
    IObserver _observer;
    Probe _probe;
    ReplayResponse _response;

    void Establish()
    {
        _probe = new Probe();
        _observer = Substitute.For<IObserver>();
        _observer.Replay().Returns(JobId.NotSet);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _response = await _observers.Replay(
        new Replay { EventStore = "application", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log" },
        CallFor(_probe, CallerNamed(a_system_store_caller.OrdinaryUser)));

    [Fact] void should_keep_the_existing_rpc_result() => _response.JobId.ShouldEqual(JobId.NotSet.Value.ToString());
    [Fact] void should_dispatch_to_the_observer() => _observer.Received(1).Replay();
    [Fact] void should_not_ask_for_administrator_privilege() => _probe.AccessResolutions.ShouldEqual(0);
}
