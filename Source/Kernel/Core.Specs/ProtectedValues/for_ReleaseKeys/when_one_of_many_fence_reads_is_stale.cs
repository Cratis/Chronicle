// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_one_of_many_fence_reads_is_stale : given.many_tracked_keys
{
    Exception _error;
    bool _handedOff;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        TrackKeys();
        var completion = ReleaseKeys.Complete(() => _handedOff = true);
        await AwaitBatch();
        var stalled = _pending[0];
        _pending.RemoveAt(0);
        FinishBatch();
        _clock.Advance(TimeSpan.FromSeconds(2));
        stalled.SetResult(null);
        await AwaitBatch(1);
        FinishBatch(new EncryptionKeyErasure(2u, [], false, 1));
        await AwaitBatch();
        FinishBatch();
        _error = await Catch.Exception(() => completion);
    }

    [Fact] void should_observe_erasure_in_the_refreshed_fence() => _error.ShouldBeOfExactType<EncryptionKeyErased>();
    [Fact] void should_not_hand_off_protected_material() => _handedOff.ShouldBeFalse();
    [Fact] void should_refresh_the_stale_read() => _reads.ShouldEqual(65);
}
