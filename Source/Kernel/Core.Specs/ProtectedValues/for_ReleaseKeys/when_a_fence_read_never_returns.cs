// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_a_fence_read_never_returns : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    readonly TaskCompletionSource<EncryptionKeyErasure?> _pending = new();
    Exception _error;

    void Establish() => _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier).Returns(_pending.Task);

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock, TimeSpan.FromSeconds(2));
        var release = Release(_keys);
        _clock.Advance(TimeSpan.FromSeconds(6));
        _error = await Catch.Exception(() => release);
    }

    [Fact] void should_time_out_using_the_configured_grace_and_clock() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_require_the_provider_to_finish() => _pending.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_retry_an_unknown_read_outcome() => _keys.Received(1).GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
}
