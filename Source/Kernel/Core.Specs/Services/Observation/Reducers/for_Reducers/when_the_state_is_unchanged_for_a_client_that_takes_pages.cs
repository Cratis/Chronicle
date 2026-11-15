// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_the_state_is_unchanged_for_a_client_that_takes_pages : given.a_paged_state_caching_observation_stream
{
    void Establish()
    {
        _ = Deliver(null);
        ReplyWithState(StateOf("first", 1));
    }

    void Because() => _ = Deliver(StateOf("first", 1));

    [Fact] void should_mark_the_partition_state_as_unchanged() => Unchanged(1).ShouldBeTrue();
    [Fact] void should_not_send_the_state() => InitialStateSent(1).ShouldBeNull();
    [Fact] void should_not_use_the_single_partition_fields() => _deliveries[1].InitialStateUnchanged.ShouldBeFalse();
}
