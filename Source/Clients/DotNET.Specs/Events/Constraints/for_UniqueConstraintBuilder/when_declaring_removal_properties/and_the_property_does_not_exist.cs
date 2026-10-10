// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_declaring_removal_properties;

public class and_the_property_does_not_exist : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _constraintBuilder.RemovedWith(_eventType, ["MissingProperty"]));

    [Fact] void should_reject_the_missing_property() => _error.ShouldBeOfExactType<PropertyDoesNotExistOnEventType>();
}
