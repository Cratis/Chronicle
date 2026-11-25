// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Security.given;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_an_application_store_is_replayed : given.a_replayable_observer
{
    Probe _probe;
    ReplayResponse _response;

    void Establish() => _probe = new Probe();

    async Task Because() => _response = await _observers.Replay(
        new Replay { EventStore = "application", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log" },
        CallFor(_probe, CallerNamed(a_system_store_caller.OrdinaryUser)));

    [Fact] void should_return_the_id_of_the_accepted_job() => _response.JobId.ShouldEqual(_acceptedJob.Value.ToString());
    [Fact] void should_accept_the_replay_as_a_job() => _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Is<ObserverOperatorCommandRequest>(_ => _.Command == ObserverOperatorCommand.Replay));
    [Fact] void should_not_call_the_observer() => _observer.DidNotReceive().Replay();
    [Fact] void should_not_ask_for_administrator_privilege() => _probe.AccessResolutions.ShouldEqual(0);
}
