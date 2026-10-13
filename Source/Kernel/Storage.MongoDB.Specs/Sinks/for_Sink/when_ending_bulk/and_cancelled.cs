// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_ending_bulk;

public class and_cancelled : given.a_sink_with_gated_bulk_writes
{
    IEnumerable<FailedPartition> _failures;

    async Task Establish()
    {
        _holdFirstFlush = false;
        await _sink.BeginBulk();
        await Apply(_firstKey, 1, 12);
        await Apply(_firstKey, 2, 13);
        await Apply(_secondKey, 1, 14);
    }

    async Task Because() => _failures = await _sink.EndBulk(new CancellationToken(true));

    [Fact] void should_report_all_affected_partitions() => _failures.Select(failure => failure.EventSourceId).ShouldContainOnly(_firstKey, _secondKey);
    [Fact] void should_resume_the_first_partition_from_its_earliest_event() => _failures.Single(failure => failure.EventSourceId == _firstKey).EventSequenceNumber.Value.ShouldEqual(12UL);
    [Fact] void should_report_an_unknown_outcome() => _failures.ShouldEachConformTo(failure => failure.Reason.Contains("outcome unknown", StringComparison.Ordinal));
}
