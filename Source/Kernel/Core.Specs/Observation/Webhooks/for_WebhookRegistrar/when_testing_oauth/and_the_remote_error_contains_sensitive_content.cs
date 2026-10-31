// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_testing_oauth;

public class and_the_remote_error_contains_sensitive_content : given.a_webhook_registrar
{
    WebhookTestResult _result;

    void Establish() => _oauthClient.AcquireToken(Arg.Any<OAuthAuthorization>(), Arg.Any<EventStoreName?>())
        .Returns(Task.FromException<AccessTokenInfo>(new HttpRequestException("remote-secret-body")));

    async Task Because() => _result = await _registrar.TestOAuthAuthorization("https://authority.example", "client", "secret");

    [Fact] void should_fail_the_probe() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_echo_remote_content() => _result.ErrorMessage.ShouldEqual("Unable to acquire OAuth access token.");
}
