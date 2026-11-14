// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_completing_many_timely_fence_reads : given.many_tracked_keys
{
    bool _handedOff;
    int _firstBatchSize;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        TrackKeys();
        var completion = ReleaseKeys.Complete(() => _handedOff = true);
        await AwaitBatch();
        _firstBatchSize = _pending.Count;
        FinishBatch();
        await AwaitBatch();
        FinishBatch();
        await completion;
    }

    [Fact] void should_hand_off_while_every_snapshot_is_fresh() => _handedOff.ShouldBeTrue();
    [Fact] void should_bound_concurrent_provider_reads() => _firstBatchSize.ShouldEqual(32);
    [Fact] void should_validate_every_key() => _reads.ShouldEqual(64);
    [Fact] void should_finish_in_two_read_latencies_instead_of_sixty_four() => _clock.GetUtcNow().ShouldEqual(DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds(2));
}
