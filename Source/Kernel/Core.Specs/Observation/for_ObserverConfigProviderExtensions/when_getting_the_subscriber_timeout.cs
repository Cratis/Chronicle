// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_ObserverConfigProviderExtensions;

public class when_getting_the_subscriber_timeout : Specification
{
    static readonly ObserverKey _observerKey = new("observer-id", "event-store", "namespace", EventSequenceId.Log);

    IConfigurationForObserverProvider _provider;

    void Establish() => _provider = Substitute.For<IConfigurationForObserverProvider>();

    [Fact]
    async Task should_take_the_configured_seconds()
    {
        _provider.GetFor(Arg.Any<string>()).Returns(new Observers { SubscriberTimeout = 42 });
        (await _provider.GetSubscriberTimeoutForObserver(_observerKey)).ShouldEqual(TimeSpan.FromSeconds(42));
    }

    [Fact]
    async Task should_read_zero_as_waiting_indefinitely()
    {
        _provider.GetFor(Arg.Any<string>()).Returns(new Observers { SubscriberTimeout = 0 });
        (await _provider.GetSubscriberTimeoutForObserver(_observerKey)).ShouldEqual(TimeSpan.Zero);
    }

    [Fact]
    async Task should_read_a_negative_configuration_as_waiting_indefinitely()
    {
        _provider.GetFor(Arg.Any<string>()).Returns(new Observers { SubscriberTimeout = -1 });
        (await _provider.GetSubscriberTimeoutForObserver(_observerKey)).ShouldEqual(TimeSpan.Zero);
    }
}
