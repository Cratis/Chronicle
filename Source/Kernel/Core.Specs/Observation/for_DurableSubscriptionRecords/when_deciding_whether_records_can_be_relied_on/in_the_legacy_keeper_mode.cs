// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_deciding_whether_records_can_be_relied_on;

public class in_the_legacy_keeper_mode : given.durable_subscription_records
{
    void Establish()
    {
        _keeper = ClientKeeperMode.Legacy;
        Create();
    }

    [Fact] void should_be_relied_on() => _records.CanBeRelied.ShouldEqual(true);
    [Fact] void should_not_confirm_client_subscriptions() => _records.ConfirmsClientSubscriptions.ShouldEqual(false);
}
