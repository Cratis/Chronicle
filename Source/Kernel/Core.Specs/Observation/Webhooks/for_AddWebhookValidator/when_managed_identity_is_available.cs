// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using FluentValidation.Results;

namespace Cratis.Chronicle.Observation.Webhooks.for_AddWebhookValidator;

public class when_managed_identity_is_available : given.a_managed_identity_validator
{
    ValidationResult _result;

    async Task Because() => _result = await _validator.ValidateAsync(_command);

    [Fact] void should_allow_a_system_assigned_identity() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_request_the_configured_scope() => _tokens.Received(1).AcquireToken(new ManagedIdentityAuthorization(ManagedIdentityClientId.NotSet, "api://receiver/.default"), Arg.Any<CancellationToken>());
}
