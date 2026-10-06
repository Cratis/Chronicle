// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_releasing_compliance;

public class and_the_stored_type_is_marked_as_a_tombstone : Events.for_EventCompliance.given.all_dependencies
{
    IStorage _storage;
    Concepts.Events.AppendedEvent _event;
    IEnumerable<Concepts.Events.AppendedEvent> _result;

    void Establish()
    {
        dynamic content = new ExpandoObject();
        content.name = "encrypted-name";
        _event = new Concepts.Events.AppendedEvent(
            Concepts.Events.EventContext.Empty with { EventType = SomeEventType, Subject = new Subject(SubjectValue) },
            content);

        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var eventTypes = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.EventTypes.Returns(eventTypes);
        eventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns([
            new EventTypeSchema(SomeEventType with { Tombstone = true }, EventTypeOwner.Client, EventTypeSource.Code, _schemaWithPii)
        ]);
    }

    async Task Because() => _result = await EventSequenceQuerying.ReleaseCompliance([_event], _storage, "test-store", _compliance);

    [Fact] void should_release_the_pii_content() => ((IDictionary<string, object?>)_result.Single().Content)["name"].ShouldEqual("decrypted-name");
    [Fact] void should_preserve_the_event_type_without_the_registration_marker() => _result.Single().Context.EventType.ShouldEqual(SomeEventType);
}
