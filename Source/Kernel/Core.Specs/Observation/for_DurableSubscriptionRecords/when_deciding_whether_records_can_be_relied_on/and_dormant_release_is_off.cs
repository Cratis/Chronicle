// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_deciding_whether_records_can_be_relied_on;

public class and_dormant_release_is_off : given.durable_subscription_records
{
    void Establish()
    {
        _dormantAfter = 0;
        Create();
    }

    [Fact] void should_not_be_relied_on() => _records.CanBeRelied.ShouldEqual(false);
    [Fact] void should_not_confirm_client_subscriptions() => _records.ConfirmsClientSubscriptions.ShouldEqual(false);
}
