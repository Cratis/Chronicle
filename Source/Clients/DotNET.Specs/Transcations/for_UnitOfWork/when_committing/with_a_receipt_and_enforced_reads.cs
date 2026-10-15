// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_a_receipt_and_enforced_reads : given.a_receipted_unit
{
    ReadProtectionFrame _frame;

    void Establish()
    {
        _frame = _unitOfWork.EnforceReadProtection();
        _unitOfWork.UseEffectReceipt(_key);
        _unitOfWork.AddDecisionRead(new DecisionRead<object>("source", null, _eventStore.Name, _eventStore.Namespace, 7, [new EventType("created", EventTypeGeneration.First)]));
    }

    Task Because() => _unitOfWork.CommitAsOwner(_unitOfWork.ClaimDecisionReadCommitOwnership());

    [Fact] void should_commit_a_receipted_validation() => _receiptedAppends.ShouldEqual(1);
    [Fact] void should_carry_the_read_contribution() => _readProtection.Contributions.Count.ShouldEqual(1);
    [Fact] void should_keep_the_receipt() => _unitOfWork.EffectReceipt.ShouldEqual(_receiptResult);

    void Destroy() => _frame.Dispose();
}
