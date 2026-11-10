// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_handing_over;

/// <summary>
/// A start-before-stop rollout: the replacement starts while the current kernel still holds the lease. The
/// replacement's silo lifecycle stage does not complete, so nothing after it (gateway, rehydration, event
/// processing) runs until the current kernel has stopped and released the lease.
/// </summary>
public class and_standby_is_enabled : given.two_kernels_sharing_one_lease
{
    bool _currentHeldWhileReplacementWaited;
    bool _replacementHeldWhileCurrentRan;
    bool _replacementWaitedWhileCurrentRan;
    bool _replacementStartedWhileCurrentRan;
    bool _currentHeldAfterStopping;
    bool _replacementHeldBeforeAcquiring;
    Task _replacementStart;

    async Task Because()
    {
        await CurrentLifecycle.OnStart(CancellationToken.None);

        var waiting = Time.WhenNextTimer();
        _replacementStart = ReplacementLifecycle.OnStart(CancellationToken.None);
        await waiting;
        _currentHeldWhileReplacementWaited = Current.IsHeld;
        _replacementHeldWhileCurrentRan = Replacement.IsHeld;
        _replacementWaitedWhileCurrentRan = Replacement.IsWaiting;
        _replacementStartedWhileCurrentRan = _replacementStart.IsCompleted;

        await CurrentLifecycle.OnStop(CancellationToken.None);
        _currentHeldAfterStopping = Current.IsHeld;
        _replacementHeldBeforeAcquiring = Replacement.IsHeld;

        var acquired = Time.WhenNextTimer();
        Time.Advance(TimeSpan.FromSeconds(2));
        await acquired;
        await _replacementStart;
    }

    [Fact] void should_keep_the_current_kernel_as_the_only_writer() => _currentHeldWhileReplacementWaited.ShouldBeTrue();
    [Fact] void should_not_let_the_replacement_hold_while_the_current_kernel_runs() => _replacementHeldWhileCurrentRan.ShouldBeFalse();
    [Fact] void should_report_the_replacement_as_waiting_for_standby() => _replacementWaitedWhileCurrentRan.ShouldBeTrue();
    [Fact] void should_not_start_the_replacement_lifecycle_while_the_current_kernel_runs() => _replacementStartedWhileCurrentRan.ShouldBeFalse();
    [Fact] void should_release_the_lease_when_the_current_kernel_stops() => _currentHeldAfterStopping.ShouldBeFalse();
    [Fact] void should_not_hold_before_the_replacement_acquires() => _replacementHeldBeforeAcquiring.ShouldBeFalse();
    [Fact] void should_hand_the_lease_to_the_replacement() => Replacement.IsHeld.ShouldBeTrue();
    [Fact] void should_end_the_replacements_wait() => Replacement.IsWaiting.ShouldBeFalse();
    [Fact] void should_not_leave_the_previous_kernel_holding() => Current.IsHeld.ShouldBeFalse();
}
