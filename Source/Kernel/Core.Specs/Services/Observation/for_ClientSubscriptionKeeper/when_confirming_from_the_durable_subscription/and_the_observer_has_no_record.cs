// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.when_confirming_from_the_durable_subscription;

public class and_the_observer_has_no_record : given.a_confirmation_from_the_durable_subscription
{
    void Establish()
    {
        RecordNamesTheClient(false);
        _grainAnswer = false;
    }

    Task Because() => Confirm();

    [Fact] void should_ask_the_client_grain() => _grainCalls.ShouldEqual(1);
    [Fact] void should_report_what_the_grain_answered() => _intact.ShouldBeFalse();
}
