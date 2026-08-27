// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_RemoveUser.when_validating;

/// <summary>
/// RemoveUser references an existing user rather than creating one, so its UserId property gets the cross-cutting
/// UserIdValidator existence check - unlike AddUser's own UserId, which opts out because that command's whole
/// purpose is to create the user.
/// </summary>
public class and_the_user_does_not_exist : Specification
{
    readonly CommandScenario<RemoveUser> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.System.Users.GetById(Arg.Any<Concepts.Security.UserId>()).Returns((Storage.Security.User?)null);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new RemoveUser(Guid.NewGuid()));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
