// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeSet;

public class when_creating_from_equal_lists : Specification
{
    EventTypeSet _first;
    EventTypeSet _second;
    EventTypeSet _viaCollectionExpression;

    void Because()
    {
        _first = EventTypeSet.From([new EventType("first-set-type", 1), new EventType("second-set-type", 2)]);
        _second = EventTypeSet.From([new EventType("first-set-type", 1), new EventType("second-set-type", 2)]);
        _viaCollectionExpression = [new EventType("first-set-type", 1), new EventType("second-set-type", 2)];
    }

    [Fact] void should_share_the_instance() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_share_the_instance_for_a_collection_expression() => ReferenceEquals(_first, _viaCollectionExpression).ShouldBeTrue();
    [Fact] void should_keep_the_order() => _first.Select(_ => _.Id.Value).ShouldEqual(["first-set-type", "second-set-type"]);
    [Fact] void should_carry_the_ids_as_a_precomputed_set() => _first.Ids.ShouldContainOnly((EventTypeId)"first-set-type", (EventTypeId)"second-set-type");
    [Fact] void should_be_equal() => _first.ShouldEqual(_second);
}
