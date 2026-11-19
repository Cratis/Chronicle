// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_flushing_a_bulk_write;

public class and_a_resumed_suffix_has_a_write_concern_error : given.an_ordered_bulk_write
{
    void Establish() => _failureIndexes = [100];
    protected override Key KeyFor(int index) => index is 101 or 700 ? new Key("key-100", ArrayIndexers.NoIndexers) : base.KeyFor(index);
    protected override bool WriteConcernFailureFor(int attempt) => attempt == 2;
    async Task Because() => await Flush();
    [Fact] void should_send_only_the_suffix_excluding_the_previously_failed_partition() => SentExcluding([[], [101, 700]], (0, OperationCount), (101, OperationCount)).ShouldBeTrue();
    [Fact] void should_report_each_failed_partition_once_at_its_sequence_number() => Failed(Enumerable.Range(100, OperationCount - 100).Except([101, 700]).ToArray()).ShouldBeTrue();
    [Fact] void should_keep_the_original_write_failure_outcome_known() => _failedPartitions[0].OutcomeUnknown.ShouldBeFalse();
    [Fact] void should_keep_the_original_write_failure_reason() => _failedPartitions[0].Reason.ShouldContain(WriteErrorMessage);
    [Fact] void should_mark_every_resubmitted_partition_outcome_unknown() => _failedPartitions.Skip(1).All(_ => _.OutcomeUnknown).ShouldBeTrue();
    [Fact] void should_carry_the_write_concern_message_with_every_resubmitted_partition() => _failedPartitions.Skip(1).All(_ => _.Reason.Contains(WriteConcernErrorMessage)).ShouldBeTrue();
}
