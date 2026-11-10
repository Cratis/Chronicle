// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_SubscriptionAdmission;

public class when_trying_to_enter_without_waiting : Specification
{
    readonly SubscriptionAdmission _admission = new(2, Environment.ProcessorCount);
    bool _firstEntered;
    bool _secondEntered;
    bool _thirdEntered;
    bool _queuedWaiterWasServedFirst;
    bool _enteredAheadOfWaiter;

    async Task Because()
    {
        _firstEntered = _admission.TryEnter();
        _secondEntered = await _admission.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _thirdEntered = _admission.TryEnter();

        var waiter = _admission.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);
        _admission.Release();
        _queuedWaiterWasServedFirst = await waiter.WaitAsync(TimeSpan.FromSeconds(30));
        _enteredAheadOfWaiter = _admission.TryEnter();

        _admission.Release();
        _admission.Release();
    }

    [Fact] void should_enter_while_free() => _firstEntered.ShouldBeTrue();
    [Fact] void should_share_the_limit_with_waiting_callers() => _secondEntered.ShouldBeTrue();
    [Fact] void should_not_enter_at_capacity() => _thirdEntered.ShouldBeFalse();
    [Fact] void should_hand_a_release_to_a_queued_waiter() => _queuedWaiterWasServedFirst.ShouldBeTrue();
    [Fact] void should_not_overtake_the_queued_waiter() => _enteredAheadOfWaiter.ShouldBeFalse();
}
