// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_Append.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<Append> _scenario = ChronicleCommandScenario.For<Append>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new Append(
        "some-event-store",
        "some-namespace",
        "event-log",
        "some-event-source",
        "Default",
        "All",
        "Default",
        new EventType("SomeEvent", 1, false),
        new JsonObject()));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
