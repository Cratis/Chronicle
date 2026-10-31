// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_effect_receipt;

public class with_an_existing_receipt : given.a_receipted_append
{
    void Establish() => _response.EffectAlreadyRecorded = true;

    async Task Because() => _result = await ((IEventSequenceWithEffectReceipts)_eventSequence).AppendManyWithEffectReceipt([new("source", "created")], _scopes, _receipt, null);

    [Fact] void should_carry_the_receipt_to_the_untagged_transport() => _request.EffectReceipt.ShouldEqual(_receipt);
    [Fact] void should_suppress_local_append_notifications() => _notifications.ShouldEqual(0);
    [Fact] void should_return_the_winning_response() => _result.EffectReceipt.Receipt.Response.ToJsonString().ShouldEqual("\"winner\"");
    [Fact] void should_report_the_duplicate() => _result.EffectReceipt.Status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
}
