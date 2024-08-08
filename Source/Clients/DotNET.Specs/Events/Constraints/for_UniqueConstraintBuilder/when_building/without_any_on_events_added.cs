// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class without_any_on_events_added : given.a_unique_constraint_builder_with_owner
{
    NoEventTypesAddedToUniqueConstraint _result;

    void Because() => _result = Catch.Exception<NoEventTypesAddedToUniqueConstraint>(() => _constraintBuilder.Build());

    [Fact] void should_throw_no_event_types_added_to_unique_constraint() => _result.ShouldNotBeNull();
}
