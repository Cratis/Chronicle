// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_skipping_validation_for_a_breaking_schema_change : given.additive_evolution_enabled
{
    const string Renamed = """{"type":"object","properties":{"fullName":{"type":"string"}}}""";
    EventTypeToRegister _registration;

    void Establish() => _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns(call =>
    {
        _registration = call.Arg<IEnumerable<EventTypeToRegister>>().Single();
        return [];
    });

    Task Because() => _subject.Register("test-store", [Registration(Renamed)], true, _storage, _eventTypesCacheClient, _patternCapture);

    [Fact] void should_send_the_generation_to_unconditional_registration() => _registration.Definition.Generations.Single().Generation.Value.ShouldEqual(1u);
    [Fact] void should_accept_the_renamed_property() => _registration.Definition.Generations.Single().Schema.Properties.Keys.ShouldContain("fullName");
    [Fact] void should_not_retain_the_old_property() => _registration.Definition.Generations.Single().Schema.Properties.Keys.ShouldNotContain("name");
    [Fact] void should_not_conditionally_evolve_the_schema() => _eventTypesStorage.DidNotReceive().TryReplaceSchema(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>(), Arg.Any<JsonSchema>());
}
