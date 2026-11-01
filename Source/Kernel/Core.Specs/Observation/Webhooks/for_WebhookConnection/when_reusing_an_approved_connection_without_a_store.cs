// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection;

public class when_reusing_an_approved_connection_without_a_store : given.a_pooled_connection
{
    void Establish() => _secondStore = null;

    async Task Because() => await SendAcrossStores();

    [Fact] void should_open_a_new_connection() => _connections.ShouldEqual(2);
    [Fact] void should_reject_the_unapproved_request() => _exception.GetBaseException().GetType().ShouldEqual(typeof(Validation.WebhookTargetRejected));
}
