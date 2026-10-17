// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class with_an_effect_receipt_after_capability_loss : given.a_receipted_unit
{
    Exception _exception;

    void Establish()
    {
        _unitOfWork.UseEffectReceipt(_key);
        ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(false);
    }

    async Task Because() => _exception = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EffectReceiptsNotSupported>();
    [Fact] void should_not_report_success() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_complete_the_unit() => _unitOfWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_append() => _receiptedAppends.ShouldEqual(0);
    [Fact] void should_not_record() => _occurrences.DidNotReceive().RecordEffectReceipt(Arg.Any<EffectKey>(), Arg.Any<EffectOutcome>(), Arg.Any<JsonNode>());
}
