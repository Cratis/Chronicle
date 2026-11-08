// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_destination_is_reserved_and_exists : given.a_sink
{
    static readonly EventSequenceId _reserved = new("jobs");

    IEnumerable<Storage.Sinks.FailedPartition> _failures;

    protected override EventSequenceId? Configured => _reserved;

    void Establish()
    {
        _destinations.ReservedIds.IsReserved(_reserved).Returns(true);
        _destinations.ExistingSequences.Add(_reserved);
    }

    async Task Because()
    {
        _failures = await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        await _sink.ApplyChanges("customer-1", Fold(new(), 20, 2, 6), 6);
    }

    [Fact] void should_fail_the_first_call() => _failures.Count().ShouldEqual(1);
    [Fact] void should_not_publish_to_the_existing_sequence() => _destinations.Events(Store, Tenant, _reserved).Count.ShouldEqual(0);
    [Fact] void should_not_consult_storage() => _destinations.Storage.DidNotReceive().GetEventStores();
}
