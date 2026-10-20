// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_testing_a_webhook;

public class and_the_remote_error_contains_sensitive_content : given.a_webhook_registrar
{
    WebhookTestResult _result;

    void Establish() => _webhookMediator.OnNext(Arg.Any<Concepts.EventStoreName?>(), Arg.Any<WebhookTarget>(), Arg.Any<Concepts.Keys.Key>(), Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>())
        .Returns(Catch.Failed(new HttpRequestException("remote-secret-body")));

    async Task Because() => _result = await _registrar.TestWebhook(new Contracts.Observation.Webhooks.WebhookTarget { Url = "https://receiver.example" });

    [Fact] void should_fail_the_probe() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_echo_remote_content() => _result.ErrorMessage.ShouldEqual("Unable to connect to webhook endpoint.");
}
