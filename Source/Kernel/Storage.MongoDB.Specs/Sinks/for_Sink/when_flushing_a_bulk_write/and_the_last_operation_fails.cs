// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_the_last_operation_fails : given.an_ordered_bulk_write
{
    void Establish() => _failureIndexes = [999];
    async Task Because() => await Flush();
    [Fact] void should_not_resend_an_operation() => Sent((0, 1000)).ShouldBeTrue();
    [Fact] void should_report_only_the_failed_partition() => Failed(999).ShouldBeTrue();
}
