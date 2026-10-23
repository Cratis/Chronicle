// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator;

public class when_managed_identity_cannot_acquire_a_token : given.a_managed_identity_validator
{
    ValidationResult _result;

    void Establish() => _tokens.AcquireToken(Arg.Any<Concepts.Security.ManagedIdentityAuthorization>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<AccessTokenInfo>(new ManagedIdentityNotAvailable()));

    async Task Because() => _result = await _validator.ValidateAsync(_command with { ManagedIdentityClientId = "identity" });

    [Fact] void should_reject_registration() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_token_failure() => _result.Errors.ShouldContain(_ => _.ErrorMessage == "Unable to acquire a valid managed identity token.");
    [Fact] void should_use_the_selected_identity() => _tokens.Received(1).AcquireToken(new Concepts.Security.ManagedIdentityAuthorization("identity", "api://receiver/.default"), Arg.Any<CancellationToken>());
}
