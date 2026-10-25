// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events.for_EventCompliance.given;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_releasing_compliance;

public class and_the_event_has_protected_history : an_event_with_protected_history
{
    IStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventTypes = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.Returns(eventTypes);
        eventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>())
            .Returns(call => Task.FromResult<IEnumerable<EventTypeSchema>>(call.Arg<IEnumerable<Concepts.Events.EventType>>().Select(type => _schemas[type]).ToArray()));
    }

    async Task Because() => _result = (await EventSequenceQuerying.ReleaseCompliance([_event], _storage, _event.Context.EventStore, _compliance)).ToArray();

    [Fact] void should_release_all_stored_generations() => _result[0].GenerationalContent.ShouldEachConformTo(item => Value(item.Value, item.Key) == "released");
    [Fact] void should_release_original_content() => Value(_result[0].OriginalContent, 1).ShouldEqual("released");
    [Fact] void should_release_revision_content() => Value(_result[0].Revisions.Single().Content, 2).ShouldEqual("released");
}
