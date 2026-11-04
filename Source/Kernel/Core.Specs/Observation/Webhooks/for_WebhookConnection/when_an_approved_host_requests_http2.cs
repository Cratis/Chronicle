// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection;

public class when_an_approved_host_requests_http2 : given.a_pooled_connection
{
    void Establish() => _version = HttpVersion.Version20;

    async Task Because() => await SendAcrossStores();

    [Fact] void should_not_reuse_the_approved_connection() => _connections.ShouldEqual(2);
    [Fact] void should_reject_the_unapproved_store() => _exception.GetBaseException().GetType().ShouldEqual(typeof(Validation.WebhookTargetRejected));
}
