// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection;

public class when_reusing_a_metadata_connection_across_stores_in_warn_mode : given.a_pooled_connection
{
    void Establish()
    {
        _address = IPAddress.Parse("169.254.169.254");
        _mode = WebhookValidationMode.Warn;
    }

    async Task Because() => await SendAcrossStores();

    [Fact] void should_open_a_new_connection() => _connections.ShouldEqual(2);
    [Fact] void should_reject_the_unapproved_store() => _exception.GetBaseException().GetType().ShouldEqual(typeof(WebhookTargetRejected));
}
