// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.when_committing;

public class with_no_events_and_a_matching_scope : given.a_store
{
    EffectReceipt _receipt;
    bool _success;
    int _count;

    async Task Because()
    {
        using var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        unit.UseEffectReceipt(_key);
        unit.AddEvents(EventSequenceId.Log, [], [new("read-source", new ConcurrencyScope(EventSequenceNumber.BeforeFirst, "read-source"))]);
        await unit.Commit();
        _success = unit.IsSuccess;
        _receipt = (await _store.Occurrences.GetEffectReceipt(_key))!;
        _count = (await _store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    [Fact] void should_validate_the_scope() => _success.ShouldBeTrue();
    [Fact] void should_record_no_events() => _receipt.Outcome.ShouldBeOfExactType<NoEvents>();
    [Fact] void should_not_append_an_event() => _count.ShouldEqual(0);
}
