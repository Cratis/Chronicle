// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_receipt_and_no_events : for_UnitOfWork.given.a_receipted_unit
{
    async Task Because()
    {
        using var owner = _unitOfWork.RefuseImmediateAppends();
        _unitOfWork.UseEffectReceipt(_key);
        await owner.Commit();
    }

    [Fact] async Task should_preserve_receipt_only_recording_without_read_protection() => await _occurrences.Received(1).RecordEffectReceipt(_key, Arg.Is<EffectOutcome>(outcome => outcome is NoEvents), Arg.Any<JsonNode>());
    [Fact] void should_not_use_a_receipted_append() => _receiptedAppends.ShouldEqual(0);
    [Fact] void should_expose_the_winning_receipt() => _unitOfWork.EffectReceipt.ShouldEqual(_receiptResult);
    [Fact] void should_leave_read_protection_off() => _unitOfWork.IsEnforcingReadProtection.ShouldBeFalse();
}
