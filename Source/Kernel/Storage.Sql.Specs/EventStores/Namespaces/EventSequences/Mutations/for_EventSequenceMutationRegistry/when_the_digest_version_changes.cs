// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_the_digest_version_changes : given.a_mutation_registry
{
    DigestVersionConformance _trace;

    void Establish() => _trace = new(_request, Request(_target, originSequenceNumber: 43), _proposedTarget);

    async Task Because() => await _trace.Run(_registry, () => new EventSequenceMutationRegistry(_eventStore, _namespace, _database));

    [Fact] void should_resume_the_active_head() => _trace.ShouldResumeTheActiveHead();
    [Fact] void should_keep_the_active_head_version() => _trace.ShouldKeepTheActiveHeadVersion();
    [Fact] void should_archive_under_the_definition_version() => _trace.ShouldArchiveUnderTheDefinitionVersion();
    [Fact] void should_reserve_under_the_new_write_version() => _trace.ShouldReserveUnderTheNewWriteVersion();
    [Fact] void should_archive_the_next_under_the_new_write_version() => _trace.ShouldArchiveTheNextUnderTheNewWriteVersion();
    [Fact] void should_retry_the_first_receipt_exactly() => _trace.ShouldRetryTheFirstReceiptExactly();
    [Fact] void should_return_the_original_first_receipt() => _trace.ShouldReturnTheOriginalFirstReceipt();
    [Fact] void should_replay_the_first_registration() => _trace.ShouldReplayTheFirstRegistration();
    [Fact] void should_replay_the_original_first_receipt() => _trace.ShouldReplayTheOriginalFirstReceipt();
    [Fact] void should_retry_the_second_receipt_after_switching_back() => _trace.ShouldRetryTheSecondReceiptAfterSwitchingBack();
    [Fact] void should_fail_closed_on_an_unsupported_receipt_retry() => _trace.ShouldFailClosedOnAnUnsupportedReceiptRetry();
    [Fact] void should_fail_closed_on_an_unsupported_begin() => _trace.ShouldFailClosedOnAnUnsupportedBegin();
    [Fact] void should_keep_the_first_receipt_readable() => _trace.ShouldKeepTheFirstReceiptReadable();
}
