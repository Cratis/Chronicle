// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_destination_is_not_reserved : given.a_sink
{
    static readonly EventSequenceId _custom = new("public-totals");

    IEnumerable<Storage.Sinks.FailedPartition> _failures;

    protected override EventSequenceId? Configured => _custom;

    async Task Because()
    {
        _failures = await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);
        await _sink.ApplyChanges("customer-1", Fold(new(), 20, 2, 6), 6);
    }

    [Fact] void should_not_fail() => _failures.ShouldBeEmpty();
    [Fact] void should_publish_every_step() => _destinations.Events(Store, Tenant, _custom).Count.ShouldEqual(2);
    [Fact] void should_check_the_name_only_once() => _destinations.ReservedIds.Received(1).IsReserved(_custom);
}
