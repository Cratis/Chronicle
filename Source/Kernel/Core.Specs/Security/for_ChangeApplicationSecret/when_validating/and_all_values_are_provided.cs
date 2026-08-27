// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Security.for_ChangeApplicationSecret.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<ChangeApplicationSecret> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.System.Applications.GetById(Arg.Any<Concepts.Security.ApplicationId>()).Returns(new Storage.Security.Application());
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new ChangeApplicationSecret(Guid.NewGuid(), "some-secret"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
