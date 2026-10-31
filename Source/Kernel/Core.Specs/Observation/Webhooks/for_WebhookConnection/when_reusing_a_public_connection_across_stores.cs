// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection;

public class when_reusing_a_public_connection_across_stores : given.a_pooled_connection
{
    void Establish()
    {
        _address = IPAddress.Parse("93.184.216.34");
        _approveFirstStore = false;
    }

    async Task Because() => await SendAcrossStores();

    [Fact] void should_reuse_the_public_connection() => _connections.ShouldEqual(1);
    [Fact] void should_allow_the_unlisted_public_destination() => _exception.ShouldBeNull();
}
