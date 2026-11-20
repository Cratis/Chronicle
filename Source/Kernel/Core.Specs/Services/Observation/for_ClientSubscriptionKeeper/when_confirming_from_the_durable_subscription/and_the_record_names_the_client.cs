// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_durable_subscription;

public class and_the_record_names_the_client : given.a_confirmation_from_the_durable_subscription
{
    void Establish() => RecordNamesTheClient(true);

    Task Because() => Confirm();

    [Fact] void should_find_the_subscription_intact() => _intact.ShouldBeTrue();
    [Fact] void should_not_call_the_client_grain_that_would_wake_the_observer() => _grainCalls.ShouldEqual(0);
}
