// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Storage.ReadModels;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

public class and_resuming_an_isolated_reducer_replay : given.a_performing_job_step
{
    IReducerReplay _replay;
    ReplayContext _context;
    void Establish()
    {
        _performState.ReducerReplayJobId = Guid.NewGuid();
        _performState.LastSuccessfullyHandledEventSequenceNumber = 2UL;
        _replay = Substitute.For<IReducerReplay>();
        _context = new(new("model", 1), "Model", "Revert", DateTimeOffset.UtcNow) { ReplayContainerName = "fresh-attempt" };
        _replay.Begin(_performState.ReducerReplayJobId).Returns(_context);
        _silo.AddProbe(_ => _replay);
    }
    async Task Because() => await _jobStep.InvokePerformStep(_performState);
    [Fact] void should_restart_from_the_beginning_not_the_old_checkpoint() => _startSequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_allocate_a_fresh_attempt() => _replay.Received(1).Begin(_performState.ReducerReplayJobId);
    [Fact]
    void should_carry_the_isolated_target_to_every_silo() => _observerSubscriber.Received(3).OnNext(
        Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Is<ObserverSubscriberContext>(_ => _.ReplayContext == _context));
}
