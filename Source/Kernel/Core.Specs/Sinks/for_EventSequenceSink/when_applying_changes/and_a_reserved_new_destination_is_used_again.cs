// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_a_reserved_new_destination_is_used_again : given.a_sink
{
    static readonly EventSequenceId _reserved = new("jobs");

    IEnumerable<Storage.Sinks.FailedPartition> _firstFailures;
    IEnumerable<Storage.Sinks.FailedPartition> _secondFailures;

    protected override EventSequenceId? Configured => _reserved;

    void Establish() => _destinations.ReservedIds.IsReserved(_reserved).Returns(true);

    async Task Because()
    {
        _firstFailures = await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        _secondFailures = await _sink.ApplyChanges("customer-1", Fold(new(), 20, 2, 6), 6);
    }

    [Fact] void should_fail_the_first_call() => _firstFailures.Count().ShouldEqual(1);
    [Fact] void should_fail_the_second_call() => _secondFailures.Count().ShouldEqual(1);
    [Fact] void should_not_append_anything() => _destinations.AppendAttempts.ShouldEqual(0);
    [Fact] void should_not_consult_storage() => _destinations.Storage.DidNotReceive().GetEventStores();
}
