// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_repairing_a_failed_bulk_write;

public class and_the_repair_succeeds : given.an_ordered_bulk_with_a_null_parent_failure
{
    async Task Because() => _failedPartitions = await _sink.FlushBulk();

    [Fact] void should_repair_the_rejected_write() => _repairAttempts.ShouldEqual(2);
    [Fact] void should_repair_only_the_rejected_key() => _repairFilters.TrueForAll(filter => filter.ToJson().Contains("probe-1") && !filter.ToJson().Contains("probe-0") && !filter.ToJson().Contains("probe-2")).ShouldBeTrue();
    [Fact] void should_retry_only_the_rejected_write_and_resume_the_suffix() => _batchSizes.ShouldContainOnly([3, 1, 1]);
    [Fact] void should_not_report_a_failed_partition() => _failedPartitions.ShouldBeEmpty();
}
