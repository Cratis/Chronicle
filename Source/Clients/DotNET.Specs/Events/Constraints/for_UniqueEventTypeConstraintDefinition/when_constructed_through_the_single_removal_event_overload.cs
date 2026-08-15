// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

/// <summary>
/// The client-side twin of the kernel compatibility overload. This record is the one an application reaches when it
/// writes its own <see cref="ICanProvideConstraints"/>, so the signature it had while a constraint could only be
/// released by one event is kept and exercised rather than left to rot.
/// </summary>
public class when_constructed_through_the_single_removal_event_overload : Specification
{
    static readonly ConstraintName _name = "LoanOpen";
    static readonly EventTypeId _coveredEventTypeId = "LoanCheckedOut";
    static readonly EventTypeId _removalEventTypeId = "LoanReturned";

    UniqueEventTypeConstraintDefinition _withRemovalEvent;
    UniqueEventTypeConstraintDefinition _withoutRemovalEvent;

    void Because()
    {
#pragma warning disable CS0618 // The compatibility overload is what this covers.
        _withRemovalEvent = new(_name, _ => string.Empty, [_coveredEventTypeId], _removalEventTypeId, null);
        _withoutRemovalEvent = new(_name, _ => string.Empty, [_coveredEventTypeId], (EventTypeId?)null, null);
#pragma warning restore CS0618
    }

    [Fact] void should_carry_the_single_removal_event() => _withRemovalEvent.RemovedWith.ShouldContainOnly([_removalEventTypeId]);
    [Fact] void should_release_on_nothing_when_none_was_given() => _withoutRemovalEvent.RemovedWith.ShouldBeEmpty();
    [Fact] void should_keep_the_covered_event_types() => _withRemovalEvent.EventTypeIds.ShouldContainOnly([_coveredEventTypeId]);
    [Fact] void should_carry_the_single_removal_event_onto_the_contract() => _withRemovalEvent.ToContract().RemovedWith.ShouldContainOnly([_removalEventTypeId.Value]);
}
