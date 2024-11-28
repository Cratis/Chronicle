// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class without_explicit_name_and_owner_specified : given.a_unique_constraint_builder_without_owner_with_an_event_type
{
    Exception _result;

    void Establish()
    {
        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
    }

    void Because() => _result = Catch.Exception(() => _constraintBuilder.Build());

    [Fact] void should_throw_missing_name_for_unique_constraint() => _result.ShouldBeOfExactType<MissingNameForUniqueConstraint>();
}
