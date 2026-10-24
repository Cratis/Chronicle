// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator;

public class when_managed_identity_has_no_scope : given.a_managed_identity_validator
{
    ValidationResult _result;

    async Task Because() => _result = await _validator.ValidateAsync(_command with { ManagedIdentityScope = "" });

    [Fact] void should_require_the_scope() => _result.Errors.ShouldContain(_ => _.PropertyName == nameof(AddWebhook.ManagedIdentityScope));
    [Fact] void should_not_try_to_acquire_a_token() => _tokens.DidNotReceive().AcquireToken(Arg.Any<Concepts.Security.ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>());
}
