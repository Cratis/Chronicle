// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Contracts.Security;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator;

public class when_the_receiver_is_not_allowed_in_warn_mode : given.a_managed_identity_validator
{
    ValidationResult _managedIdentity;
    ValidationResult _none;

    void Establish() => _command = _command with { Url = "https://unapproved.example" };

    async Task Because()
    {
        _managedIdentity = await _validator.ValidateAsync(_command);
        _none = await _validator.ValidateAsync(_command with { AuthorizationType = AuthorizationType.None });
    }

    [Fact] void should_reject_managed_identity() => _managedIdentity.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_warn_mode_for_other_authorization_types() => _none.IsValid.ShouldBeTrue();
    [Fact] async Task should_not_request_a_token() => await _tokens.DidNotReceive().AcquireToken(Arg.Any<ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>());
}
