// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_failed_operation_has_no_metadata : given.an_ordered_bulk_write
{
    void Establish()
    {
        _failureIndexes = [500];
        _missingMetadataIndex = 500;
    }

    async Task Because() => await Flush();
    [Fact] void should_resume_the_other_operations() => Sent((0, 1000), (501, 1000)).ShouldBeTrue();
    [Fact] void should_not_report_an_unknown_partition() => _failedPartitions.ShouldBeEmpty();
}
