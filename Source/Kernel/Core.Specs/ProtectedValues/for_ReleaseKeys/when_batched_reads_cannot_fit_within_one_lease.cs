// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_batched_reads_cannot_fit_within_one_lease : given.many_tracked_keys
{
    Exception _error;
    bool _handedOff;
    int _largestBatch;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock, TimeSpan.FromSeconds(5));
        TrackKeys(96);
        var completion = ReleaseKeys.Complete(() => _handedOff = true);
        while (!completion.IsCompleted)
        {
            await AwaitBatch();
            _largestBatch = Math.Max(_largestBatch, _pending.Count);
            FinishBatch();
        }

        _error = await Catch.Exception(() => completion);
    }

    [Fact] void should_complete_without_exhausting_the_overall_budget() => _error.ShouldBeNull();
    [Fact] void should_hand_off_healthy_protected_material() => _handedOff.ShouldBeTrue();
    [Fact] void should_refresh_every_key_together_after_two_batched_passes() => _largestBatch.ShouldEqual(96);
    [Fact] void should_reread_every_key_in_the_parallel_refresh() => _reads.ShouldEqual(288);
    [Fact] void should_finish_after_seven_read_latencies() => _clock.GetUtcNow().ShouldEqual(DateTimeOffset.UnixEpoch + TimeSpan.FromSeconds(7));
}
