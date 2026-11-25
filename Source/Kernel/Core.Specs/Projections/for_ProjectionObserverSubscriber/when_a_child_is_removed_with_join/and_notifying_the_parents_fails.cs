// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_notifying_the_parents_fails : given.a_run_with_affected_parents
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        _notifier.NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>()).Returns(_ => throw new InvalidOperationException("the notifier is gone"));
        _result = await _subscriber.OnNext("the-removed-group", [_event], new(null));
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_still_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_have_tried_to_notify_the_parents() => _notifier.Received(1).NotifyMany(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IReadOnlyList<ReadModelChangeNotification>>());
}
