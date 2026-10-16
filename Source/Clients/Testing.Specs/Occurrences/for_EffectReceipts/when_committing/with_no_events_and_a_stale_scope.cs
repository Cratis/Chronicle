// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.when_committing;

public class with_no_events_and_a_stale_scope : given.a_store
{
    EffectReceipt? _receipt;
    bool _success;

    async Task Establish() => await _store.EventLog.Append("read-source", new SomethingHappened("new fact"));

    async Task Because()
    {
        using var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        unit.UseEffectReceipt(_key);
        unit.AddEvents(EventSequenceId.Log, [], [new("read-source", new ConcurrencyScope(EventSequenceNumber.BeforeFirst, "read-source"))]);
        await unit.Commit();
        _success = unit.IsSuccess;
        _receipt = await _store.Occurrences.GetEffectReceipt(_key);
    }

    [Fact] void should_refuse_the_stale_decision() => _success.ShouldBeFalse();
    [Fact] void should_not_record_a_receipt() => _receipt.ShouldBeNull();
}
