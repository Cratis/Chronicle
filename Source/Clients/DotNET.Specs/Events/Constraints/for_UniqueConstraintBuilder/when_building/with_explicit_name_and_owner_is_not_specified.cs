// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_explicit_name_and_owner_is_not_specified : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    const string Name = "SomeName";

    IConstraintDefinition _result;

    void Establish()
    {
        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.WithName(Name);
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_set_name_to_owners_name() => _result.Name.Value.ShouldEqual(Name);
}
