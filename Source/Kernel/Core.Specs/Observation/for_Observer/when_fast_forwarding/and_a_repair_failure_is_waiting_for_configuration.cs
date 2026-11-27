// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_fast_forwarding;

public class and_a_repair_failure_is_waiting_for_configuration : given.an_observer_behind_a_repair_barrier
{
    async Task Because()
    {
        await _observer.CatchUp();
        await _silo.RunCatchUpContinuation();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<Observers>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = true;
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_ =>
        {
            if (!first) return Task.FromResult(_observersConfig);
            first = false;
            entered.TrySetResult();
            return release.Task;
        });
        var acknowledgement = _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 11UL, succeeded: false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        Task fastForward;
        try
        {
            fastForward = _observer.FastForward();
            fastForward.IsCompleted.ShouldBeFalse();
            _stateStorage.State.PendingCatchUpRepairs!.ShouldBeEmpty();
        }
        finally
        {
            release.TrySetResult(_observersConfig);
        }
        await Task.WhenAll(acknowledgement, fastForward).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_recreate_the_discarded_failure() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_not_restore_the_discarded_obligation() => _stateStorage.State.PendingCatchUpRepairs!.ShouldBeEmpty();
    [Fact] void should_preserve_the_fast_forwarded_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
}
