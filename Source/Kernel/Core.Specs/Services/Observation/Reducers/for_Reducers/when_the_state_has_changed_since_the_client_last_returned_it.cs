// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_the_state_has_changed_since_the_client_last_returned_it : given.a_state_caching_observation_stream
{
    void Establish()
    {
        _ = Deliver(null);
        ReplyWithState(StateOf("first", 1));
    }

    void Because() => _ = Deliver(StateOf("first", 2));

    [Fact] void should_not_mark_the_state_as_unchanged() => Unchanged(1).ShouldBeFalse();
    [Fact] void should_send_the_state() => InitialStateSent(1).ShouldNotBeNull();
}
