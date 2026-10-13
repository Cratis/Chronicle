// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_an_open_bulk_window;

public class and_a_size_flush_has_reported_failed_partitions : when_flushing_a_bulk_write.given.an_ordered_bulk_write
{
    Exception? _error;

    void Establish() => _failureIndexes = [0];

    async Task Because()
    {
        await Flush();
        _error = await Catch.Exception(_sink.FlushBulk);
    }

    [Fact] void should_report_the_failed_partition_to_the_size_flush_caller() => Failed(0).ShouldBeTrue();
    [Fact] void should_not_confirm_durability_after_the_failure_is_claimed() => _error.ShouldBeOfExactType<BulkWindowHasFailedWrites>();
}
