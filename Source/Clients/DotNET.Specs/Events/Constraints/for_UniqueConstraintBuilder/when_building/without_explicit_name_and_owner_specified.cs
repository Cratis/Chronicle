// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class without_explicit_name_and_owner_specified : given.a_unique_constraint_builder_without_owner
{
    Exception _result;

    void Establish()
    {
        var eventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        _constraintBuilder.On(eventType, nameof(EventWithStringProperty.SomeProperty));
    }

    void Because() => _result = Catch.Exception(() => _constraintBuilder.Build());

    [Fact] void should_throw_missing_name_for_unique_constraint() => _result.ShouldBeOfExactType<MissingNameForUniqueConstraint>();
}
