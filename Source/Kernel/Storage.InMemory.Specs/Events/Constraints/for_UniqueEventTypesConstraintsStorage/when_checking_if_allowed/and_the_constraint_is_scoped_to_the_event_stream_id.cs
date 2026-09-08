// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// A constraint scoped per event stream id holds one cycle per stream: a borrower with an open loan at one branch
/// is blocked there and free at another.
/// </summary>
public class and_the_constraint_is_scoped_to_the_event_stream_id : given.a_unique_event_types_constraints_storage
{
    static readonly EventStreamId _thisBranch = "branch-1";
    static readonly EventStreamId _anotherBranch = "branch-2";
    static readonly UniqueEventTypeConstraintDefinition _definition = DefinitionScopedTo(new ConstraintScope(EventStreamId: (EventStreamId)Marker));

    bool _isAllowedOnTheSameStream;
    EventSequenceNumber _sequenceNumberOnTheSameStream;
    bool _isAllowedOnAnotherStream;

    async Task Establish() => await Append(0, _checkedOutEventType, _borrower, eventStreamId: _thisBranch);

    async Task Because()
    {
        (_isAllowedOnTheSameStream, _sequenceNumberOnTheSameStream) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, eventStreamId: _thisBranch));
        (_isAllowedOnAnotherStream, _) = await _storage.IsAllowed(_definition, _borrower, ScopeFor(_definition, eventStreamId: _anotherBranch));
    }

    [Fact] void should_not_allow_a_covered_event_on_the_same_stream() => _isAllowedOnTheSameStream.ShouldBeFalse();
    [Fact] void should_report_the_covered_event_holding_the_cycle() => _sequenceNumberOnTheSameStream.ShouldEqual(new EventSequenceNumber(0));
    [Fact] void should_allow_a_covered_event_on_another_stream() => _isAllowedOnAnotherStream.ShouldBeTrue();
}
