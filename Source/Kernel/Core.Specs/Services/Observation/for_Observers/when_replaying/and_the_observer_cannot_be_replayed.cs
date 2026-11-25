// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

/// <summary>
/// An observer that cannot be replayed accepts no job; clients read the empty job id as not set.
/// </summary>
public class and_the_observer_cannot_be_replayed : given.a_replayable_observer
{
    ReplayResponse _response;

    void Establish() => _observerDefinitionsStorage.Get(Arg.Any<Concepts.Observation.ObserverId>()).Returns(new ObserverDefinition(
        _key.ObserverId, [], _key.EventSequenceId, Concepts.Observation.ObserverType.Reactor, Concepts.Observation.ObserverOwner.Client, false));

    async Task Because() => _response = await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "inbox-source"
    });

    [Fact] void should_report_that_no_job_was_started_so_clients_return_not_set() =>
        _response.JobId.ShouldEqual(JobId.NotSet.Value.ToString());

    [Fact] void should_not_start_a_job() => _jobsManager.DidNotReceive().Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
        Arg.Any<ObserverOperatorCommandRequest>());
}
