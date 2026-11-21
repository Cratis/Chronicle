// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_asking_whether_a_connection_is_named;

public class and_the_record_has_it_as_a_target : given.durable_subscription_records
{
    bool _named;
    bool _other;

    void Establish() => Create();

    async Task Because()
    {
        _named = await _records.Names(_observer, new ConnectionId("the-connection"));
        _other = await _records.Names(_observer, new ConnectionId("another-connection"));
    }

    [Fact] void should_name_the_target() => _named.ShouldBeTrue();
    [Fact] void should_not_name_another_connection() => _other.ShouldBeFalse();
}
