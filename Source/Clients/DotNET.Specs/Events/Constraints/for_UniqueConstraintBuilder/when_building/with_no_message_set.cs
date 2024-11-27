// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

public class with_no_message_set : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    const string _message = "Some message";

    IConstraintDefinition _result;

    void Establish()
    {
        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.WithMessage(_ => _message);
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_have_a_proper_message_factory() => _result.MessageCallback.ShouldNotBeNull();
}
