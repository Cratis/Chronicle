// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_the_final_flush_fails : given.an_ordered_bulk_write
{
    FailedPartition[] _finalFailures;

    void Establish() => _failureIndexes = [0];

    async Task Because() => _finalFailures = (await FlushFinal()).ToArray();

    [Fact] void should_report_the_failed_partition() => _finalFailures.Single().EventSourceId.ShouldEqual(KeyFor(0));
    [Fact] void should_report_the_failed_sequence_number() => _finalFailures.Single().EventSequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_flush_only_at_the_end() => _attempts.Count.ShouldEqual(1);
}
