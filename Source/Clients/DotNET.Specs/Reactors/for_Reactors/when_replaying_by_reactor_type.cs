// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_replaying_by_reactor_type : given.all_dependencies
{
    readonly ReactorId _reactorId = "73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4";
    ReactorHandler _handler;

    void Establish()
    {
        _handler = Substitute.For<ReactorHandler>();
        _handler.Id.Returns(_reactorId);

        _handlers[typeof(MyReactor)] = _handler;

        _observers.Replay(Arg.Any<Replay>()).Returns(Task.CompletedTask);
    }

    async Task Because() => await _reactors.Replay<MyReactor>();

    [Fact]
    void should_call_replay_with_correct_parameters() =>
        _observers
            .Received(1)
            .Replay(Arg.Is<Replay>(r =>
                r.EventStore == _eventStore.Name.Value &&
                r.Namespace == _eventStore.Namespace.Value &&
                r.ObserverId == _reactorId.Value &&
                r.EventSequenceId == string.Empty));

    class MyReactor : IReactor;
}
