// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverDeliveryRuns;

public class when_no_target_declares_the_first_event : given.a_controlled_subscription
{
    ObserverSubscriberTarget? _selected;

    void Establish() => _subscription = _subscription with { Targets = [_old] };

    void Because() => _selected = ObserverDeliveryRuns.Select(_subscription, "partition", Event(_e, 10), _selector);

    [Fact] void should_report_no_capable_target() => _selected.ShouldBeNull();
    [Fact] void should_not_fall_back_to_an_incapable_target() => _selector.DidNotReceive().Select(Arg.Any<ObserverSubscription>(), Arg.Any<Cratis.Chronicle.Concepts.Keys.Key>());
}
