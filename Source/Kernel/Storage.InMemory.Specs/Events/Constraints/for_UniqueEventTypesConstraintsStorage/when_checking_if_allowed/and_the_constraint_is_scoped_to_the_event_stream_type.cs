// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// A constraint scoped per event stream type holds one cycle per event stream type: the covered event already
/// appended blocks the next one on the same stream type, and says nothing about any other.
/// </summary>
public class and_the_constraint_is_scoped_to_the_event_stream_type : given.a_unique_event_types_constraints_storage
{
    static readonly EventStreamType _loans = "loans";
    static readonly EventStreamType _reservations = "reservations";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(new ConstraintScope(EventStreamType: (EventStreamType)Marker));

    bool _isAllowedForTheSameEventStreamType;
    EventSequenceNumber _sequenceNumberForTheSameEventStreamType;
    bool _isAllowedForAnotherEventStreamType;

    async Task Establish() => await Append(0, _checkedOutEventType, _borrower, eventStreamType: _loans);

    async Task Because()
    {
        (_isAllowedForTheSameEventStreamType, _sequenceNumberForTheSameEventStreamType) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, eventStreamType: _loans));
        (_isAllowedForAnotherEventStreamType, _) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, eventStreamType: _reservations));
    }

    [Fact] void should_not_allow_a_covered_event_on_the_same_event_stream_type() => _isAllowedForTheSameEventStreamType.ShouldBeFalse();
    [Fact] void should_report_the_covered_event_holding_the_cycle() => _sequenceNumberForTheSameEventStreamType.ShouldEqual(new EventSequenceNumber(0));
    [Fact] void should_allow_a_covered_event_on_another_event_stream_type() => _isAllowedForAnotherEventStreamType.ShouldBeTrue();
}
