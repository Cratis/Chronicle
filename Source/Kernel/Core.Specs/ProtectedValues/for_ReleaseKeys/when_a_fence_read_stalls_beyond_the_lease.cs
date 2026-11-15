// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_a_fence_read_stalls_beyond_the_lease : given.a_key_store
{
    readonly given.a_release_clock _clock = new();
    readonly TaskCompletionSource<EncryptionKeyErasure?> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EncryptionKey _result;
    int _reads;

    void Establish() => _keys.GetErasureForRelease(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier)
        .Returns(_ => ++_reads == 1 ? _pending.Task : Task.FromResult<EncryptionKeyErasure?>(new(2u, [], false, 1)));

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        var release = Release(_keys);
        _clock.Advance(TimeSpan.FromSeconds(6));
        _pending.SetResult(null);
        _result = await release;
    }

    [Fact] void should_measure_the_lease_from_read_start() => _result.ShouldBeNull();
    [Fact] void should_refresh_the_stalled_read() => _reads.ShouldEqual(2);
}
