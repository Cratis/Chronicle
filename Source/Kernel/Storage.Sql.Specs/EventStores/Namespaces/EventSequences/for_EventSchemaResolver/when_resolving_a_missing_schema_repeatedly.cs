// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSchemaResolver;

public class when_resolving_a_missing_schema_repeatedly : Specification
{
    IEventTypesStorage _storage;
    EventSchemaResolver _resolver;
    JsonSchema _read;
    readonly EventType _type = new("unknown", EventTypeGeneration.First);

    void Establish()
    {
        _storage = Substitute.For<IEventTypesStorage>();
        _storage.GetFor(_type.Id, _type.Generation).Returns(Task.FromException<Concepts.EventTypes.EventTypeSchema>(new UnknownEventType(EventStoreName.NotSet, _type.Id)));
        _resolver = new(_storage);
    }

    async Task Because()
    {
        await _resolver.GetFor(_type);
        _read = await _resolver.GetFor(_type);
    }

    [Fact] void should_use_the_raw_content_fallback() => _read.ShouldBeNull();
    [Fact] void should_lookup_the_missing_schema_once() => _storage.Received(1).GetFor(_type.Id, _type.Generation);
    [Fact] void should_not_probe_for_existence() => _storage.DidNotReceive().HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>());
}
