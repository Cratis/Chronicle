// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EncryptionKeyCacheGrainService;

public class when_a_successor_write_races_authorization_invalidation : given.a_key_cache
{
    TaskCompletionSource<EncryptionKeyErasure?> _fence;
    bool _signedDuring;
    bool _signedAfter;

    void Establish()
    {
        _fence = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _storage.GetErasureForRelease(_address.EventStore, _address.Namespace, _address.Identifier).Returns(_fence.Task);
    }

    async Task Because()
    {
        var invalidation = EncryptionKeyCacheGrainService.EvictFor(_storage, _cache, _address);
        _signedDuring = _operation.Acquire().TrySign("during"u8.ToArray(), out _);
        _fence.SetResult(new(7, ["previous-key-fingerprint"], true));
        await invalidation;
        _signedAfter = _operation.Acquire().TrySign("after"u8.ToArray(), out _);
    }

    [Fact] void should_allow_the_write_while_authorization_is_being_invalidated() => _signedDuring.ShouldBeTrue();
    [Fact] void should_not_revoke_the_live_successor() => _signedAfter.ShouldBeTrue();
}
