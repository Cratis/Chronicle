// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

/// <summary>
/// Declaring the same removal event twice says nothing the first declaration did not, so the definition carries it
/// once. Registration compares the incoming definition with the stored one to decide whether the constraint changed,
/// and a repeated entry would make an otherwise identical redeclaration look like a change on every startup.
/// </summary>
public class with_the_same_removed_with_declared_twice : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    UniqueConstraintDefinition _result;
    EventType _removedWithEventType;

    void Establish()
    {
        _removedWithEventType = new EventType("InvitationRevoked", EventTypeGeneration.First);

        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.RemovedWith(_removedWithEventType);
        _constraintBuilder.RemovedWith(_removedWithEventType);
    }

    void Because() => _result = _constraintBuilder.Build() as UniqueConstraintDefinition;

    [Fact] void should_carry_the_removal_event_once() => _result.RemovedWith.ShouldContainOnly([_removedWithEventType.Id]);
}
