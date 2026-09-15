// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueEventTypesConstraints.for_UniqueEventTypesConstraintsStorage.when_checking_if_allowed;

/// <summary>
/// An unscoped constraint spans every dimension: the covered event blocks the next one for the event source no
/// matter which stream, stream type or event source type it is appended with, and its removal releases the cycle
/// just as unconditionally.
/// </summary>
public class and_the_constraint_is_unscoped : given.a_unique_event_types_constraints_storage
{
    static readonly EventSourceType _loan = "loan";
    static readonly EventStreamType _loans = "loans";
    static readonly EventStreamId _thisBranch = "branch-1";
    static readonly EventStreamId _anotherBranch = "branch-2";

    bool _isAllowedElsewhere;
    EventSequenceNumber _sequenceNumberElsewhere;
    bool _isAllowedAfterARemovalElsewhere;

    async Task Establish() => await Append(_checkedOutEventType, _borrower, _loan, _loans, _thisBranch);

    async Task Because()
    {
        (_isAllowedElsewhere, _sequenceNumberElsewhere) = await _storage.IsAllowedWithinScope(DefinitionReleasedByReturn, _borrower, ScopeFor(DefinitionReleasedByReturn, eventStreamId: _anotherBranch));

        await Append(_returnedEventType, _borrower, eventStreamId: _anotherBranch);
        (_isAllowedAfterARemovalElsewhere, _) = await _storage.IsAllowedWithinScope(DefinitionReleasedByReturn, _borrower, ScopeFor(DefinitionReleasedByReturn, eventStreamId: _thisBranch));
    }

    [Fact] void should_not_allow_a_covered_event_appended_with_other_dimensions() => _isAllowedElsewhere.ShouldBeFalse();
    [Fact] void should_report_the_covered_event_holding_the_cycle() => _sequenceNumberElsewhere.ShouldEqual(new EventSequenceNumber(0));
    [Fact] void should_allow_a_covered_event_after_a_removal_appended_with_other_dimensions() => _isAllowedAfterARemovalElsewhere.ShouldBeTrue();
}
