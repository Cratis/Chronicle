// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_only_a_nested_title_has_changed : given.all_dependencies
{
    const string StoredSchema = """{"type":"object","properties":{"key":{"type":"object","title":"OldKey","properties":{"id":{"type":"string"}}}}}""";
    const string IncomingSchema = """{"type":"object","properties":{"key":{"type":"object","title":"NewKey","properties":{"id":{"type":"string"}}}}}""";
    Exception _exception;

    void Establish() => StoredEventTypes(StoredEventType("some-event", (1, StoredSchema)));

    async Task Because() => _exception = await Catch.Exception(async () => await _subject.Register(
        "test-store",
        [new EventTypeRegistration
        {
            Type = new() { Id = "some-event", Generation = 1 },
            Schema = IncomingSchema,
            Generations = { new Contracts.Events.EventTypeGenerationDefinition { Generation = 1, Schema = IncomingSchema } }
        }],
        false,
        _storage,
        _eventTypesCacheClient,
        _patternCapture));

    [Fact] void should_accept_the_schema() => _exception.ShouldBeNull();
    [Fact] void should_not_invalidate_any_cache() => _eventTypesCacheClient.DidNotReceive().Invalidate(Arg.Any<EventStoreName>(), Arg.Any<EventTypeId>());
    [Fact] void should_not_resubscribe_pattern_capture() => _patternCapture.DidNotReceiveWithAnyArgs().SubscribeAcrossNamespaces(default!);
}
