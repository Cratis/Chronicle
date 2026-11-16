// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue;

public class when_filtering_a_fully_matching_batch : given.subscription_matching
{
    void Establish() => _subscription = _subscription with { AllEventTypes = true };
    void Because() => Filter();

    [Fact] void should_share_the_input_batch() => ReferenceEquals(_result, _events).ShouldBeTrue();
    [Fact] void should_retain_every_event() => _result.Count.ShouldEqual(3);
}
