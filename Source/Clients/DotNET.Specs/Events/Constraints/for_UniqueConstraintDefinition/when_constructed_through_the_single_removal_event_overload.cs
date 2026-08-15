// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintDefinition;

/// <summary>
/// The client-side twin of the kernel compatibility overload. This record is the one an application reaches when it
/// writes its own <see cref="ICanProvideConstraints"/>, so the signature it had while a constraint could only be
/// released by one event is kept and exercised rather than left to rot.
/// </summary>
public class when_constructed_through_the_single_removal_event_overload : Specification
{
    static readonly ConstraintName _name = "UniqueInvitedAddress";
    static readonly EventTypeId _removalEventTypeId = "InvitationRevoked";
    static readonly UniqueConstraintEventDefinition[] _eventsWithProperties = [new("InvitationSent", ["EmailAddress"])];

    UniqueConstraintDefinition _withRemovalEvent;
    UniqueConstraintDefinition _withoutRemovalEvent;

    void Because()
    {
#pragma warning disable CS0618 // The compatibility overload is what this covers.
        _withRemovalEvent = new(_name, _ => string.Empty, _eventsWithProperties, _removalEventTypeId, true, null);
        _withoutRemovalEvent = new(_name, _ => string.Empty, _eventsWithProperties, (EventTypeId?)null, false, null);
#pragma warning restore CS0618
    }

    [Fact] void should_carry_the_single_removal_event() => _withRemovalEvent.RemovedWith.ShouldContainOnly([_removalEventTypeId]);
    [Fact] void should_release_on_nothing_when_none_was_given() => _withoutRemovalEvent.RemovedWith.ShouldBeEmpty();
    [Fact] void should_keep_the_rest_of_the_definition() => _withRemovalEvent.EventsWithProperties.ShouldContainOnly(_eventsWithProperties);
    [Fact] void should_keep_the_casing_choice() => _withRemovalEvent.IgnoreCasing.ShouldBeTrue();
    [Fact] void should_carry_the_single_removal_event_onto_the_contract() => _withRemovalEvent.ToContract().RemovedWith.ShouldContainOnly([_removalEventTypeId.Value]);
}
