// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance.for_InMemoryEncryptionKeyStorage.when_recording_an_erasure;

/// <summary>
/// "Erased" and "never provisioned" look identical to a key store that only holds key material, and that is the
/// whole defect - so the fence has to be readable as a distinct state even where there was never a key to destroy.
/// </summary>
/// <remarks>
/// The erasure fans out to every event store in the namespace, including ones that never held this subject's key,
/// which is what stops a later forwarded event making one of them the place the subject gets a key again. The
/// floor is still the initial revision, so a mint at revision 1 is refused rather than allowed through a gap.
/// </remarks>
public class and_the_subject_was_never_provisioned : given.an_in_memory_key_store
{
    EncryptionKeyErasure? _beforeErasure;
    EncryptionKeyErasure? _afterErasure;
    Exception _provisioningError;

    async Task Establish() => _beforeErasure = await Erasure();

    async Task Because()
    {
        await Erase();
        _afterErasure = await Erasure();
        _provisioningError = await Catch.Exception(() => Provision(KeyNamed("fresh")));
    }

    [Fact] void should_report_nothing_before_the_erasure() => _beforeErasure.ShouldBeNull();
    [Fact] void should_report_an_erasure_afterwards() => _afterErasure.ShouldNotBeNull();
    [Fact] void should_fence_the_initial_revision() => _afterErasure!.ErasedThrough.ShouldEqual(EncryptionKeyRevision.Initial);
    [Fact] void should_fence_no_key_material() => _afterErasure!.ErasedKeyFingerprints.ShouldBeEmpty();
    [Fact] void should_refuse_to_provision_a_key() => _provisioningError.ShouldBeOfExactType<EncryptionKeyErased>();
}
