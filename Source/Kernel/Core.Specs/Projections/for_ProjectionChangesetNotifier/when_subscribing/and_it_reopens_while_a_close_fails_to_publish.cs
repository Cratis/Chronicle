// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_reopens_while_a_close_fails_to_publish : given.a_subscribed_notifier
{
    readonly List<bool> _published = [];
    readonly TaskCompletionSource _closeCompleted = new();

    async Task Because()
    {
        _projectionsServiceClient.SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), Arg.Any<bool>())
            .Returns(call =>
            {
                var isWatched = call.Arg<bool>();
                _published.Add(isWatched);
                return isWatched ? Task.CompletedTask : _closeCompleted.Task;
            });

        await _silo.TimerRegistry.FireAllAsync();
        await _notifier.Unsubscribe(_subscribers[0]);
        await _notifier.Unsubscribe(_subscribers[1]);
        var closing = _silo.TimerRegistry.FireAllAsync();

        await _notifier.Subscribe(Substitute.For<IReadModelChangesetSubscriber>());
        _closeCompleted.SetException(new InvalidOperationException("the silo could not be reached"));
        await closing;
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_end_with_the_projection_published_as_watched() => _published[^1].ShouldBeTrue();
    [Fact] void should_publish_the_failed_close_and_then_the_reopen() => _published.ShouldEqual([true, false, true]);
}
