// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_two_operations_fail : given.an_ordered_bulk_write
{
    void Establish() => _failureIndexes = [100, 700];
    async Task Because() => await Flush();
    [Fact] void should_resume_after_each_failure_without_reordering() => Sent((0, 1000), (101, 1000), (701, 1000)).ShouldBeTrue();
    [Fact] void should_report_both_failed_partitions() => Failed(100, 700).ShouldBeTrue();
}
