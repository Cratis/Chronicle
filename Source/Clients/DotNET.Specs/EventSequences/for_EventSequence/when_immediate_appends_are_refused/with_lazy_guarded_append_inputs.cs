// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_lazy_guarded_append_inputs : given.a_unit_with_native_sequence
{
    Dictionary<string, Exception> _errors;

    async Task Because()
    {
        using var refusal = Refuse();
        ClearCalls();
        _errors = new()
        {
            ["lazy reads"] = await Catch.Exception(() => _sequence.AppendMany(Poison<EventForEventSourceId>(), guardedBy: Poison<IDecisionRead>())),
            ["lazy events"] = await Catch.Exception(() => _sequence.AppendMany(Poison<EventForEventSourceId>(), guardedBy: [new DecisionRead<object>("source", null, "store", "tenant", 7, [new EventType("changed", EventTypeGeneration.First)])])),
            ["invalid sequence"] = await Catch.Exception(() => DecisionReadExtensions.AppendMany(null!, null!, null!))
        };
    }

    [Fact] void should_check_admission_before_all_guarded_inputs() => _errors.Values.ShouldEachConformTo(error => error is ImmediateAppendRefused);
    [Fact] void should_cover_reads_events_and_target() => _errors.Count.ShouldEqual(3);
    [Fact] void should_not_serialize() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_types() => _eventTypes.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
