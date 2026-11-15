// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_the_previous_result_was_not_ok : given.a_state_caching_observation_stream
{
    void Establish()
    {
        _ = Deliver(null);
        ReplyWithState(StateOf("first", 1));
        _ = Deliver(StateOf("first", 1));
        ReplyWithState(StateOf("first", 1), observationState: ObservationState.Failed);
    }

    void Because() => _ = Deliver(StateOf("first", 1));

    [Fact] void should_have_sent_the_state_as_unchanged_before_the_failure() => Unchanged(1).ShouldBeTrue();
    [Fact] void should_not_mark_the_state_as_unchanged() => Unchanged(2).ShouldBeFalse();
    [Fact] void should_send_the_state() => InitialStateSent(2).ShouldNotBeNull();
}
