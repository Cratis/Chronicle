// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventTypes.for_RegisterSingleEventType.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<RegisterSingleEventType> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new RegisterSingleEventType("some-event-store", new EventTypeRegistration { Type = new EventType { Id = "SomeEvent" }, Schema = "{}" }));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
