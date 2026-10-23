// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

public class and_a_replay_job_is_already_running : given.all_dependencies
{
    readonly JobId _jobId = JobId.New();
    ReplayResponse _response;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.Replay().Returns(_jobId);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _response = await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "event-log"
    });

    [Fact] void should_return_the_existing_job_id() => _response.JobId.ShouldEqual(_jobId.Value.ToString());
}
