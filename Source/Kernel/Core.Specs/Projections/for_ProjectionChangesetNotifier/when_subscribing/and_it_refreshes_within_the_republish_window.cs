// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionChangesetNotifier.when_subscribing;

public class and_it_refreshes_within_the_republish_window : given.a_subscribed_notifier
{
    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        _timeProvider.Seconds += 10;
        await _notifier.Subscribe(_subscribers[0]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_publish_again() => _projectionsServiceClient.ReceivedWithAnyArgs(1).SetWatchState(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ObserverId>(), Arg.Any<bool>());
}
