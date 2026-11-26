// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

[Collection(VaultCollection.Name)]
public class and_none_exists_yet(VaultFixture fixture) : Specification
{
    readonly EventStoreName _eventStore = $"test-store-{Guid.NewGuid():N}";
    readonly EncryptionKey _candidate = new([1, 2, 3], [4, 5, 6]);
    IEncryptionKeyStorage _storage;
    EncryptionKey _first;
    EncryptionKey _second;
    readonly EncryptionKey _otherCandidate = new([9], [10]);
    EncryptionKey _otherPurpose;
    bool _storedApart;

    void Establish() => _storage = new VaultEncryptionKeyStorage(fixture.VaultAddress);

    async Task Because()
    {
        _first = await _storage.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.FederationAuthority, _candidate);
        _second = await _storage.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.FederationAuthority, new([7], [8]));
        _otherPurpose = await _storage.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.MutationReceiptDigest, _otherCandidate);
        _storedApart = await new given.raw_vault(fixture.VaultAddress).Exists($"k1/~internal/{_eventStore}/{InternalSecretPurpose.FederationAuthority}");
    }

    [Fact] void should_store_the_candidate() => _first.Private.ShouldEqual(_candidate.Private);
    [Fact] void should_keep_the_stored_secret_when_asked_again() => _second.Private.ShouldEqual(_candidate.Private);
    [Fact] void should_keep_each_purpose_apart() => _otherPurpose.Private.ShouldEqual(_otherCandidate.Private);
    [Fact] void should_store_it_beneath_the_internal_segment() => _storedApart.ShouldBeTrue();
}
