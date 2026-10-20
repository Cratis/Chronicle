// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_testing_oauth_authorization;

public class with_a_scope : given.a_webhook_registrar
{
    OAuthAuthorization _requested;
    WebhookTestResult _result;

    void Establish() =>
        _oauthClient.AcquireToken(Arg.Do<OAuthAuthorization>(_ => _requested = _), Arg.Any<Concepts.EventStoreName?>())
            .Returns(new AccessTokenInfo("the-token", DateTimeOffset.UtcNow.AddHours(1)));

    async Task Because() => _result = await _registrar.TestOAuthAuthorization("https://login.example.com/tenant", "client", "secret", "api://target/.default", "orders");

    [Fact] void should_request_the_scope() => _requested.Scope.ShouldEqual("api://target/.default");
    [Fact] async Task should_use_the_event_store() => await _oauthClient.Received(1).AcquireToken(_requested, (Concepts.EventStoreName)"orders");
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
}
