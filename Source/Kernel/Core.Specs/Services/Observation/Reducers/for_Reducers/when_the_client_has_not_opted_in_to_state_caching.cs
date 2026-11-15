// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_the_client_has_not_opted_in_to_state_caching : given.a_state_caching_observation_stream
{
    protected override StateCaching? StateCaching => null;

    void Establish()
    {
        _ = Deliver(null);
        ReplyWithState(StateOf("first", 1));
    }

    void Because() => _ = Deliver(StateOf("first", 1));

    [Fact] void should_not_mark_the_state_as_unchanged() => Unchanged(1).ShouldBeFalse();
    [Fact] void should_send_the_state() => InitialStateSent(1).ShouldNotBeNull();
}
