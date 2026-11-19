// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

public class when_comparing_with_a_reordered_definition : Specification
{
    UniqueEventTypeConstraintDefinition _definition;
    UniqueEventTypeConstraintDefinition _reordered;
    UniqueEventTypeConstraintDefinition _other;

    void Establish()
    {
        _definition = new("some-constraint", [(EventTypeId)"first", (EventTypeId)"second"], [(EventTypeId)"removed-one", (EventTypeId)"removed-two"]);
        _reordered = new("some-constraint", [(EventTypeId)"second", (EventTypeId)"first", (EventTypeId)"first"], [(EventTypeId)"removed-two", (EventTypeId)"removed-one"]) { Scope = new ConstraintScope() };
        _other = new("some-constraint", [(EventTypeId)"first", (EventTypeId)"third"], [(EventTypeId)"removed-one", (EventTypeId)"removed-two"]);
    }

    [Fact] void should_be_equal() => _definition.Equals(_reordered).ShouldBeTrue();
    [Fact] void should_hash_alike() => _definition.GetHashCode().ShouldEqual(_reordered.GetHashCode());
    [Fact] void should_not_be_equal_to_one_covering_another_event_type() => _definition.Equals(_other).ShouldBeFalse();
}
