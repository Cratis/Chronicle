// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry;

/// <summary>
/// Requires permanent archive retry semantics after a complete archive and head reuse in real MongoDB.
/// </summary>
/// <param name="fixture">The fixture providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_retrying_archive_after_head_reuse(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    ArchiveRetryConformance _trace = null!;

    async Task Establish()
    {
        _trace = new(Request, BuildRequest(Target, originSequenceNumber: 43), ProposedTarget);
        await _trace.Establish(Registry);
    }

    async Task Because() => await _trace.RetryArchive(RegistryOver(Database));

    [Fact] void should_be_already_archived() => _trace.ShouldBeAlreadyArchived();
    [Fact] void should_return_the_original_receipt() => _trace.ShouldReturnOriginalReceipt();
    [Fact] void should_return_a_verified_receipt() => _trace.ShouldReturnVerifiedReceipt();
    [Fact] void should_keep_the_next_mutation_resumable() => _trace.ShouldKeepNextMutationResumable();
    [Fact] void should_keep_the_next_token_unchanged() => _trace.ShouldKeepNextToken();
    [Fact] void should_keep_the_next_ordinal_unchanged() => _trace.ShouldKeepNextOrdinal();
    [Fact] void should_keep_the_next_mutation_unchanged() => _trace.ShouldKeepNextMutation();
}
#endif
