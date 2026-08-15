// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintBuilder.when_building;

/// <summary>
/// A lifecycle can end in more than one way — an invited address is released by the invitation being accepted,
/// revoked, or expiring — and the builder accepts a declaration for each. Every one of them has to reach the
/// definition: the builder used to hold a single value, so each call replaced the previous one and only the last
/// declaration released anything. The others compiled, registered, and did nothing.
/// </summary>
public class with_several_removed_with_declarations : given.a_unique_constraint_builder_with_owner_and_an_event_type
{
    UniqueConstraintDefinition _result;
    EventType _acceptedEventType;
    EventType _revokedEventType;
    EventType _expiredEventType;

    void Establish()
    {
        _acceptedEventType = new EventType("InvitationAccepted", EventTypeGeneration.First);
        _revokedEventType = new EventType("InvitationRevoked", EventTypeGeneration.First);
        _expiredEventType = new EventType("InvitationExpired", EventTypeGeneration.First);

        _constraintBuilder.On(_eventType, nameof(EventWithStringProperty.SomeProperty));
        _constraintBuilder.RemovedWith(_acceptedEventType);
        _constraintBuilder.RemovedWith(_revokedEventType);
        _constraintBuilder.RemovedWith(_expiredEventType);
    }

    void Because() => _result = _constraintBuilder.Build() as UniqueConstraintDefinition;

    [Fact] void should_carry_every_declared_removal_event() => _result.RemovedWith.ShouldContainOnly([_acceptedEventType.Id, _revokedEventType.Id, _expiredEventType.Id]);
    [Fact] void should_not_have_dropped_the_first_declaration() => _result.RemovedWith.ShouldContain(_acceptedEventType.Id);
}
