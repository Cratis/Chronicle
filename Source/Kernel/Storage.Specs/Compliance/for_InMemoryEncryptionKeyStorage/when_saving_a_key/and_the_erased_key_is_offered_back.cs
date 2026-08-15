// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_saving_a_key;

/// <summary>
/// Writing the key straight back in is how a composed store heals a survivor into a member that was erased, and how
/// a cross-event-store copy reinstates the pre-erasure material. Both arrive here, as a save.
/// </summary>
/// <remarks>
/// The revision floor refuses the erased incarnation, and the fingerprint refuses the exact destroyed material at
/// any revision at all - so neither writing it back where it was, nor sliding it in above the floor, works.
/// </remarks>
public class and_the_erased_key_is_offered_back : given.an_in_memory_key_store
{
    EncryptionKey _original;
    Exception _atTheErasedRevision;
    Exception _aboveTheFence;
    EncryptionKey? _latestAfterwards;

    async Task Establish()
    {
        _original = KeyNamed("original");
        await Provision(_original);
        await Erase();
    }

    async Task Because()
    {
        _atTheErasedRevision = await Catch.Exception(() => Save(_original, EncryptionKeyRevision.Initial));
        _aboveTheFence = await Catch.Exception(() => Save(_original, new EncryptionKeyRevision(7u)));
        _latestAfterwards = await Latest();
    }

    [Fact] void should_refuse_the_erased_revision() => _atTheErasedRevision.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_refuse_the_erased_key_material_above_the_fence_too() => _aboveTheFence.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_leave_the_subject_without_a_key() => _latestAfterwards.ShouldBeNull();
}
