// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_durable_subscription;

public class and_reading_the_record_fails : given.a_confirmation_from_the_durable_subscription
{
    void Establish() => _dormantObservers.HoldsClient(Arg.Any<ObserverKey>(), _key.ConnectionId).Returns(Task.FromException<bool>(new InvalidOperationException("storage is down")));

    Task Because() => Confirm();

    [Fact] void should_ask_the_client_grain_instead() => _grainCalls.ShouldEqual(1);
    [Fact] void should_report_what_the_grain_answered() => _intact.ShouldBeTrue();
}
