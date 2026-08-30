// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Chronicle.Security.for_AddUser.when_validating;

public class and_email_is_not_a_valid_address : Specification
{
    readonly CommandScenario<AddUser> _scenario = ChronicleCommandScenario.For<AddUser>();
    CommandResult _result;

    async Task Because() => _result = await _scenario.Validate(new AddUser(Guid.NewGuid(), "some-user", "not-an-email-address", "some-password-123"));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
