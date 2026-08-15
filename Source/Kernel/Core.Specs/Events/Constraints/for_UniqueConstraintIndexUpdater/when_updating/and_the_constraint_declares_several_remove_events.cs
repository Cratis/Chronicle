// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintIndexUpdater.when_updating;

/// <summary>
/// The claimed value has to be released by every event the constraint declares as terminal, not by one of them. This
/// is where the defect was visible in production: an invitation released on provisioning while failure and revocation
/// — declared alongside it — left the address reserved forever, the append succeeding and nothing reporting a problem.
/// </summary>
/// <remarks>
/// Each declared event is put through the updater in turn, because the failure being guarded against is not that no
/// event releases but that only one of them does. An event type the constraint does not mention goes through the same
/// sweep, so a release triggered by everything would not read as a pass either.
/// </remarks>
public class and_the_constraint_declares_several_remove_events : Specification
{
    static readonly EventTypeId _acceptedEventTypeId = "InvitationAccepted";
    static readonly EventTypeId _revokedEventTypeId = "InvitationRevoked";
    static readonly EventTypeId _expiredEventTypeId = "InvitationExpired";
    static readonly EventTypeId _resentEventTypeId = "InvitationResent";

    UniqueConstraintDefinition _definition;
    Dictionary<EventTypeId, bool> _released;

    void Establish() => _definition = new(
        "UniqueInvitedAddress",
        [new("InvitationSent", ["EmailAddress"])],
        [_acceptedEventTypeId, _revokedEventTypeId, _expiredEventTypeId]);

    async Task Because()
    {
        _released = [];
        foreach (var eventTypeId in new[] { _acceptedEventTypeId, _revokedEventTypeId, _expiredEventTypeId, _resentEventTypeId })
        {
            var storage = Substitute.For<IUniqueConstraintsStorage>();
            var context = new ConstraintValidationContext([], EventSourceId.New(), eventTypeId, new ExpandoObject());
            await new UniqueConstraintIndexUpdater(_definition, context, storage).Update(EventSequenceNumber.First);
            _released[eventTypeId] = storage.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IUniqueConstraintsStorage.Remove));
        }
    }

    [Fact] void should_release_on_the_first_declared_removal_event() => _released[_acceptedEventTypeId].ShouldBeTrue();
    [Fact] void should_release_on_the_second_declared_removal_event() => _released[_revokedEventTypeId].ShouldBeTrue();
    [Fact] void should_release_on_the_last_declared_removal_event() => _released[_expiredEventTypeId].ShouldBeTrue();
    [Fact] void should_not_release_on_an_event_type_that_was_not_declared() => _released[_resentEventTypeId].ShouldBeFalse();
}
