// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_SetInitialAdminPassword.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<SetInitialAdminPassword> _scenario = ChronicleCommandScenario.For<SetInitialAdminPassword>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.System.Users.GetById(Arg.Any<Concepts.Security.UserId>()).Returns(new Storage.Security.User());
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new SetInitialAdminPassword(Guid.NewGuid(), "some-password-123", "some-password-123"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
