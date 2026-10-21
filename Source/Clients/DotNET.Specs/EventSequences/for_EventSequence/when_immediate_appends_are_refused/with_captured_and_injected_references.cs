// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_captured_and_injected_references : given.a_unit_with_native_sequence
{
    Dictionary<string, Exception> _errors;

    async Task Because()
    {
        IEventSequence captured = _sequence;
        IEventSequence arbitrary = CreateSequence("store", "another-sequence");
        IEventSequence otherStore = CreateSequence("another-store", EventSequenceId.Log);
        var log = CreateLog();
        _store.EventLog.Returns(log);
        await using var provider = new ServiceCollection().AddSingleton<IEventLog>(log).AddSingleton(_store).BuildServiceProvider();
        using var frame = Refuse();
        ClearCalls();
        _errors = [];
        foreach (var (name, operation) in new Dictionary<string, Func<Task>>
        {
            ["captured sequence"] = () => captured.Append("source", "changed"),
            ["arbitrary sequence"] = () => arbitrary.Append("source", "changed"),
            ["another store"] = () => otherStore.Append("source", "changed"),
            ["injected event log"] = () => provider.GetRequiredService<IEventLog>().Append("source", "changed"),
            ["store event log"] = () => provider.GetRequiredService<IEventStore>().EventLog.Append("source", "changed")
        })
        {
            _errors.Add(name, await Catch.Exception(operation));
        }
    }

    [Fact] void should_cover_all_reference_shapes() => _errors.Count.ShouldEqual(5);
    [Fact] void should_refuse_every_native_reference() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendRefused);
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_serialize() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
}
