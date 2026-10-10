// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSchemaResolver;

public class when_resolving_distinct_generations : Specification
{
    IEventTypesStorage _storage;
    EventSchemaResolver _resolver;
    JsonSchema _first;
    JsonSchema _second;
    JsonSchema _read;
    readonly EventType _firstType = new("amount", EventTypeGeneration.First);
    readonly EventType _secondType = new("amount", new EventTypeGeneration(2));

    void Establish()
    {
        _first = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number"}}}""");
        _second = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}""");
        _storage = Substitute.For<IEventTypesStorage>();
        _storage.GetFor(_firstType.Id, _firstType.Generation).Returns(new EventTypeSchema(_firstType, EventTypeOwner.Client, EventTypeSource.Code, _first));
        _storage.GetFor(_secondType.Id, _secondType.Generation).Returns(new EventTypeSchema(_secondType, EventTypeOwner.Client, EventTypeSource.Code, _second));
        _resolver = new(_storage);
    }

    async Task Because()
    {
        await _resolver.GetFor(_firstType);
        await _resolver.GetFor(_secondType);
        await _resolver.GetFor(_firstType);
        _read = await _resolver.GetFor(_secondType);
    }

    [Fact] void should_preserve_each_generations_schema() => _read.ShouldEqual(_second);
    [Fact] void should_lookup_the_first_generation_once() => _storage.Received(1).GetFor(_firstType.Id, _firstType.Generation);
    [Fact] void should_lookup_the_second_generation_once() => _storage.Received(1).GetFor(_secondType.Id, _secondType.Generation);
}
