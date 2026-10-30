// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator;

public class when_the_managed_identity_client_is_not_allowed : given.a_managed_identity_validator
{
    ValidationResult _result;

    void Establish() => _command = _command with { ManagedIdentityClientId = "unapproved-client" };

    async Task Because() => _result = await _validator.ValidateAsync(_command);

    [Fact] void should_reject_registration() => _result.IsValid.ShouldBeFalse();
    [Fact] async Task should_not_request_a_token() => await _tokens.DidNotReceive().AcquireToken(Arg.Any<ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>());
}
