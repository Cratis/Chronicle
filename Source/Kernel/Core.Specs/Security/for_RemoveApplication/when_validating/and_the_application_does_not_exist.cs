// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_RemoveApplication.when_validating;

/// <summary>
/// RemoveApplication references an existing application rather than creating one, so its Id property gets the
/// cross-cutting ApplicationIdValidator existence check - unlike AddApplication's own Id, which opts out because
/// that command's whole purpose is to create the application.
/// </summary>
public class and_the_application_does_not_exist : Specification
{
    readonly CommandScenario<RemoveApplication> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.System.Applications.GetById(Arg.Any<Concepts.Security.ApplicationId>()).Returns((Storage.Security.Application?)null);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new RemoveApplication(Guid.NewGuid()));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
