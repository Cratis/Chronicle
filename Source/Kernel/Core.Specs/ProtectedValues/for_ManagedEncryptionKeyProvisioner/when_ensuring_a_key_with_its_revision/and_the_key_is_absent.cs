// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ManagedEncryptionKeyProvisioner.when_ensuring_a_key_with_its_revision;

public class and_the_key_is_absent : Specification
{
    InMemoryEncryptionKeyStorage _storage;
    IEncryption _encryption;
    ManagedEncryptionKeyProvisioner _provisioner;
    EncryptionKey _generated = new([1], [2]);
    EncryptionKeyAtRevision _result;

    void Establish()
    {
        _storage = new();
        _encryption = Substitute.For<IEncryption>();
        _encryption.GenerateKey().Returns(_generated);
        _provisioner = new(_storage, _encryption);
    }

    async Task Because() => _result = await _provisioner.EnsureKeyWithRevisionFor("store", "tenant", "subject");

    [Fact] void should_report_the_generated_key() => _result.Key.ShouldEqual(_generated);
    [Fact] void should_report_the_minted_revision() => _result.Revision.ShouldEqual(EncryptionKeyRevision.Initial);
    [Fact] async Task should_persist_the_reported_pair() => (await _storage.TryGetLatestFor("store", "tenant", "subject")).ShouldEqual(_result);
}
