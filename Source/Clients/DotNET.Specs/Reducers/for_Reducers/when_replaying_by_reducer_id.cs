// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_replaying_by_reducer_id : given.all_dependencies
{
    readonly ReducerId _reducerId = "83c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d5";
    
    void Establish()
    {
        _observers.Replay(Arg.Any<Replay>()).Returns(Task.CompletedTask);
    }

    async Task Because() => await _reducers.Replay(_reducerId);

    [Fact] void should_call_replay_with_correct_parameters() =>
        _observers
            .Received(1)
            .Replay(Arg.Is<Replay>(r => 
                r.EventStore == _eventStore.Name.Value &&
                r.Namespace == _eventStore.Namespace.Value &&
                r.ObserverId == _reducerId.Value &&
                r.EventSequenceId == string.Empty));
}