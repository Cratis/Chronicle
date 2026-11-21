// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosesStreamConstraintDefinition;

public class when_comparing_a_reordered_declaration : Specification
{
    ClosesStreamConstraintDefinition _definition;
    ClosesStreamConstraintDefinition _reordered;
    ClosesStreamConstraintDefinition _other;

    void Establish()
    {
        _definition = new("closing", [(EventTypeId)"first", (EventTypeId)"second"], ClosedStreamDimensions.EventSourceId, [(EventTypeId)"reopen-one", (EventTypeId)"reopen-two"]);
        _reordered = new("closing", [(EventTypeId)"second", (EventTypeId)"first"], ClosedStreamDimensions.EventSourceId, [(EventTypeId)"reopen-two", (EventTypeId)"reopen-one"]);
        _other = new("closing", [(EventTypeId)"first", (EventTypeId)"second"], ClosedStreamDimensions.EventSourceId, [(EventTypeId)"reopen-one"]);
    }

    [Fact] void should_be_equal() => _definition.Equals(_reordered).ShouldBeTrue();
    [Fact] void should_hash_alike() => _definition.GetHashCode().ShouldEqual(_reordered.GetHashCode());
    [Fact] void should_not_be_equal_to_one_reopened_by_fewer_events() => _definition.Equals(_other).ShouldBeFalse();
}
