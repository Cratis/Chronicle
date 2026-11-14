// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ReleaseKeys;

public class when_one_of_many_fence_reads_never_returns : given.many_tracked_keys
{
    Exception _error;
    bool _handedOff;
    bool _stalledReadCompleted;

    async Task Because()
    {
        using var scope = ReleaseKeys.BeginScope(_clock);
        TrackKeys();
        var completion = ReleaseKeys.Complete(() => _handedOff = true);
        await AwaitBatch();
        var stalled = _pending[0];
        _pending.RemoveAt(0);
        FinishBatch();
        _clock.Advance(TimeSpan.FromSeconds(14));
        _error = await Catch.Exception(() => completion);
        _stalledReadCompleted = stalled.Task.IsCompleted;
    }

    [Fact] void should_fail_closed_at_the_shared_deadline() => _error.ShouldBeOfExactType<EncryptionKeyFenceUnavailable>();
    [Fact] void should_not_hand_off_protected_material() => _handedOff.ShouldBeFalse();
    [Fact] void should_not_wait_for_the_stalled_provider() => _stalledReadCompleted.ShouldBeFalse();
    [Fact] void should_not_start_another_batch_after_the_deadline() => _reads.ShouldEqual(32);
}
