// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_asking_whether_a_connection_is_named;

public class and_the_observer_has_no_record : given.durable_subscription_records
{
    bool _named;

    void Establish()
    {
        Create();
        _stored.Clear();
    }

    async Task Because() => _named = await _records.Names(_observer, new ConnectionId("the-connection"));

    [Fact] void should_not_name_it() => _named.ShouldBeFalse();
}
