// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword.when_validating;

public class and_the_setup_token_is_missing : Specification
{
    readonly CommandScenario<SetInitialAdminPassword> _scenario = ChronicleCommandScenario.For<SetInitialAdminPassword>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new SetInitialAdminPassword(Guid.NewGuid(), "some-password-123", "some-password-123", string.Empty));

    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
