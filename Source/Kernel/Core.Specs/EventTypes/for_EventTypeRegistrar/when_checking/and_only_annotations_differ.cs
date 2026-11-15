// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

public class and_only_annotations_differ : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string AnnotatedSchema = """{"type":"object","title":"Some","description":"d","$comment":"c","examples":[{"name":"x"}],"properties":{"name":{"type":"string","deprecated":true,"description":"The name","contentEncoding":"base64"}}}""";
    IReadOnlyList<EventTypeCheckResult> _results;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, StoredSchema)));

    async Task Because() => _results = await _subject.Check(
        "test-store",
        [new EventTypeRegistration { Type = new() { Id = "some-event", Generation = 1 }, Schema = AnnotatedSchema, Generations = { new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = AnnotatedSchema } } }],
        _storage);

    [Fact] void should_accept_it() => _results.Single().Accepted.ShouldBeTrue();
    [Fact] void should_have_no_reason() => _results.Single().Reason.ShouldBeEmpty();
}
