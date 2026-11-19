// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_durable_subscription;

public class when_the_observer_is_unsubscribed : given.a_client_observer
{
    bool _recordedBefore;

    async Task Establish()
    {
        await SubscribeClient();
        _recordedBefore = Recorded is not null;
    }

    async Task Because() => await _observer.Unsubscribe();

    [Fact] void should_have_recorded_the_subscription_before() => _recordedBefore.ShouldBeTrue();
    [Fact] void should_clear_the_durable_subscription() => Recorded.ShouldBeNull();
}
