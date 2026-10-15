// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_with_an_effect_receipt;

public class without_prior_namespace_access : given.a_receipt_storage
{
    EffectReceiptWriteResult _result;
    EffectReceipt? _receipt;
    int _eventCount;

    async Task Because()
    {
        _result = await _storage.AppendManyWithEffectReceipt([_event], Receipt(0));
        _receipt = await Recreate().GetEffectReceipt(_receiptId);
        await using var context = CreateContext();
        _eventCount = await context.Events.CountAsync();
    }

    [Fact] void should_write_the_receipted_batch() => _result.ShouldEqual(EffectReceiptWriteResult.Written);
    [Fact] void should_record_the_outcome_in_namespace_storage() => _receipt!.Outcome.ShouldEqual(EffectOutcome.Appended);
    [Fact] void should_persist_the_event_alongside_its_receipt() => _eventCount.ShouldEqual(1);
}
