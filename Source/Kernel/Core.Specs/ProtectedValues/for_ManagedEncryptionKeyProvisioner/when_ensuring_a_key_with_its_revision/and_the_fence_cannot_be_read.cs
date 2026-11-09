// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.ProtectedValues.for_ManagedEncryptionKeyProvisioner.when_ensuring_a_key_with_its_revision;

public class and_the_fence_cannot_be_read : Specification
{
    IEncryptionKeyStorage _storage;
    IEncryption _encryption;
    ManagedEncryptionKeyProvisioner _provisioner;
    Exception _error;

    void Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _encryption = Substitute.For<IEncryption>();
        _storage.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject").ThrowsAsync(new InvalidOperationException());
        _provisioner = new(_storage, _encryption);
    }

    async Task Because() => _error = await Catch.Exception(() => _provisioner.EnsureKeyWithRevisionFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject"));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_generate_a_key() => _encryption.DidNotReceive().GenerateKey();
    [Fact] void should_not_provision_a_key() => _storage.DidNotReceiveWithAnyArgs().GetOrAddWithRevisionFor(default!, default!, default!, default!);
}
