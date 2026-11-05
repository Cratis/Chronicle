// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_the_state_is_unchanged_since_the_client_last_returned_it : given.a_state_caching_observation_stream
{
    void Establish()
    {
        _ = Deliver(null);
        ReplyWithState(StateOf("first", 1));
    }

    // The state read back from the sink has its properties in another order than the client returned them.
    void Because() => _ = Deliver(StateOf("first", 1, nameFirst: false));

    [Fact] void should_mark_the_state_as_unchanged() => Unchanged(1).ShouldBeTrue();
    [Fact] void should_not_send_the_state() => InitialStateSent(1).ShouldBeNull();
}
