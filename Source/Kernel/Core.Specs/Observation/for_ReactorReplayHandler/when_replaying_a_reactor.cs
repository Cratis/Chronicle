// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;

namespace Cratis.Chronicle.Observation.for_ReactorReplayHandler;

public class when_replaying_a_reactor : Specification
{
    IReactorMediator _mediator;
    ReactorReplayHandler _handler;
    ObserverDetails _details;
    readonly List<bool> _successes = [];

    void Establish()
    {
        _mediator = Substitute.For<IReactorMediator>();
        _handler = new(_mediator);
        _details = new(new ObserverKey("reactor", "store", "namespace", EventSequenceId.Log), ObserverType.Reactor);
    }

    async Task Because()
    {
        _successes.Add((await _handler.BeginReplayFor(_details)).IsSuccess);
        _successes.Add((await _handler.ResumeReplayFor(_details)).IsSuccess);
        _successes.Add((await _handler.EndReplayFor(_details)).IsSuccess);
        _successes.Add((await _handler.BeginReplayPartitionFor(_details, (Key)"partition")).IsSuccess);
        _successes.Add((await _handler.EndReplayPartitionFor(_details, (Key)"partition")).IsSuccess);
    }

    [Fact] void should_accept_every_replay_operation() => _successes.ShouldContainOnly(true, true, true, true, true);
    [Fact] void should_notify_the_beginning() => _mediator.Received(1).OnBeginReplay(new ReactorId("reactor"), "store", "namespace");
    [Fact] void should_notify_the_end() => _mediator.Received(1).OnEndReplay(new ReactorId("reactor"), "store", "namespace");
    [Fact] void should_notify_the_partition_beginning() => _mediator.Received(1).OnBeginReplayPartition(new ReactorId("reactor"), "store", "namespace", (Key)"partition");
    [Fact] void should_notify_the_partition_end() => _mediator.Received(1).OnEndReplayPartition(new ReactorId("reactor"), "store", "namespace", (Key)"partition");
}
