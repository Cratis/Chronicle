// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookValidationHandler.when_sending;

public class and_the_receiver_redirects : given.a_validation_handler
{
    void Establish()
    {
        Configure(new());
        CreateClient("https://receiver.example");
        _transport.StatusCode = System.Net.HttpStatusCode.TemporaryRedirect;
    }

    Task Because() => Send();

    [Fact] void should_not_follow_the_redirect() => _transport.Calls.ShouldEqual(1);
    [Fact] void should_explain_the_failure() => _error!.Message.ShouldContain("redirects are not allowed");
}
