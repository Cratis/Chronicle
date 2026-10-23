// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

public class and_no_replay_job_is_started : given.all_dependencies
{
    ReplayResponse _response;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.Replay().Returns(JobId.NotSet);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _response = await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "event-log"
    });

    [Fact] void should_report_that_no_job_was_started_so_clients_return_not_set() =>
        _response.JobId.ShouldEqual(JobId.NotSet.Value.ToString());
}
