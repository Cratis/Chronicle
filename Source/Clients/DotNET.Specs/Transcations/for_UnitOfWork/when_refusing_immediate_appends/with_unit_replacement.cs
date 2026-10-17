// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_unit_replacement : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Dictionary<string, Exception> _errors;
    bool _keptOriginal;

    async Task Because()
    {
        var otherUnit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        using var owner = Refuse();
        ClearCalls();
        _errors = new()
        {
            ["begin"] = Catch.Exception(() => _manager.Begin(CorrelationId.New())),
            ["set current"] = Catch.Exception(() => _manager.SetCurrent(otherUnit)),
            ["other unit commit"] = await Catch.Exception(otherUnit.Commit),
            ["other unit rollback"] = await Catch.Exception(otherUnit.Rollback),
            ["other unit dispose"] = Catch.Exception(otherUnit.Dispose),
            ["other unit refusal"] = Catch.Exception(() => otherUnit.RefuseImmediateAppends())
        };
        _keptOriginal = ReferenceEquals(_manager.Current, _unit);
        await _sequence.Transactional.Append("source", "changed");
        await owner.Commit();
    }

    [Fact] void should_refuse_every_replacement_or_captured_unit_escape() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendRefusalRequiresOwner);
    [Fact] void should_cover_all_replacement_paths() => _errors.Count.ShouldEqual(6);
    [Fact] void should_keep_the_owning_unit_current() => _keptOriginal.ShouldBeTrue();
    [Fact] void should_only_send_the_owner_batch() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
}
