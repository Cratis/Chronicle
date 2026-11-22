// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes.for_RegisterSingleEventType.when_registering;

public class and_a_generation_was_concurrently_registered : Specification
{
    IStorage _storage;
    IEventTypesCacheClient _cache;
    IPatternCapture _capture;
    Exception? _error;

    void Establish()
    {
        var types = Substitute.For<IEventTypesStorage>();
        types.RegisterIfAbsent(Arg.Any<EventTypeToRegister>()).Returns(new EventTypeRegistrationResult(false, [], EventTypeGeneration.First));
        var store = Substitute.For<IEventStoreStorage>();
        store.EventTypes.Returns(types);
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        _cache = Substitute.For<IEventTypesCacheClient>();
        _capture = Substitute.For<IPatternCapture>();
    }

    async Task Because() => _error = await Catch.Exception(() => new RegisterSingleEventType(
        "example-store", new() { Type = new() { Id = "synthetic-single-event", Generation = 1 }, Schema = "{}" })
        .Handle(_storage, _cache, _capture));

    [Fact] void should_report_the_existing_schema_conflict() => _error.ShouldBeOfExactType<EventTypeSchemaChanged>();
    [Fact] void should_not_notify_the_cache() => _cache.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_capture_patterns() => _capture.ReceivedCalls().ShouldBeEmpty();
}
