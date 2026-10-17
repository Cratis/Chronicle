// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_already_recorded_effect : given.a_receipted_unit
{
    void Establish()
    {
        _unitOfWork.UseEffectReceipt(_key, JsonNode.Parse("\"loser\""));
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", "created", Causation.Unknown());
        _receiptResult = new(EffectReceiptStatus.AlreadyRecorded, _receiptResult.Receipt with { Response = JsonNode.Parse("\"winner\"") });
    }

    Task Because() => _unitOfWork.Commit();

    [Fact] void should_report_a_successful_commit() => _unitOfWork.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_winning_response() => _unitOfWork.EffectReceipt.Receipt.Response.ToJsonString().ShouldEqual("\"winner\"");
    [Fact] void should_report_the_collision() => _unitOfWork.EffectReceipt.Status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
}
