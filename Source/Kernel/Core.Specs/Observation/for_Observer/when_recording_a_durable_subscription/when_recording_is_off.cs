// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_a_durable_subscription;

public class when_recording_is_off : given.a_client_observer
{
    protected override DurableSubscriptionMode Mode => DurableSubscriptionMode.Off;

    async Task Because() => await SubscribeClient();

    [Fact] void should_not_record_a_durable_subscription() => Recorded.ShouldBeNull();
    [Fact] async Task should_still_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
