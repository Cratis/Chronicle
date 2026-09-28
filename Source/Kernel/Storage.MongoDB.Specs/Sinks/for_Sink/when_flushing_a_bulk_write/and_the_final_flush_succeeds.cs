// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_the_final_flush_succeeds : given.an_ordered_bulk_write
{
    IEnumerable<FailedPartition> _finalFailures;

    async Task Because() => _finalFailures = await FlushFinal();

    [Fact] void should_report_no_failed_partitions() => _finalFailures.ShouldBeEmpty();
    [Fact] void should_write_the_queued_operation() => _attempts.Count.ShouldEqual(1);
}
