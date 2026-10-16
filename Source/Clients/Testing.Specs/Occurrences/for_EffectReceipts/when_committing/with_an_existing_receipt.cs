// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.when_committing;

public class with_an_existing_receipt : given.a_store
{
    EffectReceiptStatus _status;
    EffectReceipt _receipt;
    int _events;

    async Task Establish()
    {
        using var first = UnitWithEvent("first");
        await first.Commit();
    }

    async Task Because()
    {
        using var repeated = UnitWithEvent("different on retry");
        await repeated.Commit();
        _status = repeated.EffectReceipt!.Status;
        _receipt = (await _store.Occurrences.GetEffectReceipt(_key))!;
        _events = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    [Fact] void should_report_the_recorded_effect() => _status.ShouldEqual(EffectReceiptStatus.AlreadyRecorded);
    [Fact] void should_keep_one_event() => _events.ShouldEqual(1);
    [Fact] void should_keep_the_original_range() => ((Appended)_receipt.Outcome).First.ShouldEqual(EventSequenceNumber.First);
}
