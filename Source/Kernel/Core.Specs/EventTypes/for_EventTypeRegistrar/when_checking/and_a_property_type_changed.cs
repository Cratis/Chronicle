// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

public class and_a_property_type_changed : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string ChangedSchema = """{"type":"object","properties":{"name":{"type":"integer"}}}""";
    IReadOnlyList<EventTypeCheckResult> _results;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, StoredSchema)));

    async Task Because() => _results = await _subject.Check(
        "test-store",
        [new EventTypeRegistration { Type = new() { Id = "some-event", Generation = 1 }, Schema = ChangedSchema, Generations = { new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = ChangedSchema } } }],
        _storage);

    [Fact] void should_reject_it() => _results.Single().Accepted.ShouldBeFalse();
    [Fact] void should_name_the_event_type() => _results.Single().EventType.ShouldEqual("some-event");
    [Fact] void should_name_the_generation() => _results.Single().Generation.ShouldEqual(1u);
    [Fact] void should_name_the_keyword() => _results.Single().Keyword.ShouldEqual("type");
    [Fact] void should_name_the_path() => _results.Single().Path.ShouldEqual("$/properties/name");
    [Fact] void should_give_the_reason_registering_throws() => _results.Single().Reason.ShouldEqual(new EventTypeSchemaChanged("some-event", 1).Message);
}
