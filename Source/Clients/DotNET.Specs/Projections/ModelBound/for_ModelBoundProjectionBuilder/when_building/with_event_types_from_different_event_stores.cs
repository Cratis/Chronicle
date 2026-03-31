// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.given;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class with_event_types_from_different_event_stores : Specification
{
    [EventType]
    [EventStore("event-store-one")]
    class EventFromFirstStore(string Name);

    [EventType]
    [EventStore("event-store-two")]
    class EventFromSecondStore(string Value);

    [FromEvent<EventFromFirstStore>]
    [FromEvent<EventFromSecondStore>]
    record ModelWithMixedEventStores(
        [Key] Guid Id,
        string Name);

    ModelBoundProjectionBuilder _builder;

    void Establish()
    {
        var namingPolicy = new TestNamingPolicy();
        var eventTypes = new EventTypesForSpecifications([typeof(EventFromFirstStore), typeof(EventFromSecondStore)]);
        _builder = new ModelBoundProjectionBuilder(namingPolicy, eventTypes);
    }

    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _builder.Build(typeof(ModelWithMixedEventStores)));

    [Fact] void should_throw_multiple_event_stores_defined() => _exception.ShouldBeOfExactType<MultipleEventStoresDefined>();
}
