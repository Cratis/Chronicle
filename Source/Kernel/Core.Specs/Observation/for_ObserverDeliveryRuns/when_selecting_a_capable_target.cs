// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDeliveryRuns;

public class when_selecting_a_capable_target : given.a_controlled_subscription
{
    ObserverSubscriberTarget _selected;

    void Because() => _selected = ObserverDeliveryRuns.Select(_subscription, "partition", Event(_e, 10), _selector)!;

    [Fact] void should_select_the_target_that_declares_the_event_type() => _selected.ShouldBeSame(_new);
    [Fact] void should_not_include_an_incapable_target_in_fan_out() => _selector.Received(1).Select(
        Arg.Is<ObserverSubscription>(subscription => subscription.Targets.Count == 1 && subscription.Targets[0] == _new), "partition");
}
