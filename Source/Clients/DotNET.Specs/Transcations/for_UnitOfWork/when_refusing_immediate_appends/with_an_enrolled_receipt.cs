// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_an_enrolled_receipt : for_UnitOfWork.given.a_receipted_unit
{
    async Task Because()
    {
        using var owner = _unitOfWork.RefuseImmediateAppends();
        _unitOfWork.UseEffectReceipt(_key);
        _unitOfWork.AddEvents(EventSequenceId.Log, [new("source", "changed")], []);
        await owner.Commit();
    }

    [Fact] void should_commit_once_through_the_receipted_path() => _receiptedAppends.ShouldEqual(1);
    [Fact] void should_commit_the_enrolled_event() => _eventsAppended.Single().Event.ShouldEqual("changed");
    [Fact] void should_carry_the_enrolled_receipt() => _sentReceipt.EffectKey.ShouldEqual(_key.Name.Value);
    [Fact] void should_expose_the_winning_receipt() => _unitOfWork.EffectReceipt.ShouldEqual(_receiptResult);
    [Fact] async Task should_not_fall_back_to_a_public_append() => await _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
}
