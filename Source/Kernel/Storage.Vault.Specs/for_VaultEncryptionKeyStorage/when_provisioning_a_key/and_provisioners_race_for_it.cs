// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_provisioning_a_key;

[Collection(VaultCollection.Name)]
public class and_provisioners_race_for_it(VaultFixture fixture) : Specification
{
    const int Provisioners = 8;

    IEncryptionKeyStorage _store;
    EncryptionKeyIdentifier _identifier = Guid.NewGuid().ToString();
    EncryptionKeyAtRevision[] _results;
    EncryptionKeyAtRevision? _stored;

    void Establish() => _store = new VaultEncryptionKeyStorage(fixture.VaultAddress);

    async Task Because()
    {
        _results = await Task.WhenAll(Enumerable.Range(0, Provisioners).Select(provisioner =>
            _store.GetOrAddWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier, new EncryptionKey([(byte)provisioner], [(byte)(provisioner + 100)]))));
        _stored = await _store.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _identifier);
    }

    [Fact] void should_hand_every_provisioner_the_same_key() => _results.Select(_ => _.Key.Fingerprint).Distinct().Count().ShouldEqual(1);
    [Fact] void should_hand_every_provisioner_the_key_that_was_stored() => _results[0].Key.Fingerprint.ShouldEqual(_stored!.Key.Fingerprint);
    [Fact] void should_hand_every_provisioner_the_stored_private_key() => _results.All(_ => _.Key.Private.SequenceEqual(_stored!.Key.Private)).ShouldBeTrue();
    [Fact] void should_hand_every_provisioner_the_initial_revision() => _results.All(_ => _.Revision == EncryptionKeyRevision.Initial).ShouldBeTrue();
}
