// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_acquiring_token;

public class and_the_authority_is_rejected : given.an_oauth_client
{
    WebhookTargetRejected _error;

    void Establish()
    {
        _policy.Evaluate("https://authority.example/.well-known/openid-configuration", Arg.Any<Concepts.EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(new WebhookTargetValidation(false, "Authority is not approved."));
    }

    async Task Because()
    {
        try
        {
            await _client.AcquireToken(new OAuthAuthorization("https://authority.example", "client", "secret"));
        }
        catch (WebhookTargetRejected error)
        {
            _error = error;
        }
    }

    [Fact] void should_fail_with_a_local_policy_reason() => _error.ShouldNotBeNull();
    [Fact] void should_not_send_a_request_to_the_rejected_target() => _messageHandler.Requests.Count.ShouldEqual(0);
}
