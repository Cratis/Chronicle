// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver.when_starting;

public class and_reducer_initialization_fails : given.a_replay_observer_job
{
    Exception? _error;

    void Establish()
    {
        var jobsManager = Substitute.For<IJobsManager>();
        jobsManager.GetAllJobs().Returns(Task.FromResult<IImmutableList<JobState>>([]));
        _silo.AddProbe(_ => jobsManager);
        _request = _request with { ObserverType = ObserverType.Reducer };
        _keyIndex.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(CreateKeys("source"));
        _replayServiceClient.BeginReplayFor(Arg.Any<ObserverDetails>())
            .Returns(Task.FromException(new ReplayInitializationFailed(ICanHandleReplayForObserver.Error.Unknown)));
    }

    async Task Because()
    {
        await _job.Start(_request);
        _error = await Catch.Exception(_job.InitializeReplayForTesting);
    }

    [Fact] void should_prevent_job_steps_from_starting() => _error.ShouldBeOfExactType<ReplayInitializationFailed>();
    [Fact] void should_attempt_reducer_initialization() => _replayServiceClient.Received(1).BeginReplayFor(Arg.Any<ObserverDetails>());
    [Fact] void should_notify_clients_without_attempting_a_swap() => _replayServiceClient.Received(1).EndReplayFor(Arg.Any<ObserverDetails>());
    [Fact]
    void should_not_report_success() => _observer.DidNotReceive().ReplayedSuccessfullySince(
        Arg.Any<EventSequenceNumber>(), Arg.Any<IReadOnlyDictionary<Key, EventSequenceNumber>>(), Arg.Any<EventType[]>(), Arg.Any<DateTimeOffset>());
}
