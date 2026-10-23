// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Orleans.Jobs;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_replaying;

public class and_no_replay_job_is_started : given.all_dependencies
{
    Exception _exception;

    void Establish()
    {
        var observer = Substitute.For<IObserver>();
        observer.Replay().Returns(JobId.NotSet);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _observers.Replay(new Replay
    {
        EventStore = "some-event-store",
        Namespace = "some-namespace",
        ObserverId = "some-observer",
        EventSequenceId = "event-log"
    }));

    [Fact] void should_report_a_failed_precondition() =>
        ((RpcException)_exception).StatusCode.ShouldEqual(StatusCode.FailedPrecondition);

    [Fact] void should_explain_why_replay_was_not_started() =>
        ((RpcException)_exception).Status.Detail.ShouldEqual(ReplayObserver.ReplayNotStartedMessage);
}
