// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyCacheGrainService;

public class when_repeating_authorization_after_a_successor_write : given.a_key_cache
{
    bool _signedAfterFirst;
    bool _signedAfterSecond;

    void Establish() => _storage.GetErasureForRelease(_address.EventStore, _address.Namespace, _address.Identifier)
        .Returns(new EncryptionKeyErasure(7, ["previous-key-fingerprint"], true));

    async Task Because()
    {
        await EncryptionKeyCacheGrainService.EvictFor(_storage, _cache, _address);
        _signedAfterFirst = _operation.Acquire().TrySign("first"u8.ToArray(), out _);
        await EncryptionKeyCacheGrainService.EvictFor(_storage, _cache, _address);
        _signedAfterSecond = _operation.Acquire().TrySign("second"u8.ToArray(), out _);
    }

    [Fact] void should_allow_the_authorized_successor() => _signedAfterFirst.ShouldBeTrue();
    [Fact] void should_allow_the_same_live_key_after_repeated_authorization() => _signedAfterSecond.ShouldBeTrue();
}
