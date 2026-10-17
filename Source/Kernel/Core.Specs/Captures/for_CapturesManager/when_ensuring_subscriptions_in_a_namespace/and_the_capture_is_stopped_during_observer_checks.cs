// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_capture_is_stopped_during_observer_checks : given.a_captures_manager
{
    readonly TaskCompletionSource<bool> _subscriptionCheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _checking = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        StartedState(_events);
        _observer.IsSubscribed().Returns(_ =>
        {
            _checking.TrySetResult();
            return _subscriptionCheck.Task;
        });
    }

    async Task Because()
    {
        var pass = _manager.EnsureSubscribedIn("tenant");
        await _checking.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.Stop(_events.Id);
        _subscriptionCheck.TrySetResult(false);
        await pass.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_keep_the_capture_stopped() => (await _captures.Get(_events.Id)).Status.ShouldEqual(CaptureStatus.Stopped);
    [Fact] async Task should_not_resubscribe_the_stopped_capture() => await _subscriptions.DidNotReceive().Subscribe(_eventStore, "tenant", Arg.Any<CaptureDefinition>());
}
