// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_effect_receipt_and_no_events : given.a_receipted_unit
{
    void Establish() => _unitOfWork.UseEffectReceipt(_key, JsonNode.Parse("42"));
    Task Because() => _unitOfWork.Commit();

    [Fact] void should_record_a_receipt_only() => _occurrences.Received(1).RecordEffectReceipt(_key, Arg.Any<NoEvents>(), Arg.Is<JsonNode>(_ => _.ToJsonString() == "42"));
    [Fact] void should_not_append() => _receiptedAppends.ShouldEqual(0);
    [Fact] void should_expose_the_receipt() => _unitOfWork.EffectReceipt.ShouldEqual(_receiptResult);
}
