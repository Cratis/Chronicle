// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_reading;

public class an_unknown_event_type : given.a_registry
{
    Exception _singleFailure;
    Exception _definitionFailure;
    IEnumerable<EventTypeSchema> _batch;
    bool _has;
    bool _hasUnknownGeneration;

    async Task Because()
    {
        _singleFailure = await Catch.Exception(() => _registry.GetFor("never-registered", 1));
        _definitionFailure = await Catch.Exception(() => _registry.GetDefinition("never-registered"));
        _batch = await _registry.GetFor([new EventType("never-registered", 1), new EventType(_eventTypeId, 1)]);
        _has = await _registry.HasFor("never-registered", 1);
        _hasUnknownGeneration = await _registry.HasFor(_eventTypeId, 99);
    }

    [Fact] void should_let_storage_decide_what_happens_for_a_single_lookup() => _singleFailure.ShouldBeOfExactType<MissingEventSchemaForEventType>();
    [Fact] void should_let_storage_decide_what_happens_for_a_definition() => _definitionFailure.ShouldBeOfExactType<MissingEventSchemaForEventType>();
    [Fact] void should_leave_it_out_of_a_batch() => _batch.Select(_ => _.Type.Id).ShouldContainOnly(_eventTypeId);
    [Fact] void should_not_have_it() => _has.ShouldBeFalse();
    [Fact] void should_not_have_a_generation_that_was_never_registered() => _hasUnknownGeneration.ShouldBeFalse();
}
