// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_repairing_a_failed_bulk_write;

public class and_the_repair_fails : given.an_ordered_bulk_with_a_null_parent_failure
{
    void Establish() => _failRepair = true;

    async Task Because() => _failedPartitions = await _sink.FlushBulk();

    [Fact] void should_report_the_rejected_partition() => _failedPartitions.Single().EventSourceId.Value.ShouldEqual("probe-1");
    [Fact] void should_not_resume_the_suffix() => _batchSizes.ShouldContainOnly([3]);
}
