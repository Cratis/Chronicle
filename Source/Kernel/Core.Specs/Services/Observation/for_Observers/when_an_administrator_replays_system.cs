// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Security.given;
using Cratis.Orleans.Jobs;
using IObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_an_administrator_replays_system : given.all_dependencies
{
    IObserver _observer;
    ReplayResponse _response;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observer.Replay().Returns(JobId.NotSet);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _response = await _observers.Replay(
        new Replay { EventStore = "System", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log" },
        CallFor(new Probe(), CallerNamed(a_system_store_caller.Administrator)));

    [Fact] void should_keep_the_existing_rpc_result() => _response.JobId.ShouldEqual(JobId.NotSet.Value.ToString());
    [Fact] void should_dispatch_to_the_observer() => _observer.Received(1).Replay();
}
