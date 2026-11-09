// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage;

public class when_the_authority_loses_durability : Specification
{
    IEncryptionKeyFence _authority;
    AzureKeyVaultEncryptionKeyStorage _storage;
    EncryptionKeyFenceGuarantees _whileDurable;
    EncryptionKeyFenceGuarantees _afterwards;

    void Establish()
    {
        _authority = Substitute.For<IEncryptionKeyFence>();
        _authority.IsDurable.Returns(true);
        _storage = new(Substitute.For<SecretClient>(), null, _authority);
    }

    void Because()
    {
        _whileDurable = _storage.FenceGuarantees;
        _authority.IsDurable.Returns(false);
        _afterwards = _storage.FenceGuarantees;
    }

    [Fact] void should_promise_the_fence_guarantees_while_durable() => _whileDurable.ShouldEqual(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);
    [Fact] void should_promise_nothing_once_the_authority_is_not_durable() => _afterwards.ShouldEqual(EncryptionKeyFenceGuarantees.None);
}
