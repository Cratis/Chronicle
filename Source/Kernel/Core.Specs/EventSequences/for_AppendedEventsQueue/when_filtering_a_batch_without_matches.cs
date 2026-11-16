// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue;

public class when_filtering_a_batch_without_matches : given.subscription_matching
{
    void Establish() => _subscription = _subscription with { EventTypeIds = ["absent"] };
    void Because() => Filter();

    [Fact] void should_return_no_events() => _result.ShouldBeEmpty();
    [Fact] void should_not_modify_the_input() => _events.Length.ShouldEqual(3);
}
