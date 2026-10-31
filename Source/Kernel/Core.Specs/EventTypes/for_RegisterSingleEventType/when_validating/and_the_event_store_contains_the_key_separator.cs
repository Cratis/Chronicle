// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventTypes.for_RegisterSingleEventType.when_validating;

public class and_the_event_store_contains_the_key_separator : Specification
{
    readonly CommandScenario<RegisterSingleEventType> _scenario = ChronicleCommandScenario.For<RegisterSingleEventType>();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    async Task Because() => _result = await _scenario.Validate(new RegisterSingleEventType("x#System", new EventTypeRegistration { Type = new EventType { Id = "SomeEvent" }, Schema = "{}" }));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_event_store_member() => _result.ValidationResults.SelectMany(result => result.Members).ShouldContain("eventStore");
    [Fact] void should_report_the_rule() => _result.ValidationResults.Select(result => result.Message).ShouldContain("The value must not contain '#'.");
}
