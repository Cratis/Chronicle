// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

public class and_a_stored_event_type_gets_an_unreadable_schema : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string UnreadableSchema = "{ this is not a schema";
    IReadOnlyList<EventTypeCheckResult> _results;

    void Establish() => StoredEventTypes(StoredEventType("stored-event", (1, StoredSchema)));

    async Task Because() => _results = await _subject.Check(
        "test-store",
        [
            new EventTypeRegistration { Type = new() { Id = "stored-event", Generation = 1 }, Schema = UnreadableSchema, Generations = { new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = UnreadableSchema } } },
            new EventTypeRegistration { Type = new() { Id = "fine-event", Generation = 1 }, Schema = StoredSchema }
        ],
        _storage);

    [Fact] void should_report_every_registration() => _results.Count.ShouldEqual(2);
    [Fact] void should_reject_the_unreadable_one() => _results[0].Accepted.ShouldBeFalse();
    [Fact] void should_name_the_unreadable_event_type() => _results[0].EventType.ShouldEqual("stored-event");
    [Fact] void should_give_a_reason() => _results[0].Reason.ShouldContain("could not be processed");
    [Fact] void should_still_accept_the_other_one() => _results[1].Accepted.ShouldBeTrue();
}
