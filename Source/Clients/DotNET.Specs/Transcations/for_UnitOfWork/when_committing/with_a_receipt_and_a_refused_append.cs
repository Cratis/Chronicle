// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_a_receipt_and_a_refused_append : given.a_receipted_unit
{
    void Establish()
    {
        _unitOfWork.UseEffectReceipt(_key);
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", "created", Causation.Unknown());
        _appendResult = AppendManyResult.Failed(_correlationId, [new AppendError("refused")]);
    }

    Task Because() => _unitOfWork.Commit();

    [Fact] void should_report_failure() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_expose_a_receipt() => _unitOfWork.EffectReceipt.ShouldBeNull();
    [Fact] void should_not_seal_the_refusal_separately() => _occurrences.DidNotReceive().RecordEffectReceipt(Arg.Any<EffectKey>(), Arg.Any<EffectOutcome>(), Arg.Any<JsonNode>());
}
