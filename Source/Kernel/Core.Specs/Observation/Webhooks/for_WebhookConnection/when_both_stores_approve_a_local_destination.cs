// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection;

public class when_both_stores_approve_a_local_destination : given.a_pooled_connection
{
    void Establish() => _approveSecondStore = true;

    async Task Because() => await SendAcrossStores();

    [Fact] void should_use_separate_connections() => _connections.ShouldEqual(2);
    [Fact] void should_allow_both_requests() => _exception.ShouldBeNull();
}
