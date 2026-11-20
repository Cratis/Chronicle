// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_waking_from_dormancy;

/// <summary>
/// A silo of an older release joined while the observer was dormant: it behaves as it did before it could be released.
/// </summary>
public class when_the_floor_is_not_reached_any_more : given.a_dormant_client_observer
{
    protected override bool FloorIsReachedWhenWaking => false;

    void Establish() => Append(EventAt(0), EventAt(1));

    async Task Because() => await Wake();

    [Fact] void should_not_restore_the_subscription_from_the_record() => _delivered.Count.ShouldEqual(0);
    [Fact] async Task should_not_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
