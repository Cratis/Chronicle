// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_two_decision_reads_share_a_key : given.a_unit_of_work
{
    readonly EventSourceId _key = "source";
    readonly EventType _created = new("created", EventTypeGeneration.First);
    readonly EventType _removed = new("removed", EventTypeGeneration.First);

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        var first = new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 7, [_created]);
        var second = new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 9, [_removed]);
        _unitOfWork.AddDecisionRead(first);
        _unitOfWork.AddDecisionRead(second);
    }

    async Task Because()
    {
        var owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        await _unitOfWork.CommitAsOwner(owner);
    }

    [Fact] void should_validate_without_appending_events() => _eventsAppended.ShouldBeEmpty();
    [Fact] void should_use_the_earliest_boundary() => _concurrencyScopesAppended[_key].SequenceNumber.ShouldEqual((EventSequenceNumber)7);
    [Fact] void should_guard_both_event_types() => _concurrencyScopesAppended[_key].EventTypes.ShouldContain(_created);
    [Fact] void should_guard_removal_too() => _concurrencyScopesAppended[_key].EventTypes.ShouldContain(_removed);
}
