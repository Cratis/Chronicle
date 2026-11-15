// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_nothing_was_returned_for_the_partition_before : given.a_state_caching_observation_stream
{
    void Establish()
    {
        _ = Deliver(null);
        ReplyWithState(StateOf("first", 1), "other-partition");
    }

    void Because() => _ = Deliver(StateOf("first", 1));

    [Fact] void should_not_mark_the_state_as_unchanged() => Unchanged(1).ShouldBeFalse();
    [Fact] void should_send_the_state() => InitialStateSent(1).ShouldNotBeNull();
}
