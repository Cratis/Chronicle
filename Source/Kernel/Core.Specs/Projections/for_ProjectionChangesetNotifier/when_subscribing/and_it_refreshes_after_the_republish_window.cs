// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_refreshes_after_the_republish_window : given.a_subscribed_notifier
{
    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _timeProvider.Seconds += 21;
        await _notifier.Subscribe(_subscribers[0]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_publish_again() => _projectionsServiceClient.Received(2).SetWatchState(_eventStore, _namespace, _projection, true);
}
