// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationHandler.when_sending;

public class and_an_unapproved_host_is_used_in_enforce_mode : given.a_validation_handler
{
    void Establish()
    {
        Configure(new() { Validation = WebhookValidationMode.Enforce });
        CreateClient("https://unapproved.example");
    }

    Task Because() => Send();

    [Fact] void should_not_contact_the_receiver() => _transport.Calls.ShouldEqual(0);
    [Fact] void should_explain_the_rejection() => _error!.Message.ShouldContain("AllowedHosts");
}
