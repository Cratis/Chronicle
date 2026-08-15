// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition;

/// <summary>
/// The signature this record had while a constraint could only be released by one event is kept so that an assembly
/// compiled against it keeps linking. This exercises it, because a compatibility overload nothing calls is one
/// refactor away from being deleted or from quietly forwarding the wrong thing.
/// </summary>
/// <remarks>
/// A null said "no removal event" and has to keep saying it, rather than becoming a collection holding one null.
/// Both shapes go through the overload here for that reason.
/// </remarks>
public class when_constructed_through_the_single_removal_event_overload : Specification
{
    static readonly ConstraintName _name = "unique-invited-address";
    static readonly EventTypeId _removalEventTypeId = "InvitationRevoked";
    static readonly UniqueConstraintEventDefinition[] _eventDefinitions = [new("InvitationSent", ["EmailAddress"])];

    UniqueConstraintDefinition _withRemovalEvent;
    UniqueConstraintDefinition _withoutRemovalEvent;

    void Because()
    {
#pragma warning disable CS0618 // The compatibility overload is what this covers.
        _withRemovalEvent = new(_name, _eventDefinitions, _removalEventTypeId, true, null);
        _withoutRemovalEvent = new(_name, _eventDefinitions, (EventTypeId?)null, false, null);
#pragma warning restore CS0618
    }

    [Fact] void should_carry_the_single_removal_event() => _withRemovalEvent.RemovedWith.ShouldContainOnly([_removalEventTypeId]);
    [Fact] void should_release_on_nothing_when_none_was_given() => _withoutRemovalEvent.RemovedWith.ShouldBeEmpty();
    [Fact] void should_keep_the_rest_of_the_definition() => _withRemovalEvent.EventDefinitions.ShouldContainOnly(_eventDefinitions);
    [Fact] void should_keep_the_casing_choice() => _withRemovalEvent.IgnoreCasing.ShouldBeTrue();
    [Fact] void should_equal_the_same_definition_built_the_plural_way() => _withRemovalEvent.Equals(new UniqueConstraintDefinition(_name, _eventDefinitions, [_removalEventTypeId], true)).ShouldBeTrue();
}
