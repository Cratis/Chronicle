// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_adding_a_property_in_nested_definitions : given.additive_evolution_enabled
{
    const string Previous = """{"type":"object","properties":{"address":{"$ref":"#/$defs/address"}},"$defs":{"address":{"type":"object","properties":{"city":{"type":"string"}}}}}""";
    const string Addition = """{"type":"object","properties":{"address":{"$ref":"#/$defs/address"}},"$defs":{"address":{"type":"object","properties":{"city":{"type":"string"},"zip":{"type":"integer"}}}}}""";
    EventTypeCheckResult _check;

    void Establish() => _definition = StoredEventType("some-event", (1, Previous));

    async Task Because()
    {
        _check = (await _subject.Check("test-store", [Registration(Addition)], _storage)).Single();
        await Register(Addition);
    }

    [Fact] void should_accept_the_addition() => _check.Accepted.ShouldBeTrue();
    [Fact] void should_report_additive() => _check.Classification.ShouldEqual("Additive");
    [Fact] void should_keep_the_nested_addition() => _definition.Generations.Single().Schema.ToJson().ShouldContain("zip");
    [Fact] void should_conditionally_replace_the_existing_generation() => _eventTypesStorage.Received(1).TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
}
