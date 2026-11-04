// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_ObserverConfigProviderExtensions;

public class when_getting_the_subscriber_timeout_from_configuration : Specification
{
    [Fact] void should_take_the_configured_seconds() => new Observers { SubscriberTimeout = 42 }.GetSubscriberTimeout().ShouldEqual(TimeSpan.FromSeconds(42));
    [Fact] void should_read_zero_as_waiting_indefinitely() => new Observers { SubscriberTimeout = 0 }.GetSubscriberTimeout().ShouldEqual(TimeSpan.Zero);
    [Fact] void should_read_a_negative_configuration_as_waiting_indefinitely() => new Observers { SubscriberTimeout = -1 }.GetSubscriberTimeout().ShouldEqual(TimeSpan.Zero);
}
