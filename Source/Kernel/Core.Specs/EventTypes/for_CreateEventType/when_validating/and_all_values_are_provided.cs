// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventTypes.for_CreateEventType.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<CreateEventType> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new CreateEventType("some-event-store", "SomeEvent"));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
