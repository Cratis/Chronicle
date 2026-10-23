// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator;

public class when_the_managed_identity_is_approved : given.a_managed_identity_validator
{
    ValidationResult _result;

    void Establish() => _command = _command with { ManagedIdentityClientId = "client", ManagedIdentityScope = "API://RECEIVER/.default" };

    async Task Because() => _result = await _validator.ValidateAsync(_command);

    [Fact] void should_accept_the_approved_scope_client_and_host() => _result.IsValid.ShouldBeTrue();
    [Fact] async Task should_request_the_token() => await _tokens.Received(1).AcquireToken(new ManagedIdentityAuthorization("client", "API://RECEIVER/.default"), Arg.Any<CancellationToken>());
}
