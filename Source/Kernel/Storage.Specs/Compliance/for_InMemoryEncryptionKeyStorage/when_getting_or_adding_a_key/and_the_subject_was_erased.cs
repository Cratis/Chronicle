// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_getting_or_adding_a_key;

/// <summary>
/// The resurrection at the heart of the report: deleting a key leaves exactly the absence that "never provisioned"
/// leaves, so the next append quietly mints a fresh revision 1 and protection restarts for a person who asked to be
/// forgotten. The fence turns that into a refusal.
/// </summary>
/// <remarks>
/// Failing loudly is the point. Blanking the value instead would be silent data loss indistinguishable from the
/// erasure itself, and minting a key would be the defect. A caller who has a lawful reason to protect this person
/// again authorizes it explicitly.
/// </remarks>
public class and_the_subject_was_erased : given.an_in_memory_key_store
{
    Exception _error;
    EncryptionKey? _latestAfterwards;
    int _revisionCount;

    async Task Establish()
    {
        await Provision(KeyNamed("original"));
        await Erase();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => Provision(KeyNamed("fresh")));
        _latestAfterwards = await Latest();
        _revisionCount = await RevisionCount();
    }

    [Fact] void should_refuse_to_provision_a_key() => _error.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_not_mint_a_key() => _latestAfterwards.ShouldBeNull();
    [Fact] void should_leave_no_revision_behind() => _revisionCount.ShouldEqual(0);
    [Fact] void should_report_the_revision_the_erasure_covered() => ((EncryptionKeyErased)_error).Erasure.ErasedThrough.ShouldEqual(EncryptionKeyRevision.Initial);
}
