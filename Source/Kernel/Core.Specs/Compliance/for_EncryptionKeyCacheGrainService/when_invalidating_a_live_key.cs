// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyCacheGrainService;

public class when_invalidating_a_live_key : given.a_key_cache
{
    bool _signed;
    bool _oldSigned;

    async Task Because()
    {
        await EncryptionKeyCacheGrainService.EvictFor(_storage, _cache, _address);
        _oldSigned = _original.TrySign("value"u8.ToArray(), out _);
        _signed = _operation.Acquire().TrySign("value"u8.ToArray(), out _);
    }

    [Fact] void should_allow_signing_again() => _signed.ShouldBeTrue();
    [Fact] void should_dispose_the_invalidated_signer() => _oldSigned.ShouldBeFalse();
    [Fact] void should_evict_the_local_managed_key_cache() => ((IEvictEncryptionKeyCache)_storage).Received(1).EvictFromCache(_address.EventStore, _address.Namespace, _address.Identifier);
}
