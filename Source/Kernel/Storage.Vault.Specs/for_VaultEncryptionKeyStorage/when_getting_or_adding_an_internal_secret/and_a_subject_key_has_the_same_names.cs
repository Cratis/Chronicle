// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

/// <summary>
/// A subject key addressed with the internal segment and the purpose as its names must not reach the internal secret.
/// </summary>
/// <param name="fixture">The <see cref="VaultFixture"/> providing the Vault container.</param>
[Collection(VaultCollection.Name)]
public class and_a_subject_key_has_the_same_names(VaultFixture fixture) : Specification
{
    readonly EventStoreName _eventStore = $"test-store-{Guid.NewGuid():N}";
    IEncryptionKeyStorage _storage;
    readonly EncryptionKey _candidate = new([1], [2]);
    EncryptionKey _secret;
    EncryptionKeyAtRevision? _subjectKey;

    void Establish() => _storage = new VaultEncryptionKeyStorage(fixture.VaultAddress);

    async Task Because()
    {
        _secret = await _storage.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.FederationAuthority, _candidate);
        _subjectKey = await _storage.TryGetLatestFor("~internal", _eventStore.Value, InternalSecretPurpose.FederationAuthority.Value);
    }

    [Fact] void should_not_find_the_secret_as_a_subject_key() => _subjectKey.ShouldBeNull();
    [Fact] void should_still_hold_the_secret() => _secret.Private.ShouldEqual(_candidate.Private);
}
