// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_reopens_while_a_close_fails_to_publish : given.a_subscribed_notifier
{
    readonly List<bool> _published = [];
    readonly TaskCompletionSource _closeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _closeCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reopenEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reopenCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _acknowledgedDuringClose;
    bool _acknowledgedBeforeWatched;
    bool _acknowledgedAfterWatched;

    async Task Because()
    {
        _projectionsServiceClient.SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), Arg.Any<bool>())
            .Returns(call =>
            {
                var isWatched = call.Arg<bool>();
                _published.Add(isWatched);
                if (isWatched)
                {
                    _reopenEntered.SetResult();
                    return _reopenCompleted.Task;
                }

                _closeEntered.SetResult();
                return _closeCompleted.Task;
            });

        await _notifier.Unsubscribe(_subscribers[0]);
        await _notifier.Unsubscribe(_subscribers[1]);
        var closing = _silo.TimerRegistry.FireAllAsync();
        await _closeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        var reopening = _notifier.Subscribe(Substitute.For<IReadModelChangesetSubscriber>());
        _acknowledgedDuringClose = reopening.IsCompleted;
        _closeCompleted.SetException(new InvalidOperationException("the silo could not be reached"));
        await _reopenEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _acknowledgedBeforeWatched = reopening.IsCompleted;
        _reopenCompleted.SetResult();
        await closing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await reopening.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _acknowledgedAfterWatched = reopening.IsCompletedSuccessfully;
    }

    [Fact] void should_end_with_the_projection_published_as_watched() => _published[^1].ShouldBeTrue();
    [Fact] void should_publish_the_failed_close_and_then_the_reopen() => _published.ShouldEqual([false, true]);
    [Fact] void should_not_acknowledge_the_reopen_during_the_close() => _acknowledgedDuringClose.ShouldBeFalse();
    [Fact] void should_not_acknowledge_the_reopen_before_watched_is_published() => _acknowledgedBeforeWatched.ShouldBeFalse();
    [Fact] void should_acknowledge_the_reopen_after_watched_is_published() => _acknowledgedAfterWatched.ShouldBeTrue();
}
