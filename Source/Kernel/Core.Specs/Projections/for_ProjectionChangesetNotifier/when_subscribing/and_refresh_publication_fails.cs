// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_refresh_publication_fails : given.a_subscribed_notifier
{
    bool _hasSubscribers;

    void Establish()
    {
        _timeProvider.Seconds += 21;
        _projectionsServiceClient.SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), true)
            .Returns(Task.FromException(new TimeoutException("Watch refresh failed")));
    }

    async Task Because()
    {
        await _notifier.Subscribe(_subscribers[0]);
        await _silo.TimerRegistry.FireAllAsync();
        _hasSubscribers = await _notifier.HasSubscribers();
    }

    [Fact] void should_retain_the_established_subscribers() => _hasSubscribers.ShouldBeTrue();
    [Fact] void should_not_publish_the_projection_as_unwatched() => _projectionsServiceClient.DidNotReceive().SetWatchState(_eventStore, _namespace, _projection, false);
}
