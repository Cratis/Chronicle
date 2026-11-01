// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_applying_changes;

public class and_the_destination_contains_the_key_separator : given.a_sink
{
    static readonly EventSequenceId _smuggled = new("observers#store#tenant");

    IEnumerable<Storage.Sinks.FailedPartition> _failures;

    protected override EventSequenceId? Configured => _smuggled;

    async Task Because() => _failures = await _sink.ApplyChanges("customer-1", Fold(new(), 10, 1, 5), 5);

    [Fact] void should_report_a_failed_partition() => _failures.Count().ShouldEqual(1);
    [Fact] void should_name_the_destination() => _failures.First().Reason.ShouldContain("observers#store#tenant");
    [Fact] void should_not_append_anything() => _destinations.AppendAttempts.ShouldEqual(0);
}
