// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.for_EventTypes.when_adopting_discovery;

public class from_a_registry_over_an_equal_catalog : given.all_dependencies
{
    [EventType("adopted-event")]
    record AdoptedEvent(string Message);

    EventTypes _source;
    EventTypes _subject;

    async Task Establish()
    {
        _clientArtifacts.EventTypes.Returns([typeof(AdoptedEvent)]);
        _schemaGenerator.Generate(Arg.Any<Type>()).Returns(_ => new JsonSchema { Title = "original" });
        _source = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
        await _source.Discover();
        _schemaGenerator.ClearReceivedCalls();
        _subject = new EventTypes(_eventStore, _schemaGenerator, _clientArtifacts, _eventTypeMigrators);
    }

    async Task Because()
    {
        await _subject.AdoptDiscovery(_source.CaptureDiscovery());
        _subject.GetSchemaFor("adopted-event").Title = "changed by the adopter";
    }

    [Fact] void should_know_the_event_type() => _subject.HasFor(typeof(AdoptedEvent)).ShouldBeTrue();
    [Fact] void should_resolve_the_clr_type() => _subject.GetClrTypeFor("adopted-event").ShouldEqual(typeof(AdoptedEvent));
    [Fact] void should_not_generate_schemas_again() => _schemaGenerator.DidNotReceive().Generate(Arg.Any<Type>());
    [Fact] void should_not_share_schemas_with_the_source() => _source.GetSchemaFor("adopted-event").Title.ShouldEqual("original");
}
