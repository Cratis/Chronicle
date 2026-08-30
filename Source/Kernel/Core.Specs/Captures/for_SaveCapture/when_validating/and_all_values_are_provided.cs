// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Captures.for_SaveCapture.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<SaveCapture> _scenario = ChronicleCommandScenario.For<SaveCapture>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new SaveCapture("some-event-store", Guid.NewGuid(), "some declaration"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
