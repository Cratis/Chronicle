// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_non_generic_write_error_occurs : given.an_ordered_bulk_write
{
    void Establish()
    {
        _nonGenericFailure = true;
        _failureIndexes = [500];
    }

    async Task Because() => await Flush();
    [Fact] void should_not_resume_without_a_generic_result_identifying_the_processed_prefix() => Sent((0, OperationCount)).ShouldBeTrue();
    [Fact] void should_report_every_partition_at_its_sequence_number() => Failed(Enumerable.Range(0, OperationCount).ToArray()).ShouldBeTrue();
    [Fact] void should_not_mark_any_partition_outcome_unknown() => _failedPartitions.Any(_ => _.OutcomeUnknown).ShouldBeFalse();
    [Fact] void should_carry_the_write_error_message_with_the_failed_operation() => _failedPartitions[500].Reason.ShouldContain(WriteErrorMessage);
    [Fact] void should_not_carry_document_content_with_any_partition() => _failedPartitions.Any(_ => _.Reason.Contains(DocumentContent)).ShouldBeFalse();
}
