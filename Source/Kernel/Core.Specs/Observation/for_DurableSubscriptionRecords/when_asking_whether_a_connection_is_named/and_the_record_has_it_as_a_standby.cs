// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_asking_whether_a_connection_is_named;

/// <summary>
/// A standby is a client the observer holds while it is dormant, so the keeper must find its stream intact.
/// </summary>
public class and_the_record_has_it_as_a_standby : given.durable_subscription_records
{
    bool _named;
    bool _other;

    void Establish()
    {
        Create();
        _stored[_observer.ObserverId] = new(
            ObserverSubscriptionKind.ClientReactor,
            false,
            null,
            [new("the-connection", "silo")],
            [new("the-standby", "silo", ["id+1+False"], true, null, false, false)]);
    }

    async Task Because()
    {
        _named = await _records.Names(_observer, new ConnectionId("the-standby"));
        _other = await _records.Names(_observer, new ConnectionId("another-connection"));
    }

    [Fact] void should_name_the_standby() => _named.ShouldBeTrue();
    [Fact] void should_not_name_another_connection() => _other.ShouldBeFalse();
}
