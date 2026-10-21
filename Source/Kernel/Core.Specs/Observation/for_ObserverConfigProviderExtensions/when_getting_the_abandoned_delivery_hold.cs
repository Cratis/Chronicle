// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_ObserverConfigProviderExtensions;

public class when_getting_the_abandoned_delivery_hold
{
    [Theory]
    [InlineData(30, 300, 300)]
    [InlineData(30, 1, 30)]
    [InlineData(30, 0, 0)]
    [InlineData(30, -1, 0)]
    [InlineData(0, 300, 0)]
    [InlineData(-1, 300, 0)]
    public async Task should_apply_the_timeout_floor_and_disable_rules(int timeout, int hold, int expected)
    {
        var provider = Substitute.For<IConfigurationForObserverProvider>();
        provider.GetFor(Arg.Any<string>()).Returns(new Observers { SubscriberTimeout = timeout, AbandonedDeliveryHold = hold });
        (await provider.GetAbandonedDeliveryHoldForObserver(new("observer", "store", "namespace", "sequence"))).ShouldEqual(TimeSpan.FromSeconds(expected));
    }

    [Fact]
    public void should_default_to_five_minutes() => new Observers().AbandonedDeliveryHold.ShouldEqual(300);
}
