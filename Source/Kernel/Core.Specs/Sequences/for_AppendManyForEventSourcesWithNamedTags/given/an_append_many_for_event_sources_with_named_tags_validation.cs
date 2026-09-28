// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSourcesWithNamedTags.given;

public class an_append_many_for_event_sources_with_named_tags_validation : Specification
{
    protected readonly CommandScenario<AppendManyForEventSourcesWithNamedTags> _scenario = ChronicleCommandScenario.For<AppendManyForEventSourcesWithNamedTags>();
    protected CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
    }

    protected static AppendManyForEventSourcesWithNamedTags ValidCommand() => new(
        "some-event-store",
        "some-namespace",
        "event-log",
        [EventFor("some-event-source", new NamedTag("account", "one"))]);

    protected static EventForEventSourceIdWithNamedTags EventFor(string eventSourceId, params NamedTag[] namedTags) =>
        new(eventSourceId, "Default", "All", "Default", new EventType("SomeEvent", 1, false), "{}", null, namedTags);
}
