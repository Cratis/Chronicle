// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_history;

public class and_storage_substitutes_a_missing_generation : given.an_event_with_protected_history
{
    IEventTypesStorage _types;
    Exception? _failure;

    void Establish()
    {
        _schemas.Remove(new("person", 2));
        _types = Substitute.For<IEventTypesStorage>();
        var registered = _schemas.Values.ToArray();

        // Match the converters: label the first schema with the absent requested generation.
        _types.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(call => call.Arg<IEnumerable<EventType>>()
            .Select(type => registered.FirstOrDefault(schema => schema.Type == type) ?? registered[0] with { Type = type }).ToArray());
        _types.GetAllGenerationsForEventType(Arg.Any<EventType>()).Returns(registered);
        _types.GetDefinition("person").Returns(new EventTypeDefinition("person", EventTypeOwner.Client, false, registered.Select(schema => new EventTypeGenerationDefinition(schema.Type.Generation, schema.Schema)), []));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.Returns(_types);
        _compliance = new(_metadata, new ExpandoObjectConverter(Substitute.For<ITypeFormats>()), new EventTypeSchemaCache(storage));
        _schemas.Clear();
    }

    async Task Because()
    {
        try
        {
            await _types.EnsureSchemasFor(_schemas, [_event]);
            _result = (await _compliance.Release([_event], _schemas)).ToArray();
        }
        catch (Exception exception)
        {
            _failure = exception;
        }
    }

    [Fact] void should_not_fail_the_read() => _failure.ShouldBeNull();
    [Fact] void should_release_the_current_generation() => ((IDictionary<string, object?>)_result.Single().Content)["name3"].ShouldEqual(_releasedValue);
    [Fact] void should_release_registered_history() => Value(_result.Single().GenerationalContent[1], 1).ShouldEqual(_releasedValue);
    [Fact] void should_release_the_original() => Value(_result.Single().OriginalContent!, 1).ShouldEqual(_releasedValue);
    [Fact] void should_preserve_the_missing_generation() => _result.Single().GenerationalContent[2].ShouldEqual(_event.GenerationalContent[2]);
    [Fact] void should_preserve_the_missing_revision_generation() => _result.Single().Revisions.Single().Content.ShouldEqual(_event.Revisions.Single().Content);
    [Fact] void should_not_change_stored_history() => _event.GenerationalContent[2].ShouldEqual(Content(2));
}
