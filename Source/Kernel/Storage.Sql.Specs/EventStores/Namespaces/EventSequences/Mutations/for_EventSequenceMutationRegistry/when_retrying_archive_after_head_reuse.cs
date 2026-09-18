// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_retrying_archive_after_head_reuse : given.a_mutation_registry
{
    ArchiveRetryConformance _trace;

    async Task Establish()
    {
        _trace = new(_request, Request(_target, originSequenceNumber: 43), _proposedTarget);
        await _trace.Establish(_registry);
    }

    // The existing fixture keeps one SQLite connection alive across fresh registry/context wrappers.
    async Task Because() => await _trace.RetryArchive(new EventSequenceMutationRegistry(_eventStore, _namespace, _database));

    [Fact] void should_be_already_archived() => _trace.ShouldBeAlreadyArchived();
    [Fact] void should_return_the_original_receipt() => _trace.ShouldReturnOriginalReceipt();
    [Fact] void should_return_a_verified_receipt() => _trace.ShouldReturnVerifiedReceipt();
    [Fact] void should_keep_the_next_mutation_resumable() => _trace.ShouldKeepNextMutationResumable();
    [Fact] void should_keep_the_next_token_unchanged() => _trace.ShouldKeepNextToken();
    [Fact] void should_keep_the_next_ordinal_unchanged() => _trace.ShouldKeepNextOrdinal();
    [Fact] void should_keep_the_next_mutation_unchanged() => _trace.ShouldKeepNextMutation();
}
#endif
