// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_ChangeUserPassword.when_validating;

public class and_required_values_are_missing : Specification
{
    readonly CommandScenario<ChangeUserPassword> _scenario = ChronicleCommandScenario.For<ChangeUserPassword>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.System.Users.GetById(Arg.Any<Concepts.Security.UserId>()).Returns(new Storage.Security.User());
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new ChangeUserPassword(Guid.Empty, string.Empty, string.Empty, string.Empty));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
