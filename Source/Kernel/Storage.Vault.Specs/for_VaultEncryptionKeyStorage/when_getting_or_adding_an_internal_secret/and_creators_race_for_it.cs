// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_getting_or_adding_an_internal_secret;

[Collection(VaultCollection.Name)]
public class and_creators_race_for_it(VaultFixture fixture) : Specification
{
    const int Creators = 8;

    readonly EventStoreName _eventStore = $"test-store-{Guid.NewGuid():N}";
    IEncryptionKeyStorage _storage;
    EncryptionKey[] _results;
    EncryptionKey _stored;

    void Establish() => _storage = new VaultEncryptionKeyStorage(fixture.VaultAddress);

    async Task Because()
    {
        _results = await Task.WhenAll(Enumerable.Range(0, Creators).Select(creator =>
            _storage.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.FederationAuthority, new([(byte)creator], [(byte)(creator + 100)]))));
        _stored = await _storage.GetOrAddInternalSecretFor(_eventStore, InternalSecretPurpose.FederationAuthority, new([200], [201]));
    }

    [Fact] void should_hand_every_creator_the_stored_secret() => _results.All(_ => _.Private.SequenceEqual(_stored.Private)).ShouldBeTrue();
}
