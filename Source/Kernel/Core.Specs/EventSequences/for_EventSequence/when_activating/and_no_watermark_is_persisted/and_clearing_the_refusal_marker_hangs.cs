// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_activating.and_no_watermark_is_persisted;

public class and_clearing_the_refusal_marker_hangs : given.a_catching_up_sequence
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception? _error;

    async Task Establish()
    {
        await TransferHistory();
        _snapshots.BeforeWrite = () =>
        {
            if (_claimWrites == 0 || _stateStorage.State.ConstraintIndexCatchUpRefusing is not null) return Task.CompletedTask;

            // The storage double deliberately hangs until teardown; no latency assumption drives the deadline.
            _clock.Advance(TimeSpan.FromMilliseconds(250));
            return _release.Task;
        };
    }

    async Task Because()
    {
        _error = await Catch.Exception(async () => await Tick().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    void Destroy() => _release.TrySetResult();

    [Fact] void should_bound_the_marker_snapshot_inside_the_turn() => _error.ShouldBeNull();
    [Fact] void should_keep_refusing_until_the_clear_is_confirmed() => _stateStorage.State.ConstraintIndexCatchUpRefusing.ShouldNotBeNull();
}
