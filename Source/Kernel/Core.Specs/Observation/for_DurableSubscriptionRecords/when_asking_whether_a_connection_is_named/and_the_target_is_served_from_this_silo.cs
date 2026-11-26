// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_DurableSubscriptionRecords.when_asking_whether_a_connection_is_named;

public class and_the_target_is_served_from_this_silo : given.durable_subscription_records
{
    bool _named;

    void Establish()
    {
        var thisSilo = Orleans.Runtime.SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1);
        _thisSilo = thisSilo;
        Create();
        _stored[_observer.ObserverId] = new(
            ObserverSubscriptionKind.ClientReactor,
            false,
            null,
            [new("the-connection", thisSilo.ToParsableString())]);
    }

    async Task Because() => _named = await _records.Names(_observer, new ConnectionId("the-connection"));

    [Fact] void should_name_the_client() => _named.ShouldBeTrue();
}
