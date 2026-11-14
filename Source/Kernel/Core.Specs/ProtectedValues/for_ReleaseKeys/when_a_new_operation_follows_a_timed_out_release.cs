// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_a_new_operation_follows_a_timed_out_release : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    readonly TaskCompletionSource<EncryptionKeyErasure?> _pending = new();
    Exception _firstError;
    EncryptionKey _result;
    bool _handedOff;

    async Task Because()
    {
        _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_pending.Task);
        using (ReleaseKeys.BeginScope(_clock))
        {
            var release = Release(_keys);
            _clock.Advance(TimeSpan.FromSeconds(15));
            _firstError = await Catch.Exception(() => release);
        }

        _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(Task.FromResult<EncryptionKeyErasure?>(null));
        using (ReleaseKeys.BeginScope(_clock))
        {
            _result = await Release(_keys);
            await ReleaseKeys.Complete(() => _handedOff = true);
        }
    }

    [Fact] void should_fail_the_first_release_closed() => _firstError.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_read_a_new_fence_in_the_next_operation() => _keys.Received(2).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
    [Fact] void should_release_the_key_in_the_next_operation() => _result.ShouldEqual(_pair.Key);
    [Fact] void should_allow_the_next_handoff() => _handedOff.ShouldBeTrue();
}
