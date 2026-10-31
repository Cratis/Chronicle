// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_a_receipt_and_enforcement_without_reads : given.a_receipted_unit
{
    ReadProtectionFrame _frame;

    void Establish()
    {
        _frame = _unitOfWork.EnforceReadProtection();
        _unitOfWork.UseEffectReceipt(_key);
    }

    Task Because() => _unitOfWork.CommitAsOwner(_unitOfWork.ClaimDecisionReadCommitOwnership());

    [Fact] void should_use_the_trusted_receipt_only_path() => ((IOccurrencesForUnitOfWork)_occurrences).Received(1).RecordNoEvents(_key, Arg.Any<JsonNode>());
    [Fact] void should_not_append_without_dependencies() => _receiptedAppends.ShouldEqual(0);
    [Fact] void should_keep_the_receipt() => _unitOfWork.EffectReceipt.ShouldEqual(_receiptResult);

    void Destroy() => _frame.Dispose();
}
