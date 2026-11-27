// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag;

public class and_the_subscription_is_lost_after_the_sample : given.an_observer_publishing_a_stalled_sample
{
    async Task Because() => await _observer.Unsubscribe();

    [Fact] void should_withdraw_the_sample_at_once() => Published.ShouldBeNull();
}
