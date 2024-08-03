// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_adding_on_using_event_type_directly;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_explicit_name_and_owner_is_not_specified : given.a_unique_constraint_builder_with_owner
{
    const string _name = "SomeName";

    IConstraintDefinition _result;

    void Establish()
    {
        var eventType = new EventType(nameof(EventWithStringProperty), EventGeneration.First);
        _constraintBuilder.On(eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.WithName(_name);
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_set_name_to_owners_name() => _result.Name.Value.ShouldEqual(_name);
}
