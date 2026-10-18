// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ManagedEncryptionKeyProvisioner.when_ensuring_a_key_with_its_revision;

public class and_the_key_exists : Specification
{
    IEncryptionKeyStorage _storage;
    IEncryption _encryption;
    ManagedEncryptionKeyProvisioner _provisioner;
    EncryptionKeyAtRevision _existing;
    EncryptionKeyAtRevision _result;

    void Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _encryption = Substitute.For<IEncryption>();
        _existing = new(new EncryptionKey([1], [2]), new EncryptionKeyRevision(7u));
        _storage.TryGetLatestFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject").Returns(_existing);
        _provisioner = new(_storage, _encryption);
    }

    async Task Because() => _result = await _provisioner.EnsureKeyWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject");

    [Fact] void should_return_the_pair_read() => _result.ShouldEqual(_existing);
    [Fact] void should_not_generate_a_key() => _encryption.DidNotReceive().GenerateKey();
    [Fact] void should_not_provision_a_key() => _storage.DidNotReceiveWithAnyArgs().GetOrAddWithRevisionFor(default!, default!, default!, default!);
}
