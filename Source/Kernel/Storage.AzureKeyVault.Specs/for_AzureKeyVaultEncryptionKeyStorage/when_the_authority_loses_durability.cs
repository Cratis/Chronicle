// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage;

public class when_the_authority_loses_durability : Specification
{
    IEncryptionKeyFence _authority;
    AzureKeyVaultEncryptionKeyStorage _storage;
    EncryptionKeyFenceGuarantees _whileDurable;
    EncryptionKeyFenceGuarantees _afterwards;
    Exception _confirmation;
    Exception _release;
    Exception _authorization;

    void Establish()
    {
        _authority = Substitute.For<IEncryptionKeyFence>();
        _authority.IsDurable.Returns(true);
        _storage = new(Substitute.For<SecretClient>(), null, _authority);
    }

    async Task Because()
    {
        _whileDurable = _storage.FenceGuarantees;
        _authority.IsDurable.Returns(false);
        _afterwards = _storage.FenceGuarantees;
        _confirmation = await Catch.Exception(() => _storage.ConfirmErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject", "request"));
        _release = await Catch.Exception(() => _storage.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject"));
        _authorization = await Catch.Exception(() => _storage.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "subject", 0, 1u));
    }

    [Fact] void should_refuse_confirmation() => _confirmation.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_refuse_release() => _release.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_refuse_authorization() => _authorization.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_promise_the_fence_guarantees_while_durable() => _whileDurable.ShouldEqual(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
    [Fact] void should_promise_nothing_once_the_authority_is_not_durable() => _afterwards.ShouldEqual(EncryptionKeyFenceGuarantees.None);
}
