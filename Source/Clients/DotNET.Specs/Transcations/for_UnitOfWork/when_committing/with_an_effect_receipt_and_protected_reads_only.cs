// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_effect_receipt_and_protected_reads_only : given.a_receipted_unit
{
    void Establish()
    {
        _unitOfWork.UseEffectReceipt(_key);
        _unitOfWork.AddDecisionRead(new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 7, [new EventType("created", EventTypeGeneration.First)]));
    }

    Task Because() => _unitOfWork.CommitAsOwner(_unitOfWork.ClaimDecisionReadCommitOwnership());

    [Fact] void should_validate_and_record_atomically() => _receiptedAppends.ShouldEqual(1);
    [Fact] void should_append_no_events() => _eventsAppended.ShouldBeEmpty();
    [Fact] void should_keep_the_protected_scope() => _concurrencyScopesAppended[(EventSourceId)"source"].SequenceNumber.ShouldEqual((EventSequenceNumber)7);
    [Fact] void should_never_record_an_unconditional_receipt() => _occurrences.DidNotReceive().RecordEffectReceipt(Arg.Any<EffectKey>(), Arg.Any<EffectOutcome>(), Arg.Any<JsonNode>());
}
