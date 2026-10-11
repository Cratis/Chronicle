// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_using_an_explicit_reactor : Specification
{
    ReactorScenario _scenario;
    readonly List<string> _notes = [];
    readonly List<EventContext> _contexts = [];
    int _definitionCount;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ThingTouched)] };
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterReactor("notes", builder =>
        {
            _definitionCount++;
            builder.On<ThingTouched>(async (@event, context) =>
            {
                await Task.Run(() => _notes.Add(@event.Note));
                _contexts.Add(context);
            });
        });
        _scenario = new("notes", new Defaults(artifacts, null, options));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource("note-source").Events(new ThingTouched("first"));
        await _scenario.Given.ForEventSource("note-source").Events(new ThingTouched("second"));
    }

    [Fact] void should_await_the_registered_callbacks() => _notes.ShouldContainOnly("first", "second");
    [Fact] void should_build_the_definition_once() => _definitionCount.ShouldEqual(1);
    [Fact] void should_supply_the_source_id() => _contexts[0].EventSourceId.ShouldEqual(new EventSourceId("note-source"));
    [Fact] void should_number_events_across_given_calls() => _contexts[1].SequenceNumber.ShouldEqual(EventSequenceNumber.First + 1);
}
