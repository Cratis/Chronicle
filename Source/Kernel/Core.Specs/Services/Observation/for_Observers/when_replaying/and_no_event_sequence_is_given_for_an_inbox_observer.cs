// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

public class and_no_event_sequence_is_given_for_an_inbox_observer : given.a_replayable_observer
{
    async Task Because() => await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = string.Empty
    });

    [Fact] void should_accept_the_replay_of_the_observers_inbox() =>
        _jobsManager.Received(1).Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(
            Arg.Is<ObserverOperatorCommandRequest>(_ => _.ObserverKey == _key));
}
