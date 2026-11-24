// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

public class when_an_event_store_subscription_is_woken_by_an_event : given.a_dormant_event_store_subscription_observer
{
    void Establish() => Append(EventAt(0));

    async Task Because()
    {
        await Wake();
        await _observer.RunWatchdogOnce();
    }

    [Fact] void should_deliver_the_event() => _delivered.Count.ShouldEqual(1);
    [Fact] void should_deliver_it_with_the_event_store_it_forwards_to() => (_contexts.Count > 0 && _contexts.TrueForAll(context => (context.Metadata as string) == TargetEventStore)).ShouldBeTrue();
}
