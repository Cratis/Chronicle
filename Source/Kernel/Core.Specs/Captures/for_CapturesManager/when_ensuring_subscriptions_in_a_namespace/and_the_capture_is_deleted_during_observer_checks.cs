// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_ensuring_subscriptions_in_a_namespace;

public class and_the_capture_is_deleted_during_observer_checks : given.a_captures_manager
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
        _captures.Delete(_events.Id).Returns(_ =>
        {
            _captures.Has(_events.Id).Returns(false);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        var pass = _manager.EnsureSubscribedIn("tenant");
        await _checking.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.Delete(_events.Id);
        _subscriptionCheck.TrySetResult(false);
        await pass.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_keep_the_capture_deleted() => (await _captures.Has(_events.Id)).ShouldBeFalse();
    [Fact] async Task should_not_create_an_orphan_subscription() => await _subscriptions.DidNotReceive().Subscribe(_eventStore, "tenant", Arg.Any<CaptureDefinition>());
}
