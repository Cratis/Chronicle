// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationHandler.when_sending;

public class and_an_unapproved_host_is_used_in_warn_mode : given.a_validation_handler
{
    void Establish()
    {
        Configure(new());
        CreateClient("http://unapproved.example");
    }

    Task Because() => Send();

    [Fact] void should_send_the_request() => _transport.Calls.ShouldEqual(1);
    [Fact] void should_not_block_delivery() => _error.ShouldBeNull();
}
