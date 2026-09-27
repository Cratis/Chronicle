// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_write_concern_error_occurs : given.an_ordered_bulk_write
{
    void Establish() => _writeConcernFailure = true;
    async Task Because() => await Flush();
    [Fact] void should_not_resend_operations_with_an_unknown_outcome() => Sent((0, 1000)).ShouldBeTrue();
    [Fact] void should_report_every_partition_as_uncertain() => Failed(Enumerable.Range(0, OperationCount).ToArray()).ShouldBeTrue();
}
