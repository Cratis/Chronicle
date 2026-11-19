// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_non_generic_write_concern_error_occurs : given.an_ordered_bulk_write
{
    void Establish()
    {
        _nonGenericFailure = true;
        _writeConcernFailure = true;
    }

    async Task Because() => await Flush();
    [Fact] void should_not_resend_operations_with_an_unknown_outcome() => Sent((0, OperationCount)).ShouldBeTrue();
    [Fact] void should_report_every_partition_at_its_sequence_number() => Failed(Enumerable.Range(0, OperationCount).ToArray()).ShouldBeTrue();
    [Fact] void should_mark_every_partition_outcome_unknown() => _failedPartitions.All(_ => _.OutcomeUnknown).ShouldBeTrue();
    [Fact] void should_carry_the_write_concern_message_with_every_partition() => _failedPartitions.All(_ => _.Reason.Contains(WriteConcernErrorMessage)).ShouldBeTrue();
    [Fact] void should_not_carry_document_content_with_any_partition() => _failedPartitions.Any(_ => _.Reason.Contains(DocumentContent)).ShouldBeFalse();
}
